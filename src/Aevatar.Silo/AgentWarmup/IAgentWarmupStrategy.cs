using Orleans;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Base interface for agent warmup strategies (non-generic for service compatibility)
/// </summary>
public interface IAgentWarmupStrategy
{
    /// <summary>
    /// Gets the name of this warmup strategy
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the agent type that this strategy applies to
    /// </summary>
    Type AgentType { get; }
    
    /// <summary>
    /// Gets the priority for execution order (higher = earlier)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets the estimated number of agents that will be warmed up
    /// </summary>
    int EstimatedAgentCount { get; }
    
    /// <summary>
    /// Generates the agent identifiers to warm up (non-generic version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of agent identifiers as objects</returns>
    IAsyncEnumerable<object> GenerateAgentIdentifiersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a agent reference for the given identifier (generic version)
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="identifier">The agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    T CreateAgentReference<T>(IGrainFactory agentFactory, object identifier) where T : IGrainBase;
    
    /// <summary>
    /// Validates that the strategy configuration is correct
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValid();
    
    /// <summary>
    /// Checks if strategy applies to a agent type
    /// </summary>
    /// <param name="agentType">The agent type to check</param>
    /// <returns>True if the strategy applies to this agent type</returns>
    bool AppliesTo(Type agentType);
}

/// <summary>
/// Generic interface for agent warmup strategies with strong typing for identifiers
/// </summary>
/// <typeparam name="TIdentifier">The type of agent identifier (Guid, string, int, long)</typeparam>
public interface IAgentWarmupStrategy<TIdentifier> : IAgentWarmupStrategy
{
    /// <summary>
    /// Generates the agent identifiers to warm up (strongly typed version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed agent identifiers</returns>
    new IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates the agent identifiers to warm up for a specific agent type
    /// </summary>
    /// <param name="agentType">The agent type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed agent identifiers</returns>
    IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(Type agentType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a agent reference for the given identifier (strongly typed version)
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="identifier">The strongly typed agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    T CreateAgentReference<T>(IGrainFactory agentFactory, TIdentifier identifier) where T : IGrainBase;
    
    /// <summary>
    /// Creates a agent reference for a specific agent type and identifier
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="agentType">The agent type</param>
    /// <param name="identifier">The agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    T CreateAgentReference<T>(IGrainFactory agentFactory, Type agentType, TIdentifier identifier) where T : IGrainBase;
} 