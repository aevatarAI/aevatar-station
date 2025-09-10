using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.McpClient;
using Aevatar.GAgents.MCP.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.TestBase;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AevatarModule)
)]
public class AevatarGAgentTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options => { options.IsEnabled = false; });
        context.Services.AddSingleton<ClusterFixture>();
        context.Services.AddSingleton<IClusterClient>(sp =>
            context.Services.GetRequiredService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IGrainFactory>(sp =>
            context.Services.GetRequiredService<ClusterFixture>().Cluster.GrainFactory);
        context.Services.AddSingleton<IGAgentFactory>(sp =>
            new GAgentFactory(context.Services.GetRequiredService<ClusterFixture>().Cluster.Client));
        context.Services.AddSingleton<IGAgentManager>(sp =>
            new GAgentManager(context.Services.GetRequiredService<ClusterFixture>().Cluster.Client,
                context.Services.GetRequiredService<IPluginGAgentManager>()));
        context.Services.AddSingleton<IGAgentService>(sp =>
            new GAgentService(context.Services.GetRequiredService<IGAgentManager>(),
                context.Services.GetRequiredService<ClusterFixture>().Cluster.Client,
                context.Services.GetRequiredService<ILogger<GAgentService>>()));
        context.Services.AddSingleton<IGAgentExecutor>(sp =>
            new GAgentExecutor(context.Services.GetRequiredService<ClusterFixture>().Cluster.Client,
                context.Services.GetRequiredService<IGAgentService>()));
        
        // 注册Mock MCP客户端提供者用于测试（ABP框架需要）
        context.Services.AddSingleton<IMcpClientProvider, MockMcpClientProvider>();
        context.Services.AddSingleton<MockMcpClientProvider>();
        
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarGAgentTestBaseModule>(); });
    }
}