using System;
using System.Threading.Tasks;
using Orleans;

namespace Aevatar.Silo.Tests.GrainWarmup.TestGrains;

/// <summary>
/// Test grain interface for warmup validation
/// Uses GUID as the grain key (current system support)
/// </summary>
public interface ITestWarmupGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Simple ping method to verify grain activation
    /// </summary>
    /// <returns>Pong response with activation timestamp</returns>
    Task<string> PingAsync();
    
    /// <summary>
    /// Gets the grain activation timestamp
    /// </summary>
    /// <returns>DateTime when grain was activated</returns>
    Task<DateTime> GetActivationTimeAsync();
    
    /// <summary>
    /// Performs a simple computation to simulate grain work
    /// </summary>
    /// <param name="input">Input value for computation</param>
    /// <returns>Computed result</returns>
    Task<int> ComputeAsync(int input);
    
    /// <summary>
    /// Gets the number of times this grain has been accessed
    /// </summary>
    /// <returns>Access count</returns>
    Task<int> GetAccessCountAsync();
    
    /// <summary>
    /// Simulates a database operation with configurable delay
    /// </summary>
    /// <param name="delayMs">Delay in milliseconds</param>
    /// <returns>Operation result</returns>
    Task<string> SimulateDatabaseOperationAsync(int delayMs = 100);
    
    /// <summary>
    /// Gets grain metadata for testing purposes
    /// </summary>
    /// <returns>Grain metadata</returns>
    Task<GrainMetadata> GetMetadataAsync();
}

/// <summary>
/// Metadata about the test grain
/// </summary>
[GenerateSerializer]
public class GrainMetadata
{
    [Id(0)] public Guid GrainId { get; set; }
    [Id(1)] public DateTime ActivationTime { get; set; }
    [Id(2)] public int AccessCount { get; set; }
    [Id(3)] public string SiloAddress { get; set; } = string.Empty;
    [Id(4)] public bool IsWarmedUp { get; set; }
} 