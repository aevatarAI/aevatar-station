using Aevatar.GAgents.TestBase;
using Aevatar.GAgents.Twitter;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Twitter.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsTwitterModule)
)]
public class AevatarTwitterTestModule : AbpModule
{
    // No additional configuration needed
    // All Orleans-related services are registered in TestBase's ClusterFixture
}