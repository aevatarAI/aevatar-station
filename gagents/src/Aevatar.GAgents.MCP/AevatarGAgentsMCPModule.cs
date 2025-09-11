using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.McpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP;

public class AevatarGAgentsMCPModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Configure MCP Gateway settings
        context.Services.Configure<MCPGatewayConfig>(
            configuration.GetSection("MCPGateway"));

        // Register MCP client providers
        context.Services.AddTransient<IMcpClientProvider, StdioMcpClientProvider>();
        context.Services.AddTransient<IMcpClientProvider, SseMcpClientProvider>();
        context.Services.AddTransient<IMcpClientProvider, GatewayMcpClientProvider>();

        // Register HttpClient for Gateway provider
        context.Services.AddHttpClient<GatewayMcpClientProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Aevatar-MCP-Gateway-Client/1.0");
        });

        // Add logging configuration for MCP components
        context.Services.AddLogging();
    }
}