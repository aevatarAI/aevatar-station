namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for managing subscription operations between parent and child agents.
/// Handles subscribe/unsubscribe operations for parent-child communication.
/// </summary>
public interface IRelationshipSubscriptionManager
{
    /// <summary>
    /// Subscribes to a parent agent.
    /// </summary>
    /// <param name="gAgent">The parent agent to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeToParentAsync(IGAgentPlus gAgent);

    /// <summary>
    /// Unsubscribes from a parent agent.
    /// </summary>
    /// <param name="gAgent">The parent agent to unsubscribe from</param>
    /// <returns>Task representing the async operation</returns>
    Task UnsubscribeFromParentAsync(IGAgentPlus gAgent);

    /// <summary>
    /// Subscribe to child's upward streams for bidirectional communication.
    /// </summary>
    /// <param name="childAgent">The child agent to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeToChildAsync(IGAgentPlus childAgent);

    /// <summary>
    /// Unsubscribe from child's upward streams.
    /// </summary>
    /// <param name="childAgent">The child agent to unsubscribe from</param>
    /// <returns>Task representing the async operation</returns>
    Task UnsubscribeFromChildAsync(IGAgentPlus childAgent);

    /// <summary>
    /// Subscribe to many children's upward streams using batch pattern.
    /// </summary>
    /// <param name="childAgents">The child agents to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeToManyChildAsync(List<IGAgentPlus> childAgents);

    /// <summary>
    /// Subscribe to many parents' downward streams using batch pattern to receive downward events from the parents.
    /// </summary>
    /// <param name="parentAgents">The parent agents to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeToManyParentAsync(List<IGAgentPlus> parentAgents);
}
