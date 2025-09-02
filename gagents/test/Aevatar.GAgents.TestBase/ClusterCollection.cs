namespace Aevatar.GAgents.TestBase;

/// <summary>
/// xUnit collection definition for Orleans cluster tests.
/// This ensures that all tests in this collection share the same Orleans cluster instance
/// and run sequentially rather than in parallel.
/// </summary>
[CollectionDefinition(Name)]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    /// <summary>
    /// The name of the test collection
    /// </summary>
    public const string Name = "ClusterCollection";
}