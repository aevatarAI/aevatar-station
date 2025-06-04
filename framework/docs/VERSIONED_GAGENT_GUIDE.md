# VersionedGAgent Usage Guide

## Overview

The `IVersionedGAgent` interface provides a version-stable abstraction on top of `GAgentBase` that shields developers from implementation changes while maintaining full Orleans compatibility. This interface supports Orleans attributes and can be called like a regular Grain.

## Key Benefits

- **Version Stability**: Interface won't change when `GAgentBase` implementation evolves
- **Orleans Compatibility**: Full support for Orleans attributes (`[ReadOnly]`, `[AlwaysInterleave]`, etc.)
- **Grain-like Interface**: Can be called directly like any Orleans Grain
- **Type Safety**: Strong typing with generic constraints
- **Enhanced Observability**: Built-in metrics and health monitoring
- **Batch Operations**: Efficient bulk registration and subscription methods

## Interface Hierarchy

```csharp
// Full interface with all type parameters
IVersionedGAgent<TState, TEvent, TConfiguration>

// Simplified interface with default Event and Configuration
IVersionedGAgent<TState> : IVersionedGAgent<TState, EventBase, ConfigurationBase>

// Basic interface with minimal constraints
IVersionedGAgent : IVersionedGAgent<StateBase>
```

## Basic Usage

### 1. Define Your Agent Interface

```csharp
// Custom state
public class MyAgentState : StateBase
{
    public string Name { get; set; } = string.Empty;
    public int Counter { get; set; } = 0;
}

// Custom event
public class MyEvent : EventBase
{
    public string Message { get; set; } = string.Empty;
}

// Custom configuration
public class MyConfiguration : ConfigurationBase
{
    public int MaxRetries { get; set; } = 3;
}

// Agent interface - this is what clients will use
public interface IMyAgent : IVersionedGAgent<MyAgentState, MyEvent, MyConfiguration>
{
    // Custom methods specific to your agent
    Task<string> DoSomethingAsync(string input);
    
    [ReadOnly]
    Task<int> GetCounterValueAsync();
}
```

### 2. Implement Your Agent

```csharp
[GAgent]
public class MyAgent : VersionedGAgentBase<MyAgentState, MyStateLogEvent, MyEvent, MyConfiguration>, IMyAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("My custom agent implementation");
    }

    public async Task<string> DoSomethingAsync(string input)
    {
        // Your business logic here
        State.Counter++;
        State.Name = input;
        
        // Publish an event
        await PublishEventAsync(new MyEvent { Message = $"Processed: {input}" });
        
        return $"Processed {input}, counter is now {State.Counter}";
    }

    [ReadOnly]
    public Task<int> GetCounterValueAsync()
    {
        return Task.FromResult(State.Counter);
    }

    // Event handler
    [EventHandler]
    public async Task HandleMyEventAsync(MyEvent evt)
    {
        Logger.LogInformation("Received event: {Message}", evt.Message);
        // Handle the event
    }
}
```

### 3. Use the Factory to Create Agents

```csharp
public class MyService
{
    private readonly IVersionedGAgentFactory _factory;

    public MyService(IVersionedGAgentFactory factory)
    {
        _factory = factory;
    }

    public async Task ExampleUsageAsync()
    {
        // Create agent with configuration
        var config = new MyConfiguration { MaxRetries = 5 };
        var agent = await _factory.GetVersionedGAgentAsync<IMyAgent, MyAgentState, MyEvent, MyConfiguration>(
            Guid.NewGuid(), config);

        // Use the agent like any Orleans grain
        var result = await agent.DoSomethingAsync("Hello World");
        var counter = await agent.GetCounterValueAsync();
        
        // Check health and metrics
        var health = await agent.GetHealthStatusAsync();
        var metrics = await agent.GetMetricsAsync();
        
        Console.WriteLine($"Health: {health.Status}");
        Console.WriteLine($"Events processed: {metrics.EventsProcessed}");
    }
}
```

## Advanced Features

### Orleans Attributes Support

The interface fully supports Orleans attributes:

```csharp
public interface IMyAdvancedAgent : IVersionedGAgent<MyState>
{
    [ReadOnly]
    Task<string> GetStatusAsync();
    
    [AlwaysInterleave]
    Task ProcessHighPriorityEventAsync(HighPriorityEvent evt);
    
    [OneWay]
    Task FireAndForgetOperationAsync(string data);
}
```

### Batch Operations

```csharp
// Register multiple agents efficiently
var childAgents = await _factory.GetVersionedGAgentsBatchAsync<IChildAgent, ChildState, EventBase, ConfigurationBase>(
    childGrainIds);

await parentAgent.RegisterAgentsAsync(childAgents);

// Subscribe to multiple agents
await currentAgent.SubscribeToAgentsAsync(otherAgents);
```

### Event Publishing with Response

```csharp
// Publish event and wait for response
var response = await agent.PublishEventWithResponseAsync<MyRequestEvent, MyResponseEvent>(
    new MyRequestEvent { Data = "request" },
    timeout: TimeSpan.FromSeconds(10));
```

### Health Monitoring

```csharp
// Get detailed health status
var health = await agent.GetHealthStatusAsync();
if (!health.IsHealthy)
{
    Logger.LogWarning("Agent is unhealthy: {Status}", health.Status);
    // Take corrective action
}

// Get metrics for observability
var metrics = await agent.GetMetricsAsync();
Logger.LogInformation("Agent uptime: {Uptime}, Events processed: {EventsProcessed}", 
    metrics.Uptime, metrics.EventsProcessed);
```

## Migration from GAgentBase

### Before (using GAgentBase directly)

```csharp
public class OldAgent : GAgentBase<MyState, MyStateLogEvent>
{
    // Implementation tied to GAgentBase
}

// Client code
var agent = grainFactory.GetGrain<IGAgent>(grainId);
```

### After (using IVersionedGAgent)

```csharp
public interface INewAgent : IVersionedGAgent<MyState>
{
    // Your custom methods
}

public class NewAgent : VersionedGAgentBase<MyState>, INewAgent
{
    // Same implementation, but version-stable
}

// Client code
var agent = await factory.GetVersionedGAgentAsync<INewAgent, MyState>(grainId);
```

## Registration with DI Container

```csharp
// In your startup/configuration
services.AddSingleton<IVersionedGAgentFactory, VersionedGAgentFactory>();

// Register your agent implementations
services.AddSingleton<IMyAgent, MyAgent>();
```

## Best Practices

1. **Always use the interface**: Never reference the concrete implementation directly
2. **Define custom interfaces**: Create specific interfaces for your agents rather than using the base interface
3. **Use appropriate Orleans attributes**: Apply `[ReadOnly]`, `[AlwaysInterleave]` as needed
4. **Monitor health and metrics**: Regularly check agent health in production
5. **Use batch operations**: For bulk operations, use the batch methods for better performance
6. **Handle cancellation**: Pass and respect `CancellationToken` parameters
7. **Configure appropriately**: Use typed configuration classes for complex setups

## Error Handling

```csharp
try
{
    await agent.PublishEventAsync(new MyEvent());
}
catch (OperationCanceledException)
{
    // Handle cancellation
}
catch (TimeoutException)
{
    // Handle timeout
}
catch (Exception ex)
{
    // Handle other errors
    Logger.LogError(ex, "Failed to publish event");
}
```

This approach provides a future-proof way to work with GAgents while maintaining all the power and flexibility of the Orleans programming model.