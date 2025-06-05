using Orleans;

namespace E2E.Grains;

/// <summary>
/// Test grain interface for warmup validation
/// Provides methods to test grain activation and access patterns
/// </summary>
public interface ITestWarmupGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Simple ping method to verify grain responsiveness
    /// </summary>
    Task<string> PingAsync();
    
    /// <summary>
    /// Returns the time when this grain was activated
    /// </summary>
    Task<DateTime> GetActivationTimeAsync();
    
    /// <summary>
    /// Performs a computation operation for testing
    /// </summary>
    Task<int> ComputeAsync(int input);
    
    /// <summary>
    /// Returns the number of times this grain has been accessed
    /// </summary>
    Task<int> GetAccessCountAsync();
    
    /// <summary>
    /// Simulates a database operation with configurable delay
    /// </summary>
    Task<string> SimulateDatabaseOperationAsync(int delayMs = 100);
    
    /// <summary>
    /// Returns comprehensive metadata about this grain instance
    /// </summary>
    Task<GrainMetadata> GetMetadataAsync();
}

/// <summary>
/// Metadata information about a grain instance
/// </summary>
[GenerateSerializer]
public class GrainMetadata
{
    [Id(0)]
    public Guid GrainId { get; set; }
    
    [Id(1)]
    public DateTime ActivationTime { get; set; }
    
    [Id(2)]
    public int AccessCount { get; set; }
    
    [Id(3)]
    public string SiloAddress { get; set; } = string.Empty;
    
    [Id(4)]
    public bool IsWarmedUp { get; set; }
} 