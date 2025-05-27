using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace Aevatar;

public abstract class AevatarOrleansTestBase<TStartupModule> :
    AevatarTestBase<TStartupModule> where TStartupModule : IAbpModule
{

    protected readonly TestCluster Cluster;

    protected AevatarOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}