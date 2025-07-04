# TODO-009: Update GAgentBase to Support Stream Subscription

## Task Overview
Update the `GAgentBase` class to automatically handle Orleans stream subscription and event processing, enabling agents to receive events from external sources via Orleans streams (potentially backed by Kafka).

## Description
Enhance the existing GAgentBase infrastructure to support automatic stream subscription during grain activation. This replaces the need for agents to implement custom PublishEventAsync methods for external communication, as events will flow through Orleans streams instead.

## Acceptance Criteria
- [ ] Add automatic stream subscription to GAgentBase
- [ ] Create stream naming conventions for agent-specific streams
- [ ] Implement EventWrapper deserialization
- [ ] Add stream event routing to existing [EventHandler] methods
- [ ] Maintain backward compatibility with existing agents
- [ ] Add error handling for stream failures
- [ ] Create comprehensive unit tests
- [ ] Add integration tests with Orleans streams
- [ ] Support graceful stream reconnection
- [ ] Add stream health monitoring

## File Locations
- `framework/src/Aevatar.Core/GAgentBase.cs` (update existing)
- `framework/src/Aevatar.Core/Abstractions/IStreamSubscriber.cs` (new)
- `framework/src/Aevatar.Core/Models/StreamEventWrapper.cs` (new)

## Implementation Details

### Enhanced GAgentBase
```csharp
public abstract class GAgentBase<TState, TEvent> : 
    Grain<TState>, IStateGAgent<TState>, IStreamSubscriber
    where TState : StateBase, new()
    where TEvent : EventBase
{
    private IAsyncStream<EventWrapper> _agentStream;
    private StreamSubscriptionHandle<EventWrapper> _streamSubscription;
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        // Initialize stream subscription
        await InitializeStreamSubscriptionAsync();
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // Clean up stream subscription
        if (_streamSubscription != null)
        {
            await _streamSubscription.UnsubscribeAsync();
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    private async Task InitializeStreamSubscriptionAsync()
    {
        try
        {
            var streamProvider = this.GetStreamProvider("Aevatar");
            var streamId = StreamId.Create("agent-events", this.GetPrimaryKey().ToString());
            _agentStream = streamProvider.GetStream<EventWrapper>(streamId);
            
            _streamSubscription = await _agentStream.SubscribeAsync(OnStreamEventReceived);
            
            Logger.LogInformation("Stream subscription initialized for agent {AgentId}", 
                this.GetPrimaryKey());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize stream subscription for agent {AgentId}", 
                this.GetPrimaryKey());
            throw;
        }
    }
    
    private async Task OnStreamEventReceived(EventWrapper wrapper, StreamSequenceToken token)
    {
        try
        {
            Logger.LogDebug("Received stream event: {EventType} for agent {AgentId}", 
                wrapper.Event.GetType().Name, this.GetPrimaryKey());
            
            // Route to appropriate event handler
            await RouteStreamEventAsync(wrapper.Event);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing stream event {EventType} for agent {AgentId}", 
                wrapper.Event?.GetType().Name, this.GetPrimaryKey());
            
            // Consider dead letter queue or retry logic here
            throw;
        }
    }
}
```

### IStreamSubscriber Interface
```csharp
public interface IStreamSubscriber
{
    Task OnStreamEventReceived(EventWrapper wrapper, StreamSequenceToken token);
    Task InitializeStreamSubscriptionAsync();
    Task<bool> IsStreamHealthyAsync();
}
```

