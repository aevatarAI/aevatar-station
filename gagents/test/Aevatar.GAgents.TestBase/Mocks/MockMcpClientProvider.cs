using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.MCP.McpClient;
using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.Test.Mocks;

/// <summary>
/// Mock MCP client provider for testing without real MCP servers
/// 模拟MCP客户端提供者，用于测试而无需真实的MCP服务器
/// </summary>
public class MockMcpClientProvider : IMcpClientProvider
{
    private readonly Dictionary<string, MockMcpClient> _clients = new();
    private readonly Dictionary<string, List<MockTool>> _serverTools = new();

    public McpClientType ClientType => McpClientType.Stdio;

    public MockMcpClientProvider()
    {
        InitializeMockServerTools();
    }

    public async Task<IMcpClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        Console.WriteLine($"[MockMcpClientProvider] GetOrCreateClientAsync called for server '{config.ServerName}'");
        Console.WriteLine($"[MockMcpClientProvider] Available servers: {string.Join(", ", _serverTools.Keys)}");

        if (_clients.TryGetValue(config.ServerName, out var existingClient))
        {
            Console.WriteLine($"[MockMcpClientProvider] Returning existing client for '{config.ServerName}'");
            return existingClient;
        }

        var tools = _serverTools.GetValueOrDefault(config.ServerName, new List<MockTool>());
        Console.WriteLine(
            $"[MockMcpClientProvider] Creating new client for '{config.ServerName}' with {tools.Count} tools");

        var mockClient = new MockMcpClient(config.ServerName, tools);
        _clients[config.ServerName] = mockClient;

        // 模拟连接延迟
        await Task.Delay(100);

        return mockClient;
    }

    public async Task DisconnectClientAsync(string serverName)
    {
        if (_clients.TryGetValue(serverName, out var client))
        {
            await client.DisposeAsync();
            _clients.Remove(serverName);
        }
    }

    public async Task<bool> IsConnectedAsync(string serverName)
    {
        if (!_clients.TryGetValue(serverName, out var client))
            return false;

        return client.IsConnected;
    }

    /// <summary>
    /// 初始化模拟服务器工具
    /// </summary>
    private void InitializeMockServerTools()
    {
        // Filesystem server tools
        _serverTools["filesystem"] = new List<MockTool>
        {
            new("read_file", "Read contents of a file", new Dictionary<string, MockParameter>
            {
                ["path"] = new("string", "File path to read", true)
            }),
            new("write_file", "Write contents to a file", new Dictionary<string, MockParameter>
            {
                ["path"] = new("string", "File path to write", true),
                ["content"] = new("string", "Content to write", true)
            }),
            new("list_directory", "List directory contents", new Dictionary<string, MockParameter>
            {
                ["path"] = new("string", "Directory path to list", true)
            })
        };

        // SQLite server tools
        _serverTools["sqlite"] = new List<MockTool>
        {
            new("execute_query", "Execute SQL query", new Dictionary<string, MockParameter>
            {
                ["query"] = new("string", "SQL query to execute", true)
            }),
            new("list_tables", "List all tables in database", new Dictionary<string, MockParameter>()),
            new("describe_table", "Describe table structure", new Dictionary<string, MockParameter>
            {
                ["table"] = new("string", "Table name", true)
            })
        };

        // 添加一个用于测试错误情况的服务器
        _serverTools["error-server"] = new List<MockTool>
        {
            new("failing_tool", "A tool that always fails", new Dictionary<string, MockParameter>
            {
                ["input"] = new("string", "Any input", false)
            })
        };
    }

    /// <summary>
    /// 添加自定义工具到指定服务器（用于测试特殊情况）
    /// </summary>
    public void AddToolToServer(string serverName, MockTool tool)
    {
        if (!_serverTools.ContainsKey(serverName))
        {
            _serverTools[serverName] = new List<MockTool>();
        }

        _serverTools[serverName].Add(tool);
    }

    /// <summary>
    /// 清除指定服务器的所有工具（用于测试空服务器情况）
    /// </summary>
    public void ClearServerTools(string serverName)
    {
        if (_serverTools.ContainsKey(serverName))
        {
            _serverTools[serverName].Clear();
        }
    }
}

/// <summary>
/// Mock tool definition
/// </summary>
public record MockTool(string Name, string Description, Dictionary<string, MockParameter> Parameters);

/// <summary>
/// Mock parameter definition
/// </summary>
public record MockParameter(string Type, string Description, bool Required, object? DefaultValue = null); 