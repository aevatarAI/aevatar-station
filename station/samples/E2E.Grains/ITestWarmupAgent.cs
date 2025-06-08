using Orleans;

namespace E2E.Grains;

/// <summary>
/// Test agent interface for warmup validation
/// Uses GUID as the agent key (current system support)
/// </summary>
public interface ITestWarmupAgent : IGrainWithGuidKey
{
    /// <summary>
    /// Simple ping method to verify agent activation
    /// </summary>
    /// <returns>Pong response with activation timestamp</returns>
    Task<string> PingAsync();
    
    /// <summary>
    /// Gets the agent activation timestamp
    /// </summary>
    /// <returns>DateTime when agent was activated</returns>
    Task<DateTime> GetActivationTimeAsync();
    
    /// <summary>
    /// Performs a simple computation to simulate agent work
    /// </summary>
    /// <param name="input">Input value for computation</param>
    /// <returns>Computed result</returns>
    Task<int> ComputeAsync(int input);
    
    /// <summary>
    /// Gets the number of times this agent has been accessed
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
    /// Gets agent metadata for testing purposes
    /// </summary>
    /// <returns>Agent metadata</returns>
    Task<AgentMetadata> GetMetadataAsync();
}

/// <summary>
/// Metadata about the test agent
/// </summary>
[GenerateSerializer]
public class AgentMetadata
{
    [Id(0)] public Guid AgentId { get; set; }
    [Id(1)] public DateTime ActivationTime { get; set; }
    [Id(2)] public int AccessCount { get; set; }
    [Id(3)] public string SiloAddress { get; set; } = string.Empty;
    [Id(4)] public bool IsWarmedUp { get; set; }
} 