### Stream Event Routing
```csharp
protected virtual async Task RouteStreamEventAsync(EventBase @event)
{
    // Use reflection to find matching [EventHandler] methods
    var eventType = @event.GetType();
    var handlerMethods = GetEventHandlerMethods(eventType);
    
    foreach (var method in handlerMethods)
    {
        try
        {
            if (method.GetParameters().Length == 1 && 
                method.GetParameters()[0].ParameterType.IsAssignableFrom(eventType))
            {
                var task = (Task)method.Invoke(this, new object[] { @event });
                await task;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error invoking event handler {MethodName} for event {EventType}", 
                method.Name, eventType.Name);
            throw;
        }
    }
}

private MethodInfo[] GetEventHandlerMethods(Type eventType)
{
    // Cache event handler methods for performance
    return _eventHandlerCache.GetOrAdd(eventType, type =>
    {
        return this.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<EventHandlerAttribute>() != null)
            .Where(m => m.GetParameters().Length == 1)
            .Where(m => m.GetParameters()[0].ParameterType.IsAssignableFrom(type))
            .ToArray();
    });
}
```

### Stream Health Monitoring
```csharp
public async Task<bool> IsStreamHealthyAsync()
{
    try
    {
        if (_streamSubscription == null || _agentStream == null)
            return false;
        
        // Check if subscription is still active
        // Implementation depends on Orleans stream provider capabilities
        return _streamSubscription.IsValid();
    }
    catch
    {
        return false;
    }
}
```

## Stream Naming Conventions
- Agent-specific streams: `agent-events/{agentId}`
- Broadcast streams: `{namespace}/broadcast`
- System events: `system/{category}`
- Error streams: `errors/{source}`

## Dependencies
- Orleans streams infrastructure
- EventWrapper class (from TODO-006)
- Existing [EventHandler] attribute system
- Stream provider configuration

## Testing Requirements
- Unit tests with mocked stream providers
- Stream subscription lifecycle tests
- Event routing and handler invocation tests
- Error handling and recovery tests
- Performance tests for high-volume events
- Integration tests with real Orleans streams
- Backward compatibility tests with existing agents

## Error Handling Strategy
- Graceful handling of stream initialization failures
- Retry logic for transient stream errors
- Dead letter queue for failed event processing
- Circuit breaker pattern for persistent failures
- Comprehensive error logging and monitoring
- Fallback to direct grain communication if needed

## Performance Considerations
- Cache event handler method reflection
- Optimize event deserialization
- Monitor stream processing latency
- Implement backpressure handling
- Consider event batching for high volume
- Profile memory usage during stream processing

## Backward Compatibility
- Ensure existing agents continue to work unchanged
- Maintain existing [EventHandler] attribute behavior
- Support agents that don't use streams
- Gradual migration path for existing functionality
- No breaking changes to public APIs

## Stream Configuration
```csharp
// In Orleans silo configuration
builder.UseOrleans(silo =>
{
    silo.AddMemoryStreams("Aevatar")
        .AddKafkaStreams("AevatarKafka", options =>
        {
            options.BrokerList = configuration["Kafka:BrokerList"];
            options.ConsumerGroupId = "aevatar-agents";
            options.Topic = "agent-events";
        });
});
```

## Integration Points
- Works with existing GAgent framework
- Compatible with current event sourcing patterns
- Integrates with EventPublisher service (TODO-006)
- Supports existing multi-tenancy patterns
- Maintains current logging and monitoring

## Security Considerations
- Validate incoming stream events
- Ensure proper tenant isolation in streams
- Audit stream event processing
- Protect against malicious events
- Secure stream provider configuration

## Monitoring and Observability
- Stream subscription health metrics
- Event processing rate and latency
- Error rate monitoring per agent
- Stream provider health checks
- Performance dashboard integration
- Distributed tracing for event flows

## Migration Strategy
- Deploy enhanced GAgentBase alongside existing version
- Test with non-critical agents first
- Gradually enable stream subscription for agent types
- Monitor for performance impacts
- Plan rollback strategy if issues arise

## Success Metrics
- 100% stream subscription success rate
- Sub-10ms event routing latency
- Zero message loss during normal operations
- Successful integration with all agent types
- Backward compatibility maintained

## Future Enhancements
- Support for multiple stream subscriptions per agent
- Event filtering at subscription level
- Stream event replay capabilities
- Dynamic stream routing based on agent state
- Advanced error recovery and retry strategies

## Priority: Medium
This enhancement improves the event delivery architecture but can be implemented after the core services are in place.