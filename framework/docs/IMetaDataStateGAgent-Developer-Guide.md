# IMetaDataStateGAgent Developer Guide

## Overview

`IMetaDataStateGAgent<TState>` is a helper interface that provides default implementations for common event-raising operations in GAgent-based systems. It leverages .NET 8+ default interface methods to reduce boilerplate code when working with metadata state management.

## Motivation

When implementing agents that manage metadata state, developers often need to write repetitive code for common operations like:
- Creating agents with initial metadata
- Updating agent status
- Managing properties
- Recording activity timestamps
- Batch updates

This interface provides pre-implemented methods for these common scenarios, allowing developers to focus on business logic rather than plumbing code.

## Key Features

- **Default Method Implementations**: All methods have default implementations using C# 8+ interface default methods
- **Type Safety**: Generic constraint ensures the state implements `IMetaDataState`
- **Event Sourcing Integration**: Automatically creates and raises appropriate events
- **Flexible Usage**: Optional to implement - agents can choose to use it or not
- **Zero Framework Changes**: Works alongside existing GAgentBase without modifications

## Implementation Tutorial

### Step 1: Define Your State Class

Your state class should inherit from `MetaDataStateBase` or implement `IMetaDataState`:

```csharp
public class MyAgentState : MetaDataStateBase
{
    // Add any additional properties specific to your agent
    public string CustomProperty { get; set; }
}
```

### Step 2: Create Your Agent Interface

Define your grain interface as usual:

```csharp
public interface IMyAgent : IGrainWithGuidKey
{
    Task<MyAgentState> GetStateAsync();
    // Add your custom methods
}
```

### Step 3: Implement Your Agent Class

Inherit from `GAgentBase` and implement `IMetaDataStateGAgent`:

```csharp
[GAgent]
public class MyAgent : GAgentBase<MyAgentState, MetaDataStateLogEvent>, 
    IMyAgent, IMetaDataStateGAgent<MyAgentState>
{
    // Required implementations for IMetaDataStateGAgent
    public void RaiseEvent(MetaDataStateLogEvent @event) => base.RaiseEvent(@event);
    public Task ConfirmEvents() => base.ConfirmEvents();
    public MyAgentState GetState() => State;
    public GrainId GetGrainId() => this.GetGrainId();
    
    // Your agent methods
    public Task<MyAgentState> GetStateAsync() => Task.FromResult(State);
    
    // Required by GAgentBase
    public override Task<string> GetDescriptionAsync() => Task.FromResult("My Agent");
}
```

### Step 4: Use the Default Methods

Now you can use all the default methods provided by the interface:

```csharp
// In your grain client code
var agent = grainFactory.GetGrain<IMyAgent>(agentId);

// Create agent with metadata
await agent.CreateAgentAsync(
    id: agentId,
    userId: userId,
    name: "My Agent Instance",
    agentType: "MyAgentType",
    properties: new Dictionary<string, string> 
    { 
        ["environment"] = "production" 
    });

// Update status
await agent.UpdateStatusAsync(AgentStatus.Active, "Agent initialized");

// Set individual properties
await agent.SetPropertyAsync("version", "1.0.0");

// Record activity
await agent.RecordActivityAsync("UserLogin");

// Batch update
await agent.BatchUpdateAsync(
    newStatus: AgentStatus.Paused,
    properties: new Dictionary<string, string> { ["maintenance"] = "true" },
    statusReason: "Scheduled maintenance");
```

## API Reference

### CreateAgentAsync

Creates a new agent with the specified metadata.

```csharp
Task CreateAgentAsync(
    Guid id, 
    Guid userId, 
    string name, 
    string agentType, 
    Dictionary<string, string>? properties = null)
```

**Parameters:**
- `id`: The unique identifier for the agent
- `userId`: The user ID that owns the agent
- `name`: The display name of the agent
- `agentType`: The type of agent
- `properties`: Optional initial properties

**Event Raised:** `AgentCreatedEvent`

