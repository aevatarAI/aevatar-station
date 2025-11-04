namespace Aevatar.Core.Abstractions.StateManagement;

/// <summary>
/// Interface for dispatching agent state changes to external systems and projections.
/// Wraps the original state dispatch logic from GAgentBase.
/// </summary>
public interface IStatePublisher
{
    /// <summary>
    /// Dispatches state to configured external systems and projections.
    /// This wraps the original state dispatch logic from GAgentBase.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="state">The state to dispatch</param>
    /// <param name="grainId">The grain ID</param>
    /// <param name="version">The state version</param>
    /// <returns>Task representing the async operation</returns>
    Task DispatchStateAsync<TState>(TState state, GrainId grainId, int version) where TState : CoreStateBase;
} 