using System.Reflection.Emit;
using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.McpClient;

public abstract class McpClientProviderBase : IMcpClientProvider
{
    private readonly Dictionary<string, IMcpClient> _clients = new();

    public abstract McpClientType ClientType { get; }

    public async Task<IMcpClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        if (_clients.TryGetValue(config.ServerName, out var existingClient))
        {
            return existingClient;
        }

        var clientTransport = CreateClientTransport(config);
        var client = await McpClientFactory.CreateAsync(clientTransport);
        _clients[config.ServerName] = client;

        return client;
    }

    protected abstract IClientTransport CreateClientTransport(MCPServerConfig config);

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
        if (!_clients.TryGetValue(serverName, out var client)) return false;

        try
        {
            await client.PingAsync();
        }
        catch
        {
            return false;
        }

        return true;
    }
}