### UpdateStatusAsync

Updates the agent's status with an optional reason.

```csharp
Task UpdateStatusAsync(AgentStatus newStatus, string? reason = null)
```

**Parameters:**
- `newStatus`: The new status to set
- `reason`: Optional reason for the status change

**Event Raised:** `AgentStatusChangedEvent`

### UpdatePropertiesAsync

Updates the agent's properties with merge or replace behavior.

```csharp
Task UpdatePropertiesAsync(
    Dictionary<string, string> properties, 
    bool merge = true)
```

**Parameters:**
- `properties`: The properties to update
- `merge`: Whether to merge (true) or replace (false) existing properties

**Event Raised:** `AgentPropertiesUpdatedEvent`

### RecordActivityAsync

Records activity for the agent, updating the last activity timestamp.

```csharp
Task RecordActivityAsync(string? activityType = null)
```

**Parameters:**
- `activityType`: Optional type of activity being recorded

**Event Raised:** `AgentActivityUpdatedEvent`

### SetPropertyAsync

Sets a single property value.

```csharp
Task SetPropertyAsync(string key, string value)
```

**Parameters:**
- `key`: The property key
- `value`: The property value

**Event Raised:** `AgentPropertiesUpdatedEvent` (via UpdatePropertiesAsync)

### RemovePropertyAsync

Removes a property from the agent's metadata.

```csharp
Task RemovePropertyAsync(string key)
```

**Parameters:**
- `key`: The property key to remove

**Event Raised:** `AgentPropertiesUpdatedEvent` (via UpdatePropertiesAsync)

### BatchUpdateAsync

Performs multiple updates in a single operation.

```csharp
Task BatchUpdateAsync(
    AgentStatus? newStatus = null,
    Dictionary<string, string>? properties = null,
    bool mergeProperties = true,
    string? statusReason = null)
```

**Parameters:**
- `newStatus`: Optional new status
- `properties`: Optional properties to update
- `mergeProperties`: Whether to merge properties
- `statusReason`: Optional reason for status change

**Events Raised:** 
- `AgentStatusChangedEvent` (if status provided)
- `AgentPropertiesUpdatedEvent` (if properties provided)

## Best Practices

### 1. Use Appropriate Methods

- Use `CreateAgentAsync` only for initial agent creation
- Use `BatchUpdateAsync` when updating multiple aspects simultaneously
- Use specific methods (`UpdateStatusAsync`, `SetPropertyAsync`) for single updates

### 2. Handle Exceptions

The interface methods don't include exception handling. Wrap calls in try-catch blocks:

```csharp
try
{
    await agent.UpdateStatusAsync(AgentStatus.Active);
}
catch (Exception ex)
{
    // Handle exception appropriately
    logger.LogError(ex, "Failed to update agent status");
}
```

### 3. Property Key Conventions

Establish naming conventions for property keys:
- Use lowercase with underscores: `max_retries`
- Or use PascalCase: `MaxRetries`
- Be consistent throughout your application

### 4. Status Transition Rules

Consider implementing status transition validation in your agent:

```csharp
public override async Task UpdateStatusAsync(AgentStatus newStatus, string? reason = null)
{
    // Validate transition
    if (!IsValidTransition(State.Status, newStatus))
    {
        throw new InvalidOperationException(
            $"Cannot transition from {State.Status} to {newStatus}");
    }
    
    // Use the default implementation
    await ((IMetaDataStateGAgent<MyAgentState>)this).UpdateStatusAsync(newStatus, reason);
}
```

## Common Patterns

### Pattern 1: Agent Initialization

```csharp
public async Task InitializeAsync(Guid userId, string name, Dictionary<string, string> config)
{
    // Create the agent
    await this.CreateAgentAsync(
        this.GetPrimaryKey(), 
        userId, 
        name, 
        "MyAgentType", 
        config);
    
    // Activate it
    await this.UpdateStatusAsync(AgentStatus.Active, "Initialization complete");
    
    // Record the initialization
    await this.RecordActivityAsync("Initialized");
}
```

