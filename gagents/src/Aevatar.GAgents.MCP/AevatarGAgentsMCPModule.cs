using Aevatar.GAgents.MCP.McpClient;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP;

public class AevatarGAgentsMCPModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IMcpClientProvider, StdioMcpClientProvider>();
        context.Services.AddTransient<IMcpClientProvider, SseMcpClientProvider>();
    }
}