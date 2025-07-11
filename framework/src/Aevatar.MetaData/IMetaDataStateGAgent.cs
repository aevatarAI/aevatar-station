// ABOUTME: This file defines the IMetaDataStateGAgent helper interface for common event patterns
// ABOUTME: Provides default method implementations to reduce boilerplate in agent development

using Aevatar.Core.Abstractions;
using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData;

/// <summary>
/// Provides default implementations for common event-raising operations on metadata state.
/// This interface works alongside GAgentBase to reduce boilerplate code when raising common metadata events.
/// This is a helper interface that agents can implement alongside their GAgentBase inheritance.
/// </summary>
/// <typeparam name="TState">The state type that implements IMetaDataState</typeparam>
public interface IMetaDataStateGAgent<TState> where TState : IMetaDataState
{
    /// <summary>
    /// Raises an event to be applied to the state.
    /// </summary>
    /// <param name="event">The event to raise</param>
    void RaiseEvent(MetaDataStateLogEvent @event);
    
    /// <summary>
    /// Confirms and persists all raised events.
    /// </summary>
    /// <returns>Task representing the confirmation operation</returns>
    Task ConfirmEvents();
    
    /// <summary>
    /// Gets the current state instance.
    /// </summary>
    /// <returns>The current state</returns>
    TState GetState();
    
    /// <summary>
    /// Gets the grain ID of the current agent.
    /// </summary>
    /// <returns>The grain ID</returns>
    GrainId GetGrainId();
    
    /// <summary>
    /// Creates a new agent with the specified metadata.
    /// </summary>
    /// <param name="id">The unique identifier for the agent</param>
    /// <param name="userId">The user ID that owns the agent</param>
    /// <param name="name">The display name of the agent</param>
    /// <param name="agentType">The type of agent</param>
    /// <param name="properties">Optional initial properties for the agent</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task CreateAgentAsync(
        Guid id, 
        Guid userId, 
        string name, 
        string agentType, 
        Dictionary<string, string>? properties = null)
    {
        var @event = new AgentCreatedEvent
        {
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
    /// Updates the agent's status with an optional reason.
    /// </summary>
    /// <param name="newStatus">The new status to set</param>
    /// <param name="reason">Optional reason for the status change</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task UpdateStatusAsync(AgentStatus newStatus, string? reason = null)
    {
        var @event = new AgentStatusChangedEvent
        {
            AgentId = GetState().Id,
            UserId = GetState().UserId,
            OldStatus = GetState().Status,
            NewStatus = newStatus,
            Reason = reason,
            StatusChangeTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Updates the agent's properties, with option to merge or replace.
    /// </summary>
    /// <param name="properties">The properties to update</param>
    /// <param name="merge">Whether to merge with existing properties (true) or replace them (false)</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task UpdatePropertiesAsync(
        Dictionary<string, string> properties, 
        bool merge = true)
    {
        var @event = new AgentPropertiesUpdatedEvent
        {
            AgentId = GetState().Id,
            UserId = GetState().UserId,
            UpdatedProperties = merge ? properties : properties,
            RemovedProperties = merge ? new List<string>() : GetState().Properties.Keys.Except(properties.Keys).ToList(),
            WasMerged = merge,
            UpdateTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Records activity for the agent, updating the last activity timestamp.
    /// </summary>
    /// <param name="activityType">Optional type of activity being recorded</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task RecordActivityAsync(string? activityType = null)
    {
        var @event = new AgentActivityUpdatedEvent
        {
            AgentId = GetState().Id,
            UserId = GetState().UserId,
            ActivityType = activityType ?? string.Empty,
            ActivityTime = DateTime.UtcNow
        };
        
        RaiseEvent(@event);
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Updates a single property value.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task SetPropertyAsync(string key, string value)
    {
        await UpdatePropertiesAsync(
            new Dictionary<string, string> { [key] = value }, 
            merge: true);
    }
    
    /// <summary>
    /// Removes a property from the agent's metadata.
    /// </summary>
    /// <param name="key">The property key to remove</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task RemovePropertyAsync(string key)
    {
        var currentProps = new Dictionary<string, string>(GetState().Properties);
        currentProps.Remove(key);
        await UpdatePropertiesAsync(currentProps, merge: false);
    }
    
    /// <summary>
    /// Batch updates multiple aspects of the agent in a single operation.
    /// </summary>
    /// <param name="newStatus">Optional new status to set</param>
    /// <param name="properties">Optional properties to update</param>
    /// <param name="mergeProperties">Whether to merge properties with existing ones</param>
    /// <param name="statusReason">Optional reason for status change</param>
    /// <returns>Task representing the asynchronous operation</returns>
    async Task BatchUpdateAsync(
        AgentStatus? newStatus = null,
        Dictionary<string, string>? properties = null,
        bool mergeProperties = true,
        string? statusReason = null)
    {
        if (newStatus.HasValue)
        {
            var statusEvent = new AgentStatusChangedEvent
            {
                AgentId = GetState().Id,
                UserId = GetState().UserId,
                OldStatus = GetState().Status,
                NewStatus = newStatus.Value,
                Reason = statusReason,
                StatusChangeTime = DateTime.UtcNow
            };
            RaiseEvent(statusEvent);
        }
        
        if (properties != null && properties.Any())
        {
            var propsEvent = new AgentPropertiesUpdatedEvent
            {
                AgentId = GetState().Id,
                UserId = GetState().UserId,
                UpdatedProperties = mergeProperties ? properties : properties,
                RemovedProperties = mergeProperties ? new List<string>() : GetState().Properties.Keys.Except(properties.Keys).ToList(),
                WasMerged = mergeProperties,
                UpdateTime = DateTime.UtcNow
            };
            RaiseEvent(propsEvent);
        }
        
        await ConfirmEvents();
    }
}