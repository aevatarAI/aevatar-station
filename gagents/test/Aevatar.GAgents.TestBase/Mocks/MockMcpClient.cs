using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aevatar.GAgents.MCP.Test.Mocks;

/// <summary>
/// Mock MCP client implementation
/// </summary>
public class MockMcpClient : IMcpClient
{
    private readonly string _serverName;
    private readonly List<MockTool> _tools;
    private readonly Dictionary<string, object> _memoryStore = new();
    private bool _disposed = false;

    public bool IsConnected { get; private set; } = true;

    // IMcpClient required properties
    public ServerCapabilities ServerCapabilities { get; private set; }
    public Implementation ServerInfo { get; private set; }
    public string? ServerInstructions { get; private set; }
    public string SessionId { get; private set; } = Guid.NewGuid().ToString();

    public MockMcpClient(string serverName, List<MockTool> tools)
    {
        _serverName = serverName;
        _tools = tools;

        // Initialize mock server info
        ServerInfo = new Implementation
        {
            Name = serverName,
            Version = "1.0.0-mock"
        };

        ServerCapabilities = new ServerCapabilities
        {
            Tools = new ToolsCapability { ListChanged = false }
        };
    }

    public async Task<List<McpClientTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Console.WriteLine(
            $"[MockMcpClient] ListToolsAsync called for server '{_serverName}' with {_tools.Count} tools");

        // 模拟网络延迟
        await Task.Delay(50, cancellationToken);

