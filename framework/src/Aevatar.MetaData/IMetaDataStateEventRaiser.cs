// ABOUTME: This file defines the IMetaDataStateEventRaiser helper interface for common event patterns
// ABOUTME: Provides default method implementations to reduce boilerplate in agent development

using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData;

/// <summary>
/// Helper interface that provides default method implementations for common metadata event-raising patterns.
/// This interface is designed to work alongside GAgentBase without requiring modifications to it.
/// </summary>
/// <typeparam name="TState">The state type that implements IMetaDataState.</typeparam>
public interface IMetaDataStateEventRaiser<TState> where TState : IMetaDataState
{
    /// <summary>
    /// Required method that implementing class must provide to raise events.
    /// </summary>
    /// <param name="event">The event to raise.</param>
    void RaiseEvent(MetaDataStateLogEvent @event);
    
    /// <summary>
    /// Required method that implementing class must provide to confirm events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConfirmEvents();
    
    /// <summary>
    /// Required method that implementing class must provide to get the current state.
    /// </summary>
    /// <returns>The current state.</returns>
    TState GetState();
    
    /// <summary>
    /// Required method that implementing class must provide to get the grain ID.
    /// </summary>
    /// <returns>The grain ID.</returns>
    GrainId GetGrainId();
    
    /// <summary>
    /// Creates a new agent with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the agent.</param>
    /// <param name="userId">The user ID that owns the agent.</param>
    /// <param name="name">The display name of the agent.</param>
    /// <param name="agentType">The type of agent.</param>
    /// <param name="properties">Optional initial properties for the agent.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task CreateAgentAsync(Guid id, Guid userId, string name, string agentType, Dictionary<string, string>? properties = null)
    {
        var @event = new AgentCreatedEvent
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = id,
            UserId = userId,
            Name = name,
            AgentType = agentType,
            Properties = properties ?? new Dictionary<string, string>(),
            AgentGrainId = GetGrainId(),
            InitialStatus = AgentStatus.Creating
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Updates the status of the agent.
    /// </summary>
    /// <param name="newStatus">The new status for the agent.</param>
    /// <param name="reason">Optional reason for the status change.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task UpdateStatusAsync(AgentStatus newStatus, string? reason = null)
    {
        var currentState = GetState();
        var @event = new AgentStatusChangedEvent
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = currentState.Id,
            UserId = currentState.UserId,
            OldStatus = currentState.Status,
            NewStatus = newStatus,
            StatusChangeTime = DateTime.UtcNow,
            Reason = reason
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Updates the properties of the agent.
    /// </summary>
    /// <param name="properties">The properties to update.</param>
    /// <param name="merge">Whether to merge with existing properties (true) or replace them (false).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task UpdatePropertiesAsync(Dictionary<string, string> properties, bool merge = true)
    {
        var currentState = GetState();
        var @event = new AgentPropertiesUpdatedEvent
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = currentState.Id,
            UserId = currentState.UserId,
            UpdatedProperties = properties,
            RemovedProperties = new List<string>(),
            WasMerged = merge,
            UpdateTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Records activity for the agent.
    /// </summary>
    /// <param name="activityType">Optional type of activity that occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task RecordActivityAsync(string? activityType = null)
    {
        var currentState = GetState();
        var @event = new AgentActivityUpdatedEvent
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = currentState.Id,
            UserId = currentState.UserId,
            ActivityType = activityType ?? "activity",
            ActivityTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Sets a single property on the agent.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task SetPropertyAsync(string key, string value)
    {
        var properties = new Dictionary<string, string> { { key, value } };
        await UpdatePropertiesAsync(properties, merge: true);
    }
    
    /// <summary>
    /// Removes a property from the agent.
    /// </summary>
    /// <param name="key">The property key to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task RemovePropertyAsync(string key)
    {
        var currentState = GetState();
        var @event = new AgentPropertiesUpdatedEvent
        {
            Id = Guid.NewGuid(),
            Ctime = DateTime.UtcNow,
            AgentId = currentState.Id,
            UserId = currentState.UserId,
            UpdatedProperties = new Dictionary<string, string>(),
            RemovedProperties = new List<string> { key },
            WasMerged = true,
            UpdateTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Performs a batch update of multiple agent properties in a single operation.
    /// </summary>
    /// <param name="newStatus">Optional new status for the agent.</param>
    /// <param name="properties">Optional properties to update.</param>
    /// <param name="mergeProperties">Whether to merge properties with existing ones.</param>
    /// <param name="statusReason">Optional reason for status change.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task BatchUpdateAsync(AgentStatus? newStatus = null, Dictionary<string, string>? properties = null, bool mergeProperties = true, string? statusReason = null)
    {
        if (newStatus.HasValue)
        {
            await UpdateStatusAsync(newStatus.Value, statusReason);
        }
        
        if (properties != null && properties.Count > 0)
        {
            await UpdatePropertiesAsync(properties, mergeProperties);
        }
        
        // Record activity for the batch update
        await RecordActivityAsync("batch_update");
    }
}