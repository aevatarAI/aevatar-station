namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for managing parent-child relationships in layered communication patterns.
/// Handles relationship operations and state persistence for hierarchical agent structures.
/// </summary>
public interface ILayeredRelationshipManager
{
    /// <summary>
    /// Registers a single child agent and establishes the relationship.
    /// </summary>
    /// <param name="gAgent">The child agent to register</param>
    /// <returns>Task representing the async operation</returns>
    Task RegisterAsync(IGAgent gAgent);

    /// <summary>
    /// Registers multiple child agents in a batch operation.
    /// </summary>
    /// <param name="gAgents">List of agents to register</param>
    /// <returns>Task representing the async operation</returns>
    Task RegisterManyAsync(List<IGAgent> gAgents);

    /// <summary>
    /// Unregisters a child agent and removes the relationship.
    /// </summary>
    /// <param name="gAgent">The child agent to unregister</param>
    /// <returns>Task representing the async operation</returns>
    Task UnregisterAsync(IGAgent gAgent);

    /// <summary>
    /// Subscribes to a parent agent.
    /// </summary>
    /// <param name="gAgent">The parent agent to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeToAsync(IGAgent gAgent);

    /// <summary>
    /// Unsubscribes from a parent agent.
    /// </summary>
    /// <param name="gAgent">The parent agent to unsubscribe from</param>
    /// <returns>Task representing the async operation</returns>
    Task UnsubscribeFromAsync(IGAgent gAgent);

    /// <summary>
    /// Gets the list of all child agents.
    /// </summary>
    /// <returns>List of grain IDs of child agents</returns>
    Task<List<GrainId>> GetChildrenAsync();

    /// <summary>
    /// Gets the parent agent ID.
    /// </summary>
    /// <returns>The grain ID of the parent agent</returns>
    Task<GrainId> GetParentAsync();

    /// <summary>
    /// Adds a child to the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the child to add</param>
    /// <returns>Task representing the async operation</returns>
    Task AddChildAsync(GrainId grainId);

    /// <summary>
    /// Adds multiple children to the internal state (low-level operation).
    /// </summary>
    /// <param name="grainIds">List of grain IDs to add as children</param>
    /// <returns>Task representing the async operation</returns>
    Task AddChildManyAsync(List<GrainId> grainIds);

    /// <summary>
    /// Removes a child from the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the child to remove</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveChildAsync(GrainId grainId);

    /// <summary>
    /// Sets the parent in the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the parent to set</param>
    /// <returns>Task representing the async operation</returns>
    Task SetParentAsync(GrainId grainId);

    /// <summary>
    /// Clears the parent relationship in the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the parent to clear</param>
    /// <returns>Task representing the async operation</returns>
    Task ClearParentAsync(GrainId grainId);

    /// <summary>
    /// Validates the relationship integrity for a specific grain.
    /// </summary>
    /// <param name="grainId">The grain ID to validate relationships for</param>
    /// <returns>Task representing the async operation</returns>
    Task ValidateRelationshipAsync(GrainId grainId);
} 