using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.McpClient;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Text.Json;
using System.Collections.Concurrent;

namespace MCPTest.WebHost.Services;

public class RealMCPTestService
{
    private readonly MCPServerOptions _mcpOptions;
    private readonly ILogger<RealMCPTestService> _logger;
    private readonly EnvironmentVariableService _envService;
    private readonly IEnumerable<IMcpClientProvider> _mcpClientProviders;
    private readonly Dictionary<string, IMcpClient> _connectedClients = new();

    public RealMCPTestService(
        IOptions<MCPServerOptions> mcpOptions,
        ILogger<RealMCPTestService> logger,
        EnvironmentVariableService envService,
        IEnumerable<IMcpClientProvider> mcpClientProviders)
    {
        _mcpOptions = mcpOptions.Value;
        _logger = logger;
        _envService = envService;
        _mcpClientProviders = mcpClientProviders;
    }

    public List<MCPServerInfo> GetAvailableServers()
    {
        var servers = new List<MCPServerInfo>();

        // Add configured servers from appsettings
        foreach (var (serverName, config) in _mcpOptions.MCPServers)
        {
            var resolvedEnv = config.Env?.Any() == true 
                ? _envService.ResolveEnvironmentVariables(config.Env) 
                : new Dictionary<string, string>();

            servers.Add(new MCPServerInfo
            {
                Name = serverName,
                Description = config.Description,
                Command = config.Command,
                Args = config.Args,
                RequiresEnv = config.Env?.Any() == true,
                EnvKeys = config.Env?.Keys.ToList() ?? new List<string>(),
                ResolvedEnv = resolvedEnv
            });
        }

        // Add default servers from DefaultMCPServers
        foreach (var (serverName, config) in DefaultMCPServers.Configs)
        {
            if (!servers.Any(s => s.Name == serverName))
            {
                var resolvedEnv = config.Env?.Any() == true 
                    ? _envService.ResolveEnvironmentVariables(config.Env) 
                    : new Dictionary<string, string>();

                servers.Add(new MCPServerInfo
                {
                    Name = serverName,
                    Description = config.Description,
                    Command = config.Command,
                    Args = config.Args,
                    RequiresEnv = config.Env?.Any() == true,
                    EnvKeys = config.Env?.Keys.ToList() ?? new List<string>(),
                    ResolvedEnv = resolvedEnv
                });
            }
        }

        return servers;
    }

