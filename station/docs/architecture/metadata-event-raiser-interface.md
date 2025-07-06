# IMetaDataStateEventRaiser Interface Design

## Overview

This document describes the design of the `IMetaDataStateEventRaiser<TState>` interface, which provides default method implementations for common event-raising patterns in GAgent-based systems. This interface leverages .NET 8+ default interface methods to reduce boilerplate code and standardize event creation across all agents.

**Important**: `IMetaDataStateEventRaiser` is a helper interface that works alongside `GAgentBase`, not something that modifies or is implemented by `GAgentBase` itself. Agents can optionally implement this interface to gain access to convenient default methods for common operations.

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

### Working Alongside GAgentBase

The key insight is that `IMetaDataStateEventRaiser` works alongside `GAgentBase` as a helper interface, NOT as something GAgentBase implements:

```csharp
// Your agent implements BOTH GAgentBase AND IMetaDataStateEventRaiser
[GAgent]
public class UserProfileAgent : GAgentBase<UserProfileState, MetaDataStateLogEvent>, 
                                 IMetaDataStateEventRaiser<UserProfileState>,
                                 IUserProfileAgent
{
    // GAgentBase provides these methods that IMetaDataStateEventRaiser needs
    public UserProfileState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    // RaiseEvent and ConfirmEvents are already provided by GAgentBase
    
    // Now you can use all the default methods from IMetaDataStateEventRaiser
    public async Task<Guid> InitializeAsync(string userName, Guid userId)
    {
        var agentId = Guid.NewGuid();
        
        // This method comes from IMetaDataStateEventRaiser's default implementation
        await CreateAgentAsync(agentId, userId, userName, "UserProfile");
        
        return agentId;
    }
}

// Concrete state implementation - just data, no behavior
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
- **GAgentBase** remains unchanged and provides core event sourcing functionality
- **IMetaDataStateEventRaiser** is an optional helper interface agents can implement
- **Composition over inheritance** - agents choose to add this functionality
- **No modifications** to existing framework code

## Usage Examples

### Basic Agent Implementation

```csharp
[GAgent]
public class UserProfileAgent : GAgentBase<UserProfileState, MetaDataStateLogEvent>, 
                                IMetaDataStateEventRaiser<UserProfileState>,
                                IUserProfileAgent
{
    // Implement required methods from IMetaDataStateEventRaiser
    public UserProfileState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    // RaiseEvent and ConfirmEvents are inherited from GAgentBase
    
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
public class ProjectAgent : GAgentBase<ProjectState, MetaDataStateLogEvent>, 
                            IMetaDataStateEventRaiser<ProjectState>,
                            IProjectAgent
{
    // Implement required methods from IMetaDataStateEventRaiser
    public ProjectState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    
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
public class AdvancedAgent : GAgentBase<AdvancedState, MetaDataStateLogEvent>, 
                             IMetaDataStateEventRaiser<AdvancedState>,
                             IAdvancedAgent
{
    // Implement required methods from IMetaDataStateEventRaiser
    public AdvancedState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    
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
2. **IMetaDataStateEventRaiser** - Optional helper interface providing default method implementations
3. **GAgentBase** - Core event sourcing functionality, unchanged
4. **Your Agent** - Inherits from GAgentBase AND optionally implements IMetaDataStateEventRaiser

This separation is crucial because:
- **No framework modifications** - GAgentBase remains untouched
- **Opt-in functionality** - Agents choose whether to use the helper interface
- **Clean architecture** - Each component has a single responsibility
- **Flexibility** - Agents can implement the interface partially or fully

### Why This Design Works

1. **Composition over inheritance** - Agents compose functionality by implementing interfaces
2. **Interface segregation** - Small, focused interfaces that do one thing well
3. **Open/Closed principle** - Extend functionality without modifying existing code
4. **Backward compatibility** - Existing agents continue to work unchanged

## Conclusion

The `IMetaDataStateEventRaiser<TState>` interface provides a powerful abstraction that simplifies agent development while maintaining the flexibility of the underlying event sourcing system. By designing it as a helper interface that works alongside `GAgentBase` (rather than modifying GAgentBase), we achieve:

- **Zero framework changes** - GAgentBase remains untouched
- **Opt-in simplicity** - Agents choose to implement the helper interface
- **Clean separation** - State, behavior, and helpers are properly separated
- **Maximum flexibility** - Agents can use as much or as little of the interface as needed

This design reduces developer friction and improves code quality while respecting existing architecture.