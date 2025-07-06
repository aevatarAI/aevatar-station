# IMetaDataStateEventRaiser Interface Design

## Overview

This document describes the design of the `IMetaDataStateEventRaiser<TState>` interface, which provides default method implementations for common event-raising patterns in GAgent-based systems. This interface leverages .NET 8+ default interface methods to reduce boilerplate code and standardize event creation across all agents.

## Motivation

Currently, developers must manually create and raise events in their agent implementations, leading to:
- Repetitive boilerplate code for common operations
- Inconsistent event creation patterns across different agents
- Potential for errors in event property initialization
- Increased cognitive load for developers

By providing an interface with default event-raising methods, we can:
- Standardize common agent operations
- Reduce code duplication
- Ensure consistent event creation
- Simplify the developer experience
- Maintain flexibility for custom scenarios

## Design

### Core Interface: IMetaDataStateEventRaiser<TState>

```csharp
namespace Aevatar.MetaData
{
    /// <summary>
    /// Provides default implementations for common event-raising operations on metadata state.
    /// </summary>
    /// <typeparam name="TState">The state type that implements IMetaDataState</typeparam>
    public interface IMetaDataStateEventRaiser<TState> where TState : IMetaDataState
    {
        // Required methods - must be implemented by the consuming class (e.g., GAgentBase)
        /// <summary>
        /// Raises an event to be applied to the state.
        /// </summary>
        void RaiseEvent(MetaDataStateLogEvent @event);
        
        /// <summary>
        /// Confirms and persists all raised events.
        /// </summary>
        Task ConfirmEvents();
        
        /// <summary>
        /// Gets the current state instance.
        /// </summary>
        TState GetState();
        
        /// <summary>
        /// Gets the grain ID of the current agent.
        /// </summary>
        GrainId GetGrainId();
        
        // Default method implementations
        /// <summary>
        /// Creates a new agent with the specified metadata.
        /// </summary>
        async Task CreateAgentAsync(
            Guid id, 
            Guid userId, 
            string name, 
            string agentType, 
            Dictionary<string, string> properties = null)
        {
            var @event = new AgentCreatedEvent
            {
                Id = id,
                UserId = userId,
                Name = name,
                AgentType = agentType,
                Properties = properties ?? new Dictionary<string, string>(),
                CreateTime = DateTime.UtcNow,
                AgentGrainId = GetGrainId()
            };
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }
        
        /// <summary>
        /// Updates the agent's status with an optional reason.
        /// </summary>
        async Task UpdateStatusAsync(AgentStatus newStatus, string reason = null)
        {
            var @event = new AgentStatusChangedEvent
            {
                OldStatus = GetState().Status,
                NewStatus = newStatus,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }
        
        /// <summary>
        /// Updates the agent's properties, with option to merge or replace.
        /// </summary>
        async Task UpdatePropertiesAsync(
            Dictionary<string, string> properties, 
            bool merge = true)
        {
            var @event = new AgentPropertiesUpdatedEvent
            {
                Properties = properties,
                MergeWithExisting = merge,
                Timestamp = DateTime.UtcNow
            };
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }
        
        /// <summary>
        /// Records activity for the agent, updating the last activity timestamp.
        /// </summary>
        async Task RecordActivityAsync(string activityType = null)
        {
            var @event = new AgentActivityUpdatedEvent
            {
                ActivityType = activityType,
                Timestamp = DateTime.UtcNow
            };
            
            RaiseEvent(@event);
            await ConfirmEvents();
        }
        
        /// <summary>
        /// Updates a single property value.
        /// </summary>
        async Task SetPropertyAsync(string key, string value)
        {
            await UpdatePropertiesAsync(
                new Dictionary<string, string> { [key] = value }, 
                merge: true);
        }
        
        /// <summary>
        /// Removes a property from the agent's metadata.
        /// </summary>
        async Task RemovePropertyAsync(string key)
        {
            var currentProps = new Dictionary<string, string>(GetState().Properties);
            currentProps.Remove(key);
            await UpdatePropertiesAsync(currentProps, merge: false);
        }
        
        /// <summary>
        /// Batch updates multiple aspects of the agent in a single operation.
        /// </summary>
        async Task BatchUpdateAsync(
            AgentStatus? newStatus = null,
            Dictionary<string, string> properties = null,
            bool mergeProperties = true,
            string statusReason = null)
        {
            if (newStatus.HasValue)
            {
                var statusEvent = new AgentStatusChangedEvent
                {
                    OldStatus = GetState().Status,
                    NewStatus = newStatus.Value,
                    Reason = statusReason,
                    Timestamp = DateTime.UtcNow
                };
                RaiseEvent(statusEvent);
            }
            
            if (properties != null && properties.Any())
            {
                var propsEvent = new AgentPropertiesUpdatedEvent
                {
                    Properties = properties,
                    MergeWithExisting = mergeProperties,
                    Timestamp = DateTime.UtcNow
                };
                RaiseEvent(propsEvent);
            }
            
            await ConfirmEvents();
        }
    }
}
```

