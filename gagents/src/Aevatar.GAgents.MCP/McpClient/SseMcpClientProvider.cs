using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.McpClient;

public class SseMcpClientProvider : McpClientProviderBase
{
    public override McpClientType ClientType => McpClientType.Sse;

    protected override IClientTransport CreateClientTransport(MCPServerConfig config)
    {
        return new SseClientTransport(new SseClientTransportOptions
        {
            Name = config.ServerName,
            Endpoint = new Uri(config.Url!)
        });
    }
}