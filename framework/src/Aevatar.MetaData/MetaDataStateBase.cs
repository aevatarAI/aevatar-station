// ABOUTME: This file provides a base implementation of IMetaDataState for concrete state classes
// ABOUTME: Contains the default Apply method logic that can be inherited by state implementations

using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData;

/// <summary>
/// Base implementation of IMetaDataState that provides default Apply method behavior.
/// </summary>
[GenerateSerializer]
public abstract class MetaDataStateBase : IMetaDataState
{
    /// <summary>
    /// The unique identifier for the agent.
    /// </summary>
    [Id(0)]
    public virtual Guid Id { get; set; }
    
    /// <summary>
    /// The user ID that owns this agent.
    /// </summary>
    [Id(1)]
    public virtual Guid UserId { get; set; }
    
    /// <summary>
    /// The type of agent (e.g., "CreatorAgent", "ChatAgent").
    /// </summary>
    [Id(2)]
    public virtual string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// The display name of the agent.
    /// </summary>
    [Id(3)]
    public virtual string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Key-value properties for the agent.
    /// </summary>
    [Id(4)]
    public virtual Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// The Orleans grain ID for the agent.
    /// </summary>
    [Id(5)]
    public virtual GrainId AgentGrainId { get; set; }
    
    /// <summary>
    /// The timestamp when the agent was created.
    /// </summary>
    [Id(6)]
    public virtual DateTime CreateTime { get; set; }
    
    /// <summary>
    /// The current status of the agent.
    /// </summary>
    [Id(7)]
    public virtual AgentStatus Status { get; set; }
    
    /// <summary>
    /// The timestamp of the last activity.
    /// </summary>
    [Id(8)]
    public virtual DateTime LastActivity { get; set; }
    
    /// <summary>
    /// Applies metadata state events to update the state.
    /// </summary>
    /// <param name="event">The metadata state event to apply.</param>
    public virtual void Apply(MetaDataStateLogEvent @event)
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
    protected virtual void ApplyAgentCreatedEvent(AgentCreatedEvent createdEvent)
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
    protected virtual void ApplyAgentStatusChangedEvent(AgentStatusChangedEvent statusEvent)
    {
        Status = statusEvent.NewStatus;
        LastActivity = statusEvent.StatusChangeTime;
    }
    
    /// <summary>
    /// Applies an agent properties updated event to the state.
    /// </summary>
    /// <param name="propertiesEvent">The agent properties updated event.</param>
    protected virtual void ApplyAgentPropertiesUpdatedEvent(AgentPropertiesUpdatedEvent propertiesEvent)
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
    protected virtual void ApplyAgentActivityUpdatedEvent(AgentActivityUpdatedEvent activityEvent)
    {
        LastActivity = activityEvent.ActivityTime;
    }
}