### Separation of Concerns: IMetaDataState

The `IMetaDataState` interface remains a pure state interface, separate from event-raising concerns:

```csharp
public interface IMetaDataState
{
    // State properties only - no behavior
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string AgentType { get; set; }
    string Name { get; set; }
    Dictionary<string, string> Properties { get; set; }
    GrainId AgentGrainId { get; set; }
    DateTime CreateTime { get; set; }
    AgentStatus Status { get; set; }
    DateTime LastActivity { get; set; }
    
    // Default Apply method implementation for event sourcing
    void Apply(MetaDataStateLogEvent @event) 
    {
        switch (@event)
        {
            case AgentCreatedEvent created:
                Id = created.Id;
                UserId = created.UserId;
                Name = created.Name;
                AgentType = created.AgentType;
                Properties = created.Properties;
                AgentGrainId = created.AgentGrainId;
                CreateTime = created.CreateTime;
                Status = AgentStatus.Active;
                LastActivity = created.CreateTime;
                break;
                
            case AgentStatusChangedEvent statusChanged:
                Status = statusChanged.NewStatus;
                LastActivity = statusChanged.Timestamp;
                break;
                
            case AgentPropertiesUpdatedEvent propsUpdated:
                if (propsUpdated.MergeWithExisting)
                {
                    foreach (var kvp in propsUpdated.Properties)
                        Properties[kvp.Key] = kvp.Value;
                }
                else
                {
                    Properties = propsUpdated.Properties;
                }
                LastActivity = propsUpdated.Timestamp;
                break;
                
            case AgentActivityUpdatedEvent activity:
                LastActivity = activity.Timestamp;
                break;
        }
    }
}
```

### Integration with GAgentBase

The key insight is that `GAgentBase` implements `IMetaDataStateEventRaiser`, NOT the state interface:

```csharp
// GAgentBase implements the event raiser interface
public abstract class GAgentBase<TState, TEvent> : Grain, IMetaDataStateEventRaiser<TState> 
    where TState : class, IMetaDataState, new()
    where TEvent : MetaDataStateLogEvent
{
    // Existing GAgentBase implementation...
    
    // Implement interface requirements
    public TState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    
    // RaiseEvent and ConfirmEvents already exist in GAgentBase
}

// Concrete state implementation - just data, no behavior inheritance
public class UserProfileState : IMetaDataState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public GrainId AgentGrainId { get; set; }
    public DateTime CreateTime { get; set; }
    public AgentStatus Status { get; set; }
    public DateTime LastActivity { get; set; }
}
```

This separation ensures:
- **State interfaces** remain pure data contracts
- **Behavior** is provided by GAgentBase through IMetaDataStateEventRaiser
- **No coupling** between state representation and event-raising logic

## Usage Examples

### Basic Agent Implementation

```csharp
[GAgent]
public class UserProfileAgent : GAgentBase<UserProfileState, MetaDataStateLogEvent>, IUserProfileAgent
{
    public async Task<Guid> InitializeAsync(string userName, Guid userId)
    {
        var agentId = Guid.NewGuid();
        
        // Use the default method - no manual event creation needed
        await CreateAgentAsync(
            id: agentId,
            userId: userId,
            name: userName,
            agentType: "UserProfile",
            properties: new Dictionary<string, string> 
            { 
                ["role"] = "user",
                ["created_from"] = "web_app"
            }
        );
        
        return agentId;
    }
    
    public async Task UpdateProfileAsync(string displayName, string bio)
    {
        // Update multiple properties at once
        await UpdatePropertiesAsync(new Dictionary<string, string>
        {
            ["display_name"] = displayName,
            ["bio"] = bio,
            ["last_updated"] = DateTime.UtcNow.ToString("O")
        });
    }
    
    public async Task DeactivateAsync(string reason)
    {
        // Simple status update with reason
        await UpdateStatusAsync(AgentStatus.Inactive, reason);
    }
}
```

