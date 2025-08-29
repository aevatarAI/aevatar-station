using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.Modules;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test;


[DependsOn(typeof(AevatarGAgentTestBaseModule),
    typeof(AbpBlobStoringModule),
    typeof(MockBrainTestModule)
)]
public class AevatarAIGAgentTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();

        // Register GAgentExecutor related services
        context.Services.AddSingleton<IGAgentExecutor>(provider =>
        {
            var clusterClient = provider.GetRequiredService<IClusterClient>();
            var gAgentService = provider.GetRequiredService<IGAgentService>();
            return new GAgentExecutor(clusterClient, gAgentService);
        });

        context.Services.AddSingleton<IGAgentService>(provider =>
        {
            var gAgentManager = provider.GetRequiredService<IGAgentManager>();
            var clusterClient = provider.GetRequiredService<IClusterClient>();
            return new GAgentService(gAgentManager, clusterClient, provider.GetRequiredService<ILogger<GAgentService>>());
        });

        context.Services.AddSingleton<IGAgentFactory>(provider =>
        {
            var clusterClient = provider.GetRequiredService<IClusterClient>();
            return new GAgentFactory(clusterClient);
        });
    }
}