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

        // Note: IGAgentExecutor, IGAgentService, IGAgentFactory, and IGAgentManager
        // are already registered in AevatarGAgentTestBaseModule and ClusterFixture.
        // DO NOT register them again here to avoid service resolution conflicts.
    }
}