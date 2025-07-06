// ABOUTME: This file defines the IMetaDataState interface for agent metadata management
// ABOUTME: Provides foundation interface with default Apply method for event sourcing

using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData;

/// <summary>
/// Interface for agent metadata state management.
/// Provides properties and behavior for tracking agent metadata and applying events.
/// </summary>
public interface IMetaDataState
{
    /// <summary>
    /// The unique identifier for the agent.
    /// </summary>
    Guid Id { get; set; }
    
    /// <summary>
    /// The user ID that owns this agent.
    /// </summary>
    Guid UserId { get; set; }
    
    /// <summary>
    /// The type of agent (e.g., "CreatorAgent", "ChatAgent").
    /// </summary>
    string AgentType { get; set; }
    
    /// <summary>
    /// The display name of the agent.
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Key-value properties for the agent.
    /// </summary>
    Dictionary<string, string> Properties { get; set; }
    
    /// <summary>
    /// The Orleans grain ID for the agent.
    /// </summary>
    GrainId AgentGrainId { get; set; }
    
    /// <summary>
    /// The timestamp when the agent was created.
    /// </summary>
    DateTime CreateTime { get; set; }
    
    /// <summary>
    /// The current status of the agent.
    /// </summary>
    AgentStatus Status { get; set; }
    
    /// <summary>
    /// The timestamp of the last activity.
    /// </summary>
    DateTime LastActivity { get; set; }
    
    /// <summary>
    /// Default Apply method implementation using .NET 8+ default interface methods.
    /// Applies metadata state events to update the state.
    /// </summary>
    /// <param name="event">The metadata state event to apply.</param>
    void Apply(MetaDataStateLogEvent @event)
    {
        // Update common properties
        LastActivity = DateTime.UtcNow;
        
        // Apply specific event types
        switch (@event)
        {
            case AgentCreatedEvent createdEvent:
                ApplyAgentCreatedEvent(createdEvent);
                break;
                
            case AgentStatusChangedEvent statusEvent:
                ApplyAgentStatusChangedEvent(statusEvent);
                break;
                
            case AgentPropertiesUpdatedEvent propertiesEvent:
                ApplyAgentPropertiesUpdatedEvent(propertiesEvent);
                break;
                
            case AgentActivityUpdatedEvent activityEvent:
                ApplyAgentActivityUpdatedEvent(activityEvent);
                break;
        }
    }
    
    /// <summary>
    /// Applies an agent created event to the state.
    /// </summary>
    /// <param name="createdEvent">The agent created event.</param>
    protected void ApplyAgentCreatedEvent(AgentCreatedEvent createdEvent)
    {
        Id = createdEvent.AgentId;
        UserId = createdEvent.UserId;
        Name = createdEvent.Name;
        AgentType = createdEvent.AgentType;
        Properties = new Dictionary<string, string>(createdEvent.Properties ?? new Dictionary<string, string>());
        AgentGrainId = createdEvent.AgentGrainId;
        Status = createdEvent.InitialStatus;
        CreateTime = createdEvent.Ctime;
        LastActivity = createdEvent.Ctime;
    }
    
    /// <summary>
    /// Applies an agent status changed event to the state.
    /// </summary>
    /// <param name="statusEvent">The agent status changed event.</param>
    protected void ApplyAgentStatusChangedEvent(AgentStatusChangedEvent statusEvent)
    {
        Status = statusEvent.NewStatus;
        LastActivity = statusEvent.StatusChangeTime;
    }
    
    /// <summary>
    /// Applies an agent properties updated event to the state.
    /// </summary>
    /// <param name="propertiesEvent">The agent properties updated event.</param>
    protected void ApplyAgentPropertiesUpdatedEvent(AgentPropertiesUpdatedEvent propertiesEvent)
    {
        // Remove properties first
        foreach (var removedProperty in propertiesEvent.RemovedProperties)
        {
            Properties.Remove(removedProperty);
        }
        
        // Add or update properties
        foreach (var (key, value) in propertiesEvent.UpdatedProperties)
        {
            Properties[key] = value;
        }
        
        LastActivity = propertiesEvent.UpdateTime;
    }
    
    /// <summary>
    /// Applies an agent activity updated event to the state.
    /// </summary>
    /// <param name="activityEvent">The agent activity updated event.</param>
    protected void ApplyAgentActivityUpdatedEvent(AgentActivityUpdatedEvent activityEvent)
    {
        LastActivity = activityEvent.ActivityTime;
    }
}