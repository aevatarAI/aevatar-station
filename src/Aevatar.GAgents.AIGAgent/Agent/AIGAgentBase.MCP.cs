using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

// ReSharper disable InconsistentNaming
/// <summary>
/// Partial class for AIGAgentBase that adds MCP (Model Context Protocol) tool capabilities
/// </summary>
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private readonly Dictionary<string, string>
        _toolNameMapping = new(); // Maps kernel function names to MCP tool names

    public virtual async Task<bool> ConfigureMCPServersAsync(List<IMCPGAgent> mcpGAgents)
    {
        try
        {
            var mcpAgents = new Dictionary<string, MCPGAgentReference>();

            foreach (var mcpAgent in mcpGAgents)
            {
                var mcpAgentId = mcpAgent.GetPrimaryKey();
                var server = (await mcpAgent.GetStateAsync()).MCPServerConfig;

                mcpAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = server.Description
                };

                // Log available tools from this server
                var serverTools = await mcpAgent.GetAvailableToolsAsync();
                foreach (var tool in serverTools)
                {
                    Logger.LogInformation($"Registered MCP tool: {server.ServerName}.{tool.Name} - {tool.Description}");
                }
            }

            if (!mcpAgents.Any())
            {
                // No valid MCP servers configured
                return false;
            }

            // Update state
            var configureServersEvent = new ConfigureMCPServersStateLogEvent
            {
                MCPServers = mcpAgents
            };

            var enableMCPToolsEvent = new SetEnableMCPToolsStateLogEvent
            {
                EnableMCPTools = true
            };

            RaiseEvent(configureServersEvent);
            RaiseEvent(enableMCPToolsEvent);
            await ConfirmEvents();

            // Update kernel tools if brain is initialized
            if (_brain != null)
            {
                await UpdateKernelWithMCPToolsAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure MCP servers 1");
            return false;
        }
    }

    /// <summary>
    /// Configure MCP servers for this agent
    /// </summary>
    public virtual async Task<bool> ConfigureMCPServersAsync(List<MCPServerConfig> servers)
    {
        try
        {
            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
            var mcpAgents = new Dictionary<string, MCPGAgentReference>();

            foreach (var server in servers)
            {
                if (!server.IsValid())
                {
                    Logger.LogWarning("Skipping invalid MCP server configuration");
                    continue;
                }

                // Create config for the MCP agent
                var mcpConfig = new MCPGAgentConfig
                {
                    ServerConfig = server
                };

                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);
                var mcpAgentId = mcpAgent.GetPrimaryKey();

                mcpAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = server.Description
                };

                // Log available tools from this server
                var serverTools = await mcpAgent.GetAvailableToolsAsync();
                foreach (var tool in serverTools)
                {
                    Logger.LogInformation($"Registered MCP tool: {server.ServerName}.{tool.Name} - {tool.Description}");
                }
            }

            if (!mcpAgents.Any())
            {
                // No valid MCP servers configured
                return false;
            }

            // Update state
            var configureServersEvent = new ConfigureMCPServersStateLogEvent
            {
                MCPServers = mcpAgents
            };

            var enableMCPToolsEvent = new SetEnableMCPToolsStateLogEvent
            {
                EnableMCPTools = true
            };

            RaiseEvent(configureServersEvent);
            RaiseEvent(enableMCPToolsEvent);
            await ConfirmEvents();

            // Update kernel tools if brain is initialized
            if (_brain != null)
            {
                await UpdateKernelWithMCPToolsAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure MCP servers 2");
            return false;
        }
    }

    /// <summary>
    /// Get available MCP tools from all configured servers
    /// </summary>
    public virtual async Task<List<MCPToolInfo>> GetAvailableMCPToolsAsync()
    {
        var allTools = new List<MCPToolInfo>();
        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();

                foreach (var tool in tools)
                {
                    allTools.Add(tool);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to get tools from MCP server {serverName}");
            }
        }

        return allTools;
    }

    /// <summary>
    /// Update the kernel with MCP tools
    /// </summary>
    protected virtual async Task UpdateKernelWithMCPToolsAsync()
    {
        var kernel = GetKernelFromBrain();
        if (kernel == null)
        {
            Logger.LogWarning("Cannot update kernel with MCP tools: Kernel not available");
            return;
        }

        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        var registeredFunctions = new List<string>();

        // Clear the tool name mapping
        _toolNameMapping.Clear();

        // Register MCP tools as kernel functions
        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();

                var functions = new List<KernelFunction>();

                foreach (var tool in tools)
                {
                    // toolKey already contains serverName prefix (e.g., "mcp-server-weread.get_bookshelf")
                    // Extract the actual tool name
                    var actualToolName = tool.Name;
                    var mcpToolFullName = tool.ServerName; // Use the key as-is

                    // Use GenerateMCPFunctionName to ensure the name doesn't exceed 64 characters
                    var kernelFunctionName = GenerateMCPFunctionName(serverName, actualToolName);
                    Logger.LogInformation("MCP function name: {FunctionName} (length: {Length})", kernelFunctionName,
                        kernelFunctionName.Length);

                    // Store the mapping for later use
                    _toolNameMapping[kernelFunctionName] = mcpToolFullName;

                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) => await CallMCPToolAsync(serverName, actualToolName, args),
                        functionName: kernelFunctionName,
                        description: tool.Description,
                        parameters: ConvertMCPToKernelParameters(tool.Parameters)
                    );

                    functions.Add(function);
                    registeredFunctions.Add(kernelFunctionName);
                    Logger.LogInformation($"Registered MCP tool: {kernelFunctionName} (MCP: {mcpToolFullName})");
                }

                if (functions.Any())
                {
                    // Clean server name to be a valid plugin name (only ASCII letters, digits, and underscores)
                    var pluginName = $"MCP_{serverName.Replace("-", "_").Replace(".", "_").Replace(" ", "_")}";

                    // Remove existing plugin with the same name to avoid duplicates
                    var existingPlugin = kernel.Plugins.FirstOrDefault(p => p.Name == pluginName);
                    if (existingPlugin != null)
                    {
                        kernel.Plugins.Remove(existingPlugin);
                        Logger.LogDebug("Removed existing MCP plugin '{PluginName}' before re-registering", pluginName);
                    }

                    kernel.Plugins.AddFromFunctions(pluginName, functions);
                    Logger.LogInformation(
                        $"Registered {functions.Count} tools from MCP server {serverName} as plugin {pluginName}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to register tools from MCP server {serverName}");
            }
        }

        // Update state with registered functions
        if (registeredFunctions.Any())
        {
            var updateFunctionsEvent = new SetRegisteredMCPFunctionsStateLogEvent
            {
                RegisteredFunctions = registeredFunctions
            };
            RaiseEvent(updateFunctionsEvent);
            await ConfirmEvents();
        }
    }

    /// <summary>
    /// Call an MCP tool
    /// </summary>
    protected async Task<string> CallMCPToolAsync(string serverName, string toolName, KernelArguments kernelArgs)
    {
        var toolStartTime = DateTime.UtcNow;
        var toolCall = new ToolCallDetail
        {
            ToolName = toolName,
            ServerName = serverName,
            Arguments = kernelArgs.ToDictionary(),
            Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
        };

        try
        {
            Logger.LogInformation($"Calling MCP tool: {serverName}.{toolName}");

            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

            if (!State.MCPAgents.TryGetValue(serverName, out var agentRef))
            {
                var errorMsg = $"Error: MCP server '{serverName}' not found";
                toolCall.Success = false;
                toolCall.Result = errorMsg;
                toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
                _currentToolCalls.Add(toolCall);
                return errorMsg;
            }

            var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);

            // Get tool info to understand parameter types
            var tools = await mcpAgent.GetAvailableToolsAsync();
            MCPToolInfo? toolInfo = null;
            foreach (var tool in tools)
            {
                if (tool.Name == toolName)
                {
                    toolInfo = tool;
                    break;
                }
            }

            // Convert kernel arguments to properly typed parameters
            var parameters = new Dictionary<string, object>();
            foreach (var (key, value) in kernelArgs)
            {
                if (value != null)
                {
                    // Check if we have type information for this parameter
                    if (toolInfo?.Parameters.TryGetValue(key, out var paramInfo) == true)
                    {
                        parameters[key] = ConvertToExpectedType(value, paramInfo.Type);
                    }
                    else
                    {
                        // No type info, use basic conversion
                        parameters[key] = ConvertJsonElementToBasicType(value);
                    }
                }
            }

            // Store arguments in tool call
            toolCall.Arguments = parameters;

            // Call the MCP tool with the actual tool name (not the kernel function name)
            var response = await mcpAgent.CallToolAsync(serverName, toolName, parameters);

            Logger.LogInformation($"MCP tool {serverName}.{toolName} completed");

            // Extract the actual result from MCPToolCallResult
            string result;
            if (response.Result is MCPToolCallResult mcpResult)
            {
                // If successful, return Data content; if failed, return error message
                result = mcpResult.Success
                    ? (mcpResult.Data ?? string.Empty)
                    : (mcpResult.ErrorMessage ?? "Unknown error");
            }
            else if (response.Result != null)
            {
                // If not MCPToolCallResult, try to serialize as JSON
                result = JsonSerializer.Serialize(response.Result);
            }
            else
            {
                result = string.Empty;
            }

            // Track tool call with proper success status
            if (response.Result is MCPToolCallResult mcpResult2)
            {
                toolCall.Success = mcpResult2.Success;
            }
            else
            {
                // If Result is not null, assume success
                toolCall.Success = response.Success && response.Result != null;
            }

            toolCall.Result = result;
            toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolCall);

            Logger.LogInformation(
                "[MCP Tool Call] {ServerName}.{ToolName} completed in {Duration}ms",
                serverName, toolName, toolCall.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling MCP tool {serverName}.{toolName}");

            var errorResult = $"Error calling tool: {ex.Message}";

            // Track failed tool call
            toolCall.Success = false;
            toolCall.Result = errorResult;
            toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolCall);

            Logger.LogError(ex,
                "[MCP Tool Call] {ServerName}.{ToolName} failed after {Duration}ms",
                serverName, toolName, toolCall.DurationMs);

            return errorResult;
        }
    }

    /// <summary>
    /// Convert MCP parameter info to Kernel parameter metadata
    /// </summary>
    private KernelParameterMetadata[] ConvertMCPToKernelParameters(Dictionary<string, MCPParameterInfo> mcpParameters)
    {
        var parameters = new List<KernelParameterMetadata>();

        foreach (var (name, paramInfo) in mcpParameters)
        {
            try
            {
                // For array types, use special handling to ensure OpenAI can understand correctly
                if (paramInfo.Type == "array")
                {
                    var arrayMetadata = CreateArrayParameterMetadata(name, paramInfo);
                    parameters.Add(arrayMetadata);
                }
                else
                {
                    // Use new MCPParameterInfo conversion method
                    var kernelParam = paramInfo.ToKernelParameterMetadata();

                    // Ensure the returned type is correct
                    if (kernelParam is KernelParameterMetadata metadata)
                    {
                        parameters.Add(metadata);
                    }
                    else
                    {
                        // Fall back to old method as alternative
                        Logger.LogWarning(
                            "Using fallback method to create KernelParameterMetadata for parameter: {ParameterName}",
                            name);
                        var fallbackMetadata = CreateFallbackKernelParameterMetadata(name, paramInfo);
                        parameters.Add(fallbackMetadata);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to convert MCP parameter: {ParameterName}, using fallback method", name);
                var fallbackMetadata = CreateFallbackKernelParameterMetadata(name, paramInfo);
                parameters.Add(fallbackMetadata);
            }
        }

        return parameters.ToArray();
    }

    /// <summary>
    /// Create array type KernelParameterMetadata to ensure OpenAI can understand correctly
    /// </summary>
    private KernelParameterMetadata CreateArrayParameterMetadata(string name, MCPParameterInfo paramInfo)
    {
        // Generate description containing complete JSON Schema
        var schema = GenerateSchemaForParameter(paramInfo);
        var schemaJson = JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Create clear description explaining this is an array parameter
        var itemType = paramInfo.ArrayItems?.Type ?? "string";
        var enhancedDescription = paramInfo.Description ?? $"Array of {itemType} values";

        // To address SemanticKernel limitations, we explicitly state the array structure in the description
        enhancedDescription = $"{enhancedDescription}. This parameter expects an array of {itemType} values.";

        // Create parameter metadata
        var metadata = new KernelParameterMetadata(name);
        var metadataType = metadata.GetType();

        // Set description (including schema information)
        var descProp = metadataType.GetProperty("Description");
        if (descProp != null && descProp.CanWrite)
        {
            try
            {
                descProp.SetValue(metadata, enhancedDescription);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
            }
        }

        // Set required property
        var reqProp = metadataType.GetProperty("IsRequired");
        if (reqProp != null && reqProp.CanWrite)
        {
            try
            {
                reqProp.SetValue(metadata, paramInfo.Required);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set IsRequired property on KernelParameterMetadata");
            }
        }

        // Set parameter type to JsonElement, allowing the function to handle array parsing itself
        // This is a workaround because SemanticKernel 1.57.0-alpha has issues with array schema handling
        var typeProp = metadataType.GetProperty("ParameterType");
        if (typeProp != null && typeProp.CanWrite)
        {
            try
            {
                // Use JsonElement to let MCP tool function handle JSON parsing itself
                typeProp.SetValue(metadata, typeof(System.Text.Json.JsonElement));
                Logger.LogDebug("Set ParameterType to JsonElement for array parameter {Name}", name);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set ParameterType property on KernelParameterMetadata");
                // If failed, try to set as object type
                try
                {
                    typeProp.SetValue(metadata, typeof(object));
                }
                catch
                {
                }
            }
        }

        // Set Schema property (this is crucial!)
        var schemaProp = metadataType.GetProperty("Schema");
        if (schemaProp != null && schemaProp.CanWrite)
        {
            try
            {
                // Schema property is KernelJsonSchema type
                var kernelJsonSchemaType = schemaProp.PropertyType;
                var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    var kernelJsonSchema = ctor.Invoke(new object[] { schemaJson });
                    schemaProp.SetValue(metadata, kernelJsonSchema);
                    Logger.LogDebug("Successfully set Schema property for array parameter {Name}", name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set Schema property on KernelParameterMetadata");
            }
        }

        return metadata;
    }

    /// <summary>
    /// Create fallback KernelParameterMetadata (backward compatible)
    /// </summary>
    private KernelParameterMetadata CreateFallbackKernelParameterMetadata(string name, MCPParameterInfo paramInfo)
    {
        // Special handling for array types
        if (paramInfo.Type == "array")
        {
            try
            {
                // Try to use KernelJsonSchemaBuilder to create schema including items
                var schemaBuilderType =
                    Type.GetType("Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel")
                    ?? Type.GetType(
                        "Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel.Abstractions");

                if (schemaBuilderType != null)
                {
                    var buildMethod = schemaBuilderType.GetMethod("Build",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (buildMethod != null)
                    {
                        var schema = GenerateSchemaForParameter(paramInfo);
                        var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
                        var kernelSchema = buildMethod.Invoke(null, new object[] { schemaJson });

                        // Create metadata with schema
                        var metadataCtors = typeof(KernelParameterMetadata).GetConstructors();
                        var schemaConstructor = metadataCtors.FirstOrDefault(c =>
                        {
                            var parameters = c.GetParameters();
                            return parameters.Length >= 2 &&
                                   parameters[0].ParameterType == typeof(string) &&
                                   parameters.Any(p => p.ParameterType.Name == "KernelJsonSchema");
                        });

                        if (schemaConstructor != null)
                        {
                            var ctorParams = schemaConstructor.GetParameters();
                            var args = new object[ctorParams.Length];
                            args[0] = name;

                            for (int i = 1; i < ctorParams.Length; i++)
                            {
                                if (ctorParams[i].ParameterType.Name == "KernelJsonSchema")
                                {
                                    args[i] = kernelSchema;
                                }
                                else if (ctorParams[i].HasDefaultValue)
                                {
                                    args[i] = ctorParams[i].DefaultValue;
                                }
                            }

                            return (KernelParameterMetadata)schemaConstructor.Invoke(args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Failed to create KernelParameterMetadata with schema for array parameter");
            }
        }

        // Use basic constructor
        var metadata = new KernelParameterMetadata(name);

        // Try to set properties using reflection
        var metadataType = metadata.GetType();

        // Set enhanced description containing more JsonSchema information
        var enhancedDescription = paramInfo.GetEnhancedDescription();
        var descProp = metadataType.GetProperty("Description");
        if (descProp != null && descProp.CanWrite)
        {
            try
            {
                descProp.SetValue(metadata, enhancedDescription);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
            }
        }

        // Set Required property
        var reqProp = metadataType.GetProperty("IsRequired");
        if (reqProp != null && reqProp.CanWrite)
        {
            try
            {
                reqProp.SetValue(metadata, paramInfo.Required);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set IsRequired property on KernelParameterMetadata");
            }
        }

        // Try to set parameter type
        var typeProp = metadataType.GetProperty("ParameterType");
        if (typeProp != null && typeProp.CanWrite)
        {
            try
            {
                typeProp.SetValue(metadata, paramInfo.GetDotNetType());
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set ParameterType property on KernelParameterMetadata");
            }
        }

        // Try to set default value
        if (paramInfo.DefaultValue != null)
        {
            var defaultProp = metadataType.GetProperty("DefaultValue");
            if (defaultProp != null && defaultProp.CanWrite)
            {
                try
                {
                    defaultProp.SetValue(metadata, paramInfo.DefaultValue);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Could not set DefaultValue property on KernelParameterMetadata");
                }
            }
        }

        // Try to set Schema property (especially important for array types)
        var schemaProp = metadataType.GetProperty("Schema");
        if (schemaProp != null && schemaProp.CanWrite)
        {
            try
            {
                var schema = GenerateSchemaForParameter(paramInfo);
                var schemaJson = JsonSerializer.Serialize(schema);
                // Schema property is KernelJsonSchema type
                var kernelJsonSchemaType = schemaProp.PropertyType;
                var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    var kernelJsonSchema = ctor.Invoke(new object[] { schemaJson });
                    schemaProp.SetValue(metadata, kernelJsonSchema);
                    Logger.LogDebug("Successfully set Schema property for parameter {Name} with type {Type}", name,
                        paramInfo.Type);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not set Schema property on KernelParameterMetadata");
            }
        }

        return metadata;
    }

    /// <summary>
    /// Generate JsonSchema for parameter
    /// </summary>
    private object GenerateSchemaForParameter(MCPParameterInfo paramInfo)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = paramInfo.Type ?? "string"
        };

        if (!string.IsNullOrEmpty(paramInfo.Description))
            schema["description"] = paramInfo.Description;

        // For array types, must include items property
        if (paramInfo.Type == "array")
        {
            if (paramInfo.ArrayItems != null)
            {
                schema["items"] = GenerateSchemaForParameter(paramInfo.ArrayItems);
            }
            else
            {
                // Default items as object type
                schema["items"] = new Dictionary<string, object> { ["type"] = "object" };
            }
        }

        // For object types
        if (paramInfo.Type == "object" && paramInfo.ObjectProperties != null)
        {
            var properties = new Dictionary<string, object>();
            foreach (var (propName, propInfo) in paramInfo.ObjectProperties)
            {
                properties[propName] = GenerateSchemaForParameter(propInfo);
            }

            schema["properties"] = properties;

            if (paramInfo.RequiredProperties?.Any() == true)
            {
                schema["required"] = paramInfo.RequiredProperties;
            }
        }

        // Add constraints
        if (paramInfo.EnumValues?.Any() == true)
            schema["enum"] = paramInfo.EnumValues;

        if (paramInfo.MinLength.HasValue)
            schema["minLength"] = paramInfo.MinLength.Value;

        if (paramInfo.MaxLength.HasValue)
            schema["maxLength"] = paramInfo.MaxLength.Value;

        if (paramInfo.Minimum.HasValue)
            schema["minimum"] = paramInfo.Minimum.Value;

        if (paramInfo.Maximum.HasValue)
            schema["maximum"] = paramInfo.Maximum.Value;

        if (!string.IsNullOrEmpty(paramInfo.Pattern))
            schema["pattern"] = paramInfo.Pattern;

        if (!string.IsNullOrEmpty(paramInfo.Format))
            schema["format"] = paramInfo.Format;

        return schema;
    }

    /// <summary>
    /// Convert JsonElement to basic types for Orleans serialization
    /// </summary>
    protected object ConvertJsonElementToBasicType(object value)
    {
        if (value is JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null!;
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToBasicType(item));
                    }

                    return list;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                    }

                    return dict;
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        return value;
    }

    /// <summary>
    /// Convert a value to the expected type based on MCP parameter type definition
    /// </summary>
    private object ConvertToExpectedType(object value, string expectedType)
    {
        // Handle JsonElement conversion first
        if (value is JsonElement element)
        {
            return ConvertJsonElementToExpectedType(element, expectedType);
        }

        // Handle string to other types conversion
        if (value is string strValue)
        {
            switch (expectedType.ToLower())
            {
                case "number":
                case "float":
                case "double":
                    if (double.TryParse(strValue, out var doubleValue))
                        return doubleValue;
                    throw new InvalidOperationException($"Cannot convert string '{strValue}' to number");

                case "integer":
                case "int":
                    if (int.TryParse(strValue, out var intValue))
                        return intValue;
                    throw new InvalidOperationException($"Cannot convert string '{strValue}' to integer");

                case "boolean":
                case "bool":
                    if (bool.TryParse(strValue, out var boolValue))
                        return boolValue;
                    // Handle "0"/"1" as boolean
                    if (strValue == "0") return false;
                    if (strValue == "1") return true;
                    throw new InvalidOperationException($"Cannot convert string '{strValue}' to boolean");

                case "array":
                    // Try to parse as JSON array
                    try
                    {
                        return JsonSerializer.Deserialize<List<object>>(strValue) ?? new List<object>();
                    }
                    catch
                    {
                        // If not JSON, return as single-element list
                        return new List<object> { strValue };
                    }

                case "object":
                    // Try to parse as JSON object
                    try
                    {
                        return JsonSerializer.Deserialize<Dictionary<string, object>>(strValue) ??
                               new Dictionary<string, object>();
                    }
                    catch
                    {
                        // If not JSON, return as-is
                        return strValue;
                    }

                case "string":
                    return strValue;

                default:
                    // Unknown type, return as-is
                    return strValue;
            }
        }

        // For non-string values, use the existing conversion logic
        return ConvertJsonElementToBasicType(value);
    }

    /// <summary>
    /// Convert JsonElement to expected type based on MCP parameter type definition
    /// </summary>
    private object ConvertJsonElementToExpectedType(JsonElement element, string expectedType)
    {
        switch (expectedType.ToLower())
        {
            case "string":
                return element.ValueKind == JsonValueKind.String
                    ? element.GetString() ?? string.Empty
                    : element.ToString();

            case "number":
            case "float":
            case "double":
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetDouble();
                if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var d))
                    return d;
                throw new InvalidOperationException($"Cannot convert {element.ValueKind} to number");

            case "integer":
            case "int":
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetInt32();
                if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var i))
                    return i;
                throw new InvalidOperationException($"Cannot convert {element.ValueKind} to integer");

            case "boolean":
            case "bool":
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return element.GetBoolean();
                if (element.ValueKind == JsonValueKind.String)
                {
                    var str = element.GetString();
                    if (bool.TryParse(str, out var b))
                        return b;
                    if (str == "0") return false;
                    if (str == "1") return true;
                }

                throw new InvalidOperationException($"Cannot convert {element.ValueKind} to boolean");

            case "array":
                if (element.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToBasicType(item));
                    }

                    return list;
                }

                throw new InvalidOperationException($"Cannot convert {element.ValueKind} to array");

            case "object":
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                    }

                    return dict;
                }

                throw new InvalidOperationException($"Cannot convert {element.ValueKind} to object");

            default:
                // Unknown type, use basic conversion
                return ConvertJsonElementToBasicType(element);
        }
    }

    /// <summary>
    /// State log event for configuring MCP servers
    /// </summary>
    [GenerateSerializer]
    public class ConfigureMCPServersStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Dictionary<string, MCPGAgentReference> MCPServers { get; set; } = new();
    }

    /// <summary>
    /// State log event for enabling/disabling MCP tools
    /// </summary>
    [GenerateSerializer]
    public class SetEnableMCPToolsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool EnableMCPTools { get; set; }
    }

    /// <summary>
    /// State log event for setting registered MCP functions
    /// </summary>
    [GenerateSerializer]
    public class SetRegisteredMCPFunctionsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<string> RegisteredFunctions { get; set; } = new();
    }
}