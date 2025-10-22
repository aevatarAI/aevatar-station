using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for core agent functionality without layered communication.
/// Provides basic agent operations like activation, configuration, and event subscription discovery.
/// </summary>
public interface ICoreGAgent : IGAgentBase
{
    /// <summary>
    /// Get the grain ID of the agent.
    /// </summary>
    /// <returns>The grain ID of the agent</returns>
    Task<Guid> SendEventToAgentAsync<T>(T @event, GrainId targetGrainId) where T : EventBase;
    
    /// <summary>
    /// Get the current state as JSON string for snapshot purposes.
    /// Returns null if the GAgent does not implement state interface.
    /// </summary>
    /// <returns>JSON representation of the current state, or null if not stateful</returns>
    [ReadOnly]
    Task<string?> GetStateSnapshotAsync();
} 