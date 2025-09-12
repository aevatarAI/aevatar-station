using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.ChatAgent.Test;


[DependsOn(typeof(AevatarGAgentTestBaseModule),
    typeof(AbpBlobStoringModule)
    )]
public class AevatarChatAgentTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();
    }
}