using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.McpClient;

public class StdioMcpClientProvider : McpClientProviderBase
{
    public override McpClientType ClientType => McpClientType.Stdio;

    protected override IClientTransport CreateClientTransport(MCPServerConfig config)
    {
        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = config.ServerName,
            Command = config.Command,
            Arguments = config.Args,
            EnvironmentVariables = config.Env!
        });
    }
}