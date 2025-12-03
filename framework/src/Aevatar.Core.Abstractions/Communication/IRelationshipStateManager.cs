namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for managing parent-child relationship state operations.
/// Handles low-level add/remove operations for hierarchical agent structures.
/// </summary>
public interface IRelationshipStateManager
{
    /// <summary>
    /// Gets the list of all child agents.
    /// </summary>
    /// <returns>List of grain IDs of child agents</returns>
    Task<List<GrainId>> GetChildrenAsync();

    /// <summary>
    /// Gets the list of all parent agents.
    /// </summary>
    /// <returns>List of grain IDs of parent agents</returns>
    Task<List<GrainId>> GetParentsAsync();

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
    /// Adds a parent to the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the parent to add</param>
    /// <returns>Task representing the async operation</returns>
    Task AddParentAsync(GrainId grainId);

    /// <summary>
    /// Removes a parent relationship from the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the parent to remove</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveParentAsync(GrainId grainId);

    /// <summary>
    /// Validates the relationship integrity for a specific grain.
    /// </summary>
    /// <param name="grainId">The grain ID to validate relationships for</param>
    /// <returns>Task representing the async operation</returns>
    Task ValidateRelationshipAsync(GrainId grainId);
}
