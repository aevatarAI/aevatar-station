using Xunit;

namespace Aevatar.GAgents.TestBase;

public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}
