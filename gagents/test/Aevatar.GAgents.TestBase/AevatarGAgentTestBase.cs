using Orleans.TestingHost;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace Aevatar.GAgents.TestBase;

public class AevatarGAgentTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    protected AevatarGAgentTestBase() 
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}