### Advanced Usage with Batch Operations

```csharp
[GAgent]
public class ProjectAgent : GAgentBase<ProjectState, MetaDataStateLogEvent>, IProjectAgent
{
    public async Task CompleteProjectAsync(string completionNotes)
    {
        // Batch update status and properties in one operation
        await BatchUpdateAsync(
            newStatus: AgentStatus.Completed,
            properties: new Dictionary<string, string>
            {
                ["completion_notes"] = completionNotes,
                ["completed_at"] = DateTime.UtcNow.ToString("O"),
                ["completed_by"] = GetCurrentUserId()
            },
            statusReason: "Project completed successfully"
        );
    }
    
    public async Task AddTagAsync(string tag)
    {
        // Use the convenient single-property update
        await SetPropertyAsync($"tag_{tag}", "true");
    }
    
    public async Task RemoveTagAsync(string tag)
    {
        // Remove a specific property
        await RemovePropertyAsync($"tag_{tag}");
    }
}
```

### Custom Event Scenarios

```csharp
[GAgent]
public class AdvancedAgent : GAgentBase<AdvancedState, MetaDataStateLogEvent>, IAdvancedAgent
{
    public async Task PerformComplexOperationAsync(ComplexData data)
    {
        // Use default methods for standard operations
        await RecordActivityAsync("complex_operation_started");
        
        // Create custom events when needed
        var customEvent = new CustomBusinessEvent
        {
            Data = data,
            ProcessedAt = DateTime.UtcNow,
            // ... other custom properties
        };
        
        RaiseEvent(customEvent);
        
        // Can mix default and custom operations
        await UpdatePropertyAsync("last_complex_operation", DateTime.UtcNow.ToString("O"));
        
        await ConfirmEvents();
    }
}
```

## Benefits

1. **Reduced Boilerplate**: Common operations require minimal code
2. **Consistency**: All agents use the same patterns for standard operations
3. **Type Safety**: Compile-time checking prevents common errors
4. **Flexibility**: Custom events can still be raised when needed
5. **Discoverability**: IntelliSense shows available operations
6. **Maintainability**: Changes to event creation logic happen in one place
7. **Testing**: Easier to test agents with standardized operations

## Migration Path

Existing agents can adopt this pattern gradually:

1. **Phase 1**: Add interface to existing agents without changing implementation
2. **Phase 2**: Replace manual event creation with default methods where applicable
3. **Phase 3**: Remove redundant code and simplify agent implementations

### Before Migration

```csharp
public async Task CreateUser(string name, Guid userId)
{
    var evt = new AgentCreatedEvent
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = name,
        AgentType = "User",
        Properties = new Dictionary<string, string>(),
        CreateTime = DateTime.UtcNow,
        AgentGrainId = this.GetGrainId()
    };
    
    RaiseEvent(evt);
    await ConfirmEvents();
}
```

### After Migration

```csharp
public async Task CreateUser(string name, Guid userId)
{
    await CreateAgentAsync(Guid.NewGuid(), userId, name, "User");
}
```

## Future Enhancements

1. **Validation**: Add parameter validation in default methods
2. **Hooks**: Pre/post operation hooks for custom logic
3. **Async Events**: Support for long-running operations
4. **Audit Trail**: Automatic audit event generation
5. **Permissions**: Built-in permission checking before operations

## Design Principles

### Separation of Concerns

1. **IMetaDataState** - Pure state interface with data properties and Apply method
2. **IMetaDataStateEventRaiser** - Behavior interface with event-raising operations
3. **GAgentBase** - Implements the behavior interface, manages state instances

This separation is crucial because:
- State objects should not have dependencies on grain infrastructure
- State interfaces should be serializable and lightweight
- Behavior should be provided by the grain, not the state

### Why IMetaDataState Should NOT Inherit from IMetaDataStateEventRaiser

1. **State objects are POCOs** - They should not have behavior methods
2. **Serialization concerns** - State objects are persisted; they shouldn't contain methods that depend on grain context
3. **Testing isolation** - State objects can be tested independently without grain infrastructure
4. **Clear responsibilities** - State = data, Grain = behavior

## Conclusion

The `IMetaDataStateEventRaiser<TState>` interface provides a powerful abstraction that simplifies agent development while maintaining the flexibility of the underlying event sourcing system. By keeping it separate from `IMetaDataState` and having only `GAgentBase` implement it, we achieve proper separation of concerns and a clean, extensible design that reduces developer friction and improves code quality across the entire system.