        // Use a wrapper that creates McpClientTool using reflection or factory method
        var result = _tools.Select(t => CreateMcpClientTool(t.Name, t.Description, CreateInputSchema(t.Parameters)))
            .ToList();
        Console.WriteLine($"[MockMcpClient] Returning {result.Count} tools");
        return result;
    }

    private McpClientTool CreateMcpClientTool(string name, string description, object inputSchema)
    {
        // Try to create McpClientTool using reflection or alternative method
        try
        {
            var jsonElement = JsonSerializer.SerializeToElement(inputSchema);

            // Use reflection to find constructor or factory method
            var mcpClientToolType = typeof(McpClientTool);
            var constructors = mcpClientToolType.GetConstructors();

            // Try to find a suitable constructor
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(string) &&
                    parameters[2].ParameterType == typeof(JsonElement))
                {
                    return (McpClientTool)constructor.Invoke(new object[] { name, description, jsonElement });
                }
            }

            // If no suitable constructor found, throw an error with helpful info
            throw new InvalidOperationException(
                $"Cannot create McpClientTool. Available constructors: {string.Join(", ", constructors.Select(c => $"({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})"))}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create McpClientTool for tool '{name}': {ex.Message}", ex);
        }
    }

    public async Task<CallToolResult> CallToolAsync(string name, Dictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        Console.WriteLine($"[MockMcpClient] CallToolAsync called with tool '{name}' on server '{_serverName}'");

        // 模拟网络延迟
        await Task.Delay(100, cancellationToken);

        arguments ??= new Dictionary<string, object>();

        // 处理带服务器前缀的工具名称（如 "filesystem.read_file" -> "read_file"）
        var actualToolName = name;
        if (name.Contains('.') && name.StartsWith(_serverName + "."))
        {
            actualToolName = name.Substring(_serverName.Length + 1);
            Console.WriteLine($"[MockMcpClient] Stripped server prefix from tool name: '{name}' -> '{actualToolName}'");
        }
        
        // 查找工具
        var tool = _tools.FirstOrDefault(t => t.Name == actualToolName);
        if (tool == null)
        {
            Console.WriteLine($"[MockMcpClient] Tool '{actualToolName}' (original: '{name}') not found on server '{_serverName}'. Available tools: {string.Join(", ", _tools.Select(t => t.Name))}");
            return CreateErrorResult($"Tool '{actualToolName}' not found on server '{_serverName}'");
        }

        // 验证必需参数
        Console.WriteLine($"[MockMcpClient] Checking required parameters for tool '{actualToolName}'. Tool has {tool.Parameters.Count} parameters: [{string.Join(", ", tool.Parameters.Select(p => $"{p.Key}(required:{p.Value.Required})"))}]");
        Console.WriteLine($"[MockMcpClient] Provided arguments: [{string.Join(", ", arguments.Select(a => $"{a.Key}:{a.Value}"))}]");
        
        foreach (var param in tool.Parameters.Where(p => p.Value.Required))
        {
            if (!arguments.ContainsKey(param.Key))
            {
                Console.WriteLine($"[MockMcpClient] Required parameter '{param.Key}' is missing for tool '{name}'");
                return CreateErrorResult($"Required parameter '{param.Key}' is missing");
            }
        }

        // 检查特殊的失败工具
        if (actualToolName == "failing_tool")
        {
            Console.WriteLine($"[MockMcpClient] Simulating tool failure for '{actualToolName}'");
            return CreateErrorResult("This tool always fails");
        }

        // 模拟不同工具的行为（使用去掉前缀后的工具名称）
        var result = SimulateToolExecution(actualToolName, arguments);

        var content = new ContentBlock[]
        {
            new TextContentBlock
            {
                Type = "text",
                Text = JsonSerializer.Serialize(result)
            }
        };
        
        Console.WriteLine($"[MockMcpClient] Tool '{name}' executed successfully with result: {JsonSerializer.Serialize(result)}");

        return new CallToolResult
        {
            Content = content,
            IsError = result.ContainsKey("error")
        };
    }

    /// <summary>
    /// 创建错误结果
    /// </summary>
    private CallToolResult CreateErrorResult(string errorMessage)
    {
        var errorContent = new ContentBlock[]
        {
            new TextContentBlock
            {
                Type = "text",
                Text = JsonSerializer.Serialize(new { error = errorMessage })
            }
        };

        return new CallToolResult
        {
            Content = errorContent,
            IsError = true
        };
    }

    public async Task<object> PingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.Delay(10, cancellationToken);
        return new { status = "pong", server = _serverName, timestamp = DateTime.UtcNow };
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            IsConnected = false;
            _disposed = true;
            await Task.CompletedTask;
        }
    }

    // IMcpEndpoint required methods
    public async Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Console.WriteLine($"[MockMcpClient] SendRequestAsync called with method: {request.Method}, params: {JsonSerializer.Serialize(request.Params)}");
        await Task.Delay(10, cancellationToken); // Simulate network delay

        switch (request.Method)
        {
            case "tools/list":
                // Return list of available tools
                var tools = _tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    inputSchema = CreateInputSchema(t.Parameters)
                }).ToArray();
                
                Console.WriteLine($"[MockMcpClient] Returning {tools.Length} tools for tools/list request");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = JsonNode.Parse(JsonSerializer.Serialize(new { tools = tools }))
                };

            case "tools/call":
                // Handle tool call
                if (request.Params != null)
                {
                    var toolCallResult = await HandleToolCallFromParams(request.Params);
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = JsonNode.Parse(JsonSerializer.Serialize(toolCallResult))
                    };
                }
                break;
        }

        // Default response for unhandled methods
        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = JsonNode.Parse(JsonSerializer.Serialize(new { status = "mock_response", method = request.Method }))
        };
    }

    private async Task<object> HandleToolCallFromParams(JsonNode paramsNode)
    {
        try
        {
            // Parse tool call parameters
            var toolName = paramsNode["name"]?.GetValue<string>();
            var arguments = paramsNode["arguments"]?.AsObject();

            if (string.IsNullOrEmpty(toolName))
            {
                return new { error = "Tool name is required" };
            }

            // Convert JsonObject to Dictionary<string, object>
            var argsDict = new Dictionary<string, object>();
            if (arguments != null)
            {
                foreach (var kvp in arguments)
                {
                    argsDict[kvp.Key] = kvp.Value?.ToString() ?? "";
                }
            }

            // Call the existing tool call logic
            var result = await CallToolAsync(toolName, argsDict);
            return new { content = result.Content, isError = result.IsError };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.Delay(5, cancellationToken); // Simulate network delay
    }

    public IAsyncDisposable RegisterNotificationHandler(string method,
        Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
    {
        // Mock implementation - in real scenario this would register handlers
        // For testing purposes, we don't need to actually store or use these handlers
        return new MockDisposable();
    }

    private class MockDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MockMcpClient));
        }
    }

    private static object CreateInputSchema(Dictionary<string, MockParameter> parameters)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in parameters)
        {
            properties[param.Key] = new
            {
                type = param.Value.Type,
                description = param.Value.Description
            };

            if (param.Value.Required)
            {
                required.Add(param.Key);
            }
        }

        return new
        {
            type = "object",
            properties = properties,
            required = required
        };
    }

    private Dictionary<string, object> SimulateToolExecution(string toolName, Dictionary<string, object> arguments)
    {
        // 特殊处理某些工具以模拟真实行为
        return toolName switch
        {
            "failing_tool" => new Dictionary<string, object>
                { ["error"] = "This tool always fails for testing purposes" },

            "read_file" => new Dictionary<string, object>
            {
                ["content"] = $"Mock file content for path: {arguments.GetValueOrDefault("path", "unknown")}",
                ["size"] = 1024,
                ["last_modified"] = DateTime.UtcNow.ToString("O")
            },

            "write_file" => new Dictionary<string, object>
            {
                ["success"] = true,
                ["bytes_written"] = arguments.GetValueOrDefault("content", "")?.ToString()?.Length ?? 0
            },

            "list_directory" => new Dictionary<string, object>
            {
                ["files"] = new[] { "file1.txt", "file2.json", "subdirectory/" },
                ["total_count"] = 3
            },

            "execute_query" => new Dictionary<string, object>
            {
                ["rows"] = new[]
                {
                    new { id = 1, name = "Test User", email = "test@example.com" },
                    new { id = 2, name = "Another User", email = "another@example.com" }
                },
                ["affected_rows"] = 2
            },

            "list_tables" => new Dictionary<string, object>
            {
                ["tables"] = new[] { "users", "products", "orders" }
            },

            _ => new Dictionary<string, object>
            {
                ["result"] = $"Mock result for {toolName}",
                ["arguments"] = arguments,
                ["server"] = _serverName,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };
    }
} 