namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Composite interface for managing parent-child relationships in layered communication patterns.
/// Combines state management, subscription management, and high-level orchestration operations.
/// </summary>
public interface ILayeredRelationshipManager : IRelationshipStateManager, IRelationshipSubscriptionManager
{
    /// <summary>
    /// Registers a single child agent and establishes the relationship.
    /// High-level orchestration that combines state management and communication setup.
    /// </summary>
    /// <param name="gAgent">The child agent to register</param>
    /// <returns>Task representing the async operation</returns>
    Task RegisterAsync(IGAgentPlus gAgent);

    /// <summary>
    /// Registers multiple child agents in a batch operation.
    /// High-level orchestration that combines state management and communication setup.
    /// </summary>
    /// <param name="gAgents">List of agents to register</param>
    /// <returns>Task representing the async operation</returns>
    Task RegisterManyAsync(List<IGAgentPlus> gAgents);

    /// <summary>
    /// Unregisters a child agent and removes the relationship.
    /// High-level orchestration that combines state cleanup and communication teardown.
    /// </summary>
    /// <param name="gAgent">The child agent to unregister</param>
    /// <returns>Task representing the async operation</returns>
    Task UnregisterAsync(IGAgentPlus gAgent);

    /// <summary>
    /// Unregisters the current agent from one of its parents.
    /// High-level orchestration that combines state cleanup and communication teardown.
    /// </summary>
    /// <param name="parentAgent">The parent agent to unregister from</param>
    /// <returns>Task representing the async operation</returns>
    Task UnregisterParentAsync(IGAgentPlus parentAgent);
} 