using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.McpClient;

public interface IMcpClientProvider
{
    McpClientType ClientType { get; }
    Task<IMcpClient> GetOrCreateClientAsync(MCPServerConfig config);
    Task DisconnectClientAsync(string serverName);
    Task<bool> IsConnectedAsync(string serverName);
}

public enum McpClientType
{
    Stdio,
    Sse,
    // Included to Sse
    // Http
}