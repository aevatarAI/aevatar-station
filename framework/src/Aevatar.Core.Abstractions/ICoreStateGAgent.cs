using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for core agent functionality with state access.
/// Extends ICoreGAgent to provide state retrieval capabilities.
/// </summary>
/// <typeparam name="TState">The state type that inherits from CoreStateBase</typeparam>
public interface ICoreStateGAgent<TState> : ICoreGAgent 
{
    /// <summary>
    /// Gets the current state of the agent.
    /// </summary>
    /// <returns>The current state object</returns>
    [ReadOnly]
    Task<TState> GetStateAsync();
} 