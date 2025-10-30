using Volo.Abp.Modularity;
using Aevatar.GAgents.TestBase;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarGAgentsCoreTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Core test specific configurations
        // Note: Orleans cluster and MongoDB are handled by AevatarGAgentTestBaseModule
    }
}
