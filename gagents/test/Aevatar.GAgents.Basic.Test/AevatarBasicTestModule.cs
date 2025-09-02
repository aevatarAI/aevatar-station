using Aevatar.GAgents.Common;
using Aevatar.GAgents.TestBase;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Basic.Test;

[DependsOn(
    typeof(AevatarGAgentsCommonModule),
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarBasicTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure test-specific services here
    }
}