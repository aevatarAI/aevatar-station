// ABOUTME: This file defines the test base class for MetaData integration tests
// ABOUTME: Provides Orleans cluster integration for testing IMetaDataStateGAgent implementations

using Aevatar.TestBase;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Base class for Orleans-based MetaData integration tests.
/// Provides access to Orleans cluster and test infrastructure.
/// </summary>
public abstract class AevatarMetaDataTestBase : AevatarTestBase<AevatarTestBaseModule>
{
    // This provides access to:
    // - TestCluster Cluster (from base class)
    // - GetRequiredService<T>() for getting Orleans services
    // - Orleans cluster with all configured grains and services
    
    /// <summary>
    /// Gets a grain from the Orleans cluster for testing.
    /// </summary>
    /// <typeparam name="T">The grain interface type</typeparam>
    /// <param name="key">The grain key</param>
    /// <returns>The grain instance</returns>
    protected Task<T> GetGrainAsync<T>(Guid key) where T : IGrainWithGuidKey
    {
        return Task.FromResult(Cluster.GrainFactory.GetGrain<T>(key));
    }
}