    public async Task<List<MCPToolInfo>> GetAvailableToolsAsync(string serverName)
    {
        try
        {
            var client = await GetOrCreateClientAsync(serverName);
            var tools = await client.ListToolsAsync();
            
            _logger.LogDebug("Found {ToolCount} tools for server {ServerName}", tools.Count, serverName);
            
            return tools.Select(tool => 
            {
                _logger.LogDebug("Processing tool {ToolName} with description {ToolDescription}", 
                    tool.Name, tool.Description);
                
                var parameters = ExtractToolParameters(tool);
                _logger.LogDebug("Extracted {ParameterCount} parameters for tool {ToolName}: {Parameters}", 
                    parameters.Count, tool.Name, string.Join(", ", parameters.Keys));
                    
                return new MCPToolInfo
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = parameters
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tools for server {ServerName}", serverName);
            throw new InvalidOperationException($"Failed to connect to MCP server '{serverName}': {ex.Message}");
        }
    }

    private Dictionary<string, MCPParameterInfo> ExtractToolParameters(McpClientTool tool)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
        
        try
        {
            _logger.LogDebug("Extracting parameters for tool: {ToolName}", tool.Name);
            
            // Log all available properties to understand the structure
            var properties = tool.GetType().GetProperties();
            _logger.LogDebug("Tool {ToolName} properties: {Properties}", 
                tool.Name, string.Join(", ", properties.Select(p => $"{p.Name}:{p.PropertyType.Name}")));
            
            // Try different possible property names for input schema
            object? schema = null;
            
            // Check for common schema property names
            var schemaProperty = properties.FirstOrDefault(p => 
                p.Name.Equals("InputSchema", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("Schema", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("Parameters", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("Args", StringComparison.OrdinalIgnoreCase));
            
            if (schemaProperty != null)
            {
                schema = schemaProperty.GetValue(tool);
                _logger.LogDebug("Found schema property {PropertyName} with value: {Schema}", 
                    schemaProperty.Name, JsonSerializer.Serialize(schema));
            }
            
            if (schema != null)
            {
                parameters = ParseSchemaToParameters(schema, tool.Name);
            }
            else
            {
                // If no schema found, try to infer parameters from tool description or create basic ones
                _logger.LogDebug("No schema found for tool {ToolName}, creating default parameters", tool.Name);
                parameters = CreateDefaultParameters(tool);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool parameters for tool {ToolName}", tool.Name);
            // Return basic parameters as fallback
            parameters = CreateDefaultParameters(tool);
        }
        
        return parameters;
    }

    private Dictionary<string, MCPParameterInfo> ParseSchemaToParameters(object schema, string toolName)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
        
        try
        {
            // Convert schema to JsonElement for easier parsing
            var schemaJson = JsonSerializer.Serialize(schema);
            var schemaElement = JsonSerializer.Deserialize<JsonElement>(schemaJson);
            
            _logger.LogDebug("Parsing schema for tool {ToolName}: {Schema}", toolName, schemaJson);
            
            // Handle different schema formats
            if (schemaElement.ValueKind == JsonValueKind.Object)
            {
                // Try to find properties in the schema
                if (schemaElement.TryGetProperty("properties", out var propertiesElement))
                {
                    // Standard JSON Schema format
                    parameters = ParseJsonSchemaProperties(propertiesElement, schemaElement, toolName);
                }
                else if (schemaElement.TryGetProperty("type", out var typeElement) && 
                         typeElement.GetString() == "object")
                {
                    // Simple object type - create generic parameter
                    parameters["input"] = new MCPParameterInfo
                    {
                        Type = "object",
                        Description = "Input parameters for the tool",
                        Required = false
                    };
                }
                else
                {
                    // Try to parse as direct parameter definition
                    foreach (var property in schemaElement.EnumerateObject())
                    {
                        parameters[property.Name] = new MCPParameterInfo
                        {
                            Type = ExtractTypeFromValue(property.Value),
                            Description = property.Name,
                            Required = false
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse schema for tool {ToolName}", toolName);
        }
        
        return parameters;
    }

    private Dictionary<string, MCPParameterInfo> ParseJsonSchemaProperties(JsonElement propertiesElement, JsonElement rootSchema, string toolName)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
        var requiredFields = new HashSet<string>();
        
        // Extract required fields
        if (rootSchema.TryGetProperty("required", out var requiredElement) && 
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    requiredFields.Add(item.GetString() ?? "");
                }
            }
        }
        
        // Parse each property
        foreach (var property in propertiesElement.EnumerateObject())
        {
            var paramInfo = new MCPParameterInfo
            {
                Type = "string", // Default
                Description = property.Name,
                Required = requiredFields.Contains(property.Name)
            };
            
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                // Extract type
                if (property.Value.TryGetProperty("type", out var typeElement))
                {
                    paramInfo.Type = typeElement.GetString() ?? "string";
                }
                
                // Extract description
                if (property.Value.TryGetProperty("description", out var descElement))
                {
                    paramInfo.Description = descElement.GetString() ?? property.Name;
                }
                
                // Extract default value
                if (property.Value.TryGetProperty("default", out var defaultElement))
                {
                    paramInfo.Description += $" (Default: {defaultElement})";
                }
                
                // Extract enum values
                if (property.Value.TryGetProperty("enum", out var enumElement) && 
                    enumElement.ValueKind == JsonValueKind.Array)
                {
                    var enumValues = enumElement.EnumerateArray()
                        .Select(e => e.ToString())
                        .ToList();
                    paramInfo.Description += $" (Options: {string.Join(", ", enumValues)})";
                }
            }
            
            parameters[property.Name] = paramInfo;
        }
        
        return parameters;
    }

    private Dictionary<string, MCPParameterInfo> CreateDefaultParameters(McpClientTool tool)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
        
        // Create specific parameter patterns based on tool name
        var toolName = tool.Name.ToLower();
        var toolDescription = tool.Description?.ToLower() ?? "";
        
        // Twitter specific tools
        if (toolName == "search_tweets")
        {
            parameters["query"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Search query for tweets",
                Required = true
            };
            parameters["count"] = new MCPParameterInfo
            {
                Type = "number",
                Description = "Number of tweets to return (1-100)",
                Required = true
            };
            parameters["result_type"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Type of search results (recent, popular, mixed)",
                Required = false
            };
        }
        else if (toolName == "post_tweet")
        {
            parameters["text"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Tweet text content (max 280 characters)",
                Required = true
            };
        }
        // Time tools
        else if (toolName == "get_current_time")
        {
            parameters["timezone"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Timezone (optional, e.g. 'UTC', 'America/New_York')",
                Required = false
            };
        }
        else if (toolName == "convert_time")
        {
            parameters["time"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Time to convert (ISO format or human readable)",
                Required = true
            };
            parameters["from_timezone"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Source timezone",
                Required = true
            };
            parameters["to_timezone"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Target timezone",
                Required = true
            };
        }
        // Git tools
        else if (toolName.StartsWith("git_"))
        {
            if (toolName.Contains("commit"))
            {
                parameters["message"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "Commit message",
                    Required = true
                };
            }
            else if (toolName.Contains("add"))
            {
                parameters["files"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "Files to add (space separated or '.' for all)",
                    Required = false
                };
            }
            else if (toolName.Contains("branch"))
            {
                parameters["branch_name"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "Branch name",
                    Required = false
                };
            }
        }
        // File system tools
        else if (toolName.Contains("file") || toolName.Contains("read") || toolName.Contains("write"))
        {
            parameters["path"] = new MCPParameterInfo
            {
                Type = "string", 
                Description = "File or directory path",
                Required = true
            };
            
            if (toolName.Contains("write"))
            {
                parameters["content"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "Content to write to file",
                    Required = true
                };
            }
        }
        // Generic search tools
        else if (toolName.Contains("search") || toolName.Contains("query"))
        {
            parameters["query"] = new MCPParameterInfo
            {
                Type = "string",
                Description = "Search query or text to search for",
                Required = true
            };
            
            parameters["limit"] = new MCPParameterInfo
            {
                Type = "number",
                Description = "Maximum number of results to return",
                Required = false
            };
        }
        // Generic tools - create based on description
        else
        {
            // Try to infer from description
            if (toolDescription.Contains("search") || toolDescription.Contains("query"))
            {
                parameters["query"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "Search query",
                    Required = true
                };
            }
            
            if (toolDescription.Contains("file") || toolDescription.Contains("path"))
            {
                parameters["path"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = "File path",
                    Required = true
                };
            }
            
            // If no specific patterns match, create a generic input parameter
            if (parameters.Count == 0)
            {
                parameters["input"] = new MCPParameterInfo
                {
                    Type = "string",
                    Description = $"Input parameter for {tool.Name}",
                    Required = false
                };
            }
        }
        
        _logger.LogDebug("Created default parameters for tool {ToolName}: {Parameters}", 
            toolName, string.Join(", ", parameters.Keys));
        
        return parameters;
    }

    private string ExtractTypeFromValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number", 
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => "string"
        };
    }

    private string ExtractContentText(object? content)
    {
        if (content == null)
        {
            return "No content returned";
        }

        try
        {
            // Check if it's a TextContentBlock or similar
            var contentType = content.GetType();
            _logger.LogDebug("Processing content of type: {ContentType}", contentType.Name);

            // Try to get Text property using reflection
            var textProperty = contentType.GetProperty("Text");
            if (textProperty != null)
            {
                var textValue = textProperty.GetValue(content);
                if (textValue != null)
                {
                    _logger.LogDebug("Extracted text: {Text}", textValue);
                    return textValue.ToString() ?? "Empty text content";
                }
            }

            // Try to get Content property
            var contentProperty = contentType.GetProperty("Content");
            if (contentProperty != null)
            {
                var contentValue = contentProperty.GetValue(content);
                if (contentValue != null)
                {
                    _logger.LogDebug("Extracted content: {Content}", contentValue);
                    return contentValue.ToString() ?? "Empty content";
                }
            }

            // Try to serialize the whole object to see its structure
            var serialized = JsonSerializer.Serialize(content, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            _logger.LogDebug("Serialized content: {SerializedContent}", serialized);

            // Try to parse the serialized JSON to extract text
            try
            {
                using var doc = JsonDocument.Parse(serialized);
                var root = doc.RootElement;

                // Look for common text properties
                if (root.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "Empty text";
                }

                if (root.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? contentElement.ToString();
                }

                if (root.TryGetProperty("value", out var valueElement))
                {
                    return valueElement.GetString() ?? valueElement.ToString();
                }

                // If no specific property found, return the formatted JSON
                return serialized;
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return the string representation
                return content.ToString() ?? "Unable to extract content";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract content text from {ContentType}", content.GetType().Name);
            return $"Error extracting content: {ex.Message}";
        }
    }

    public async Task<MCPToolResult> CallToolAsync(string serverName, string toolName, Dictionary<string, object> arguments)
    {
        try
        {
            var client = await GetOrCreateClientAsync(serverName);
            
            // Call tool using the correct method
            var argumentsDict = arguments.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
            var response = await client.CallToolAsync(toolName, argumentsDict);
            
            var content = response.Content?.FirstOrDefault();
            var resultText = ExtractContentText(content);

            return new MCPToolResult
            {
                Success = !response.IsError.GetValueOrDefault(),
                Result = resultText ?? "",
                Error = response.IsError.GetValueOrDefault() ? "Tool execution failed" : null,
                ExecutionTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tool {ToolName} on server {ServerName}", toolName, serverName);
            return new MCPToolResult
            {
                Success = false,
                Result = "",
                Error = ex.Message,
                ExecutionTime = DateTime.UtcNow
            };
        }
    }

    private async Task<IMcpClient> GetOrCreateClientAsync(string serverName)
    {
        if (_connectedClients.TryGetValue(serverName, out var existingClient))
        {
            try
            {
                // Test if client is still connected
                await existingClient.PingAsync();
                return existingClient;
            }
            catch
            {
                // Client is disconnected, remove it
                _connectedClients.Remove(serverName);
                await existingClient.DisposeAsync();
            }
        }

        // Get server config
        var config = GetServerConfig(serverName);
        if (config == null)
        {
            throw new InvalidOperationException($"Server '{serverName}' not found in configuration");
        }

        // Apply environment variables
        if (config.Env?.Any() == true)
        {
            var resolvedEnv = _envService.ResolveEnvironmentVariables(config.Env);
            foreach (var (key, value) in resolvedEnv)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // Create client using appropriate provider
        IMcpClientProvider provider;
        if (string.IsNullOrEmpty(config.Url))
        {
            // Use stdio provider
            provider = _mcpClientProviders.FirstOrDefault(p => p.ClientType == McpClientType.Stdio);
            if (provider == null)
            {
                throw new InvalidOperationException("No Stdio MCP client provider found");
            }
        }
        else
        {
            // Use SSE provider
            provider = _mcpClientProviders.FirstOrDefault(p => p.ClientType == McpClientType.Sse);
            if (provider == null)
            {
                throw new InvalidOperationException("No SSE MCP client provider found");
            }
        }

        var client = await provider.GetOrCreateClientAsync(config);
        _connectedClients[serverName] = client;

        _logger.LogInformation("Connected to MCP server: {ServerName}", serverName);
        return client;
    }

    private MCPServerConfig? GetServerConfig(string serverName)
    {
        // Check configured servers first
        if (_mcpOptions.MCPServers?.TryGetValue(serverName, out var config) == true)
        {
            return config;
        }

        // Check default servers
        if (DefaultMCPServers.Configs.TryGetValue(serverName, out var defaultConfig))
        {
            return defaultConfig;
        }

        return null;
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var (serverName, client) in _connectedClients.ToList())
        {
            try
            {
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting from server {ServerName}", serverName);
            }
        }
        _connectedClients.Clear();
    }
}

public class MCPServerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public bool RequiresEnv { get; set; }
    public List<string> EnvKeys { get; set; } = new();
    public Dictionary<string, string> ResolvedEnv { get; set; } = new();
}

public class MCPToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, MCPParameterInfo> Parameters { get; set; } = new();
}

public class MCPParameterInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}

public class MCPToolResult
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime ExecutionTime { get; set; }
}
