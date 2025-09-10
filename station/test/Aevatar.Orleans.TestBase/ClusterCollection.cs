using Xunit;

namespace Aevatar;

[CollectionDefinition(Name)]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}