### Pattern 2: Property Management

```csharp
public async Task ConfigureAsync(Dictionary<string, string> settings)
{
    // Validate settings
    foreach (var (key, value) in settings)
    {
        if (!IsValidSetting(key, value))
        {
            throw new ArgumentException($"Invalid setting: {key}");
        }
    }
    
    // Apply settings
    await this.UpdatePropertiesAsync(settings, merge: true);
    
    // Record configuration change
    await this.RecordActivityAsync("ConfigurationUpdated");
}
```

### Pattern 3: Lifecycle Management

```csharp
public async Task PauseAsync(string reason)
{
    await this.BatchUpdateAsync(
        newStatus: AgentStatus.Paused,
        properties: new Dictionary<string, string> 
        { 
            ["paused_at"] = DateTime.UtcNow.ToString("O"),
            ["pause_reason"] = reason
        },
        statusReason: reason);
}

public async Task ResumeAsync()
{
    await this.UpdateStatusAsync(AgentStatus.Active, "Resumed from pause");
    await this.RemovePropertyAsync("paused_at");
    await this.RemovePropertyAsync("pause_reason");
}
```

## Troubleshooting

### Common Issues

1. **Method not found**: Ensure you're casting to the interface when calling default methods
2. **State not updating**: Verify `ConfirmEvents()` is being called
3. **Events not raised**: Check that `RaiseEvent()` is properly forwarding to base class

### Debugging Tips

1. Enable Orleans event sourcing logging
2. Use breakpoints in the `RaiseEvent` method
3. Verify state changes after `ConfirmEvents`
4. Check event journal for proper event sequence

## Performance Considerations

- Default methods have minimal overhead
- Events are raised synchronously before confirmation
- Batch operations are more efficient than multiple individual calls
- Property dictionaries are copied to ensure immutability

## Migration Guide

To migrate existing agents to use `IMetaDataStateGAgent`:

1. Add the interface to your agent class declaration
2. Implement the four required methods
3. Replace manual event creation with interface method calls
4. Remove redundant boilerplate code

Example migration:

```csharp
// Before
public async Task UpdateAgentName(string newName)
{
    var @event = new AgentPropertiesUpdatedEvent
    {
        AgentId = State.Id,
        UserId = State.UserId,
        UpdatedProperties = new Dictionary<string, string> { ["name"] = newName },
        RemovedProperties = new List<string>(),
        WasMerged = true,
        UpdateTime = DateTime.UtcNow
    };
    RaiseEvent(@event);
    await ConfirmEvents();
}

// After
public async Task UpdateAgentName(string newName)
{
    await this.SetPropertyAsync("name", newName);
}
```

## Integration Testing Example

Here's how to write integration tests for agents using `IMetaDataStateGAgent`:

```csharp
using Aevatar.TestKit;
using Aevatar.TestKit.Extensions;

public class MyAgentTests : DefaultTestKitBase
{
    [Fact]
    public async Task MyAgent_CreateAndUpdate_WorksCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await Silo.CreateGrainAsync<MyAgent>(agentId);
        
        // Act - Create agent
        await ((IMetaDataStateGAgent<MyAgentState>)agent).CreateAgentAsync(
            agentId, userId, "Test Agent", "TestType");
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Id.ShouldBe(agentId);
        state.Status.ShouldBe(AgentStatus.Creating);
        
        // Act - Update status
        await ((IMetaDataStateGAgent<MyAgentState>)agent).UpdateStatusAsync(
            AgentStatus.Active, "Activated");
        
        // Assert
        state = await agent.GetStateAsync();
        state.Status.ShouldBe(AgentStatus.Active);
    }
}
```

## Summary

`IMetaDataStateGAgent<TState>` significantly reduces boilerplate code for common metadata operations while maintaining flexibility and type safety. By using this interface, developers can focus on business logic rather than event management plumbing, leading to cleaner and more maintainable code.