# TODO-006: Create EventPublisher Service for Orleans Streams

## Task Overview
Create the `EventPublisher` service that publishes events from the API layer to Orleans streams (which can be backed by Kafka) for agent consumption, replacing direct CreatorGAgent event publishing.

## Description
Implement the service that handles external event delivery through Orleans streams. This eliminates the need for GAgents to implement outbound PublishEventAsync methods and provides better integration with the Orleans ecosystem and external messaging systems like Kafka.

## Acceptance Criteria
- [ ] Create `IEventPublisher` interface
- [ ] Implement `EventPublisher` class with Orleans streams
- [ ] Create `EventWrapper` class for stream messages
- [ ] Add support for targeted and broadcast events
- [ ] Implement error handling and retry logic
- [ ] Add comprehensive logging
- [ ] Create unit tests with mocked dependencies
- [ ] Add integration tests with Orleans streams
- [ ] Support Kafka-backed streams configuration
- [ ] Add performance monitoring

## File Locations
- `station/src/Aevatar.Application/Services/IEventPublisher.cs`
- `station/src/Aevatar.Application/Services/EventPublisher.cs`
- `station/src/Aevatar.Application/Models/EventWrapper.cs`

## Implementation Details

### IEventPublisher Interface
```csharp
public interface IEventPublisher
{
    Task PublishEventAsync<T>(T @event, string agentId) where T : EventBase;
    Task PublishBroadcastEventAsync<T>(T @event, string streamNamespace) where T : EventBase;
    Task PublishEventToMultipleAgentsAsync<T>(T @event, List<string> agentIds) where T : EventBase;
    Task<bool> IsStreamHealthyAsync(string streamNamespace);
}
```

### EventWrapper Model
```csharp
public class EventWrapper
{
    public EventBase Event { get; set; }
    public DateTime Timestamp { get; set; }
    public string SourceId { get; set; }
    public string TargetAgentId { get; set; }
    public string MessageId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}
```

### Core Dependencies
- `IClusterClient` for Orleans streams access
- `ILogger<EventPublisher>` for logging
- Stream provider configuration
- Optional: Kafka stream provider

### Key Features

#### Targeted Event Publishing
- Publish events to specific agent streams
- Use stream naming convention: `agent-events/{agentId}`
- Wrap events in EventWrapper for metadata
- Support event correlation and tracing

#### Broadcast Event Publishing
- Publish events to namespace streams for multiple consumers
- Use stream naming convention: `{namespace}/broadcast`
- Support fan-out scenarios
- Handle subscriber management

#### Stream Health Monitoring
- Check stream provider availability
- Monitor stream processing rates
- Detect and report stream failures
- Provide health status for monitoring systems

## Stream Configuration

### Orleans Streams Setup
```csharp
// In Orleans silo configuration
builder.UseOrleans(silo =>
{
    silo.AddMemoryStreams("Aevatar")
        .AddKafkaStreams("AevatarKafka", kafkaOptions =>
        {
            kafkaOptions.BrokerList = "localhost:9092";
            kafkaOptions.ConsumerGroupId = "aevatar-agents";
        });
});
```

### Stream Naming Conventions
- Agent-specific streams: `agent-events/{agentId}`
- Broadcast streams: `{namespace}/broadcast`
- System streams: `system/{category}`
- Error streams: `errors/{source}`

## Dependencies
- Orleans cluster client
- Orleans streams infrastructure
- EventBase class hierarchy
- Optional: Kafka stream provider
- Configuration system for stream settings

## Testing Requirements
- Unit tests with mocked IClusterClient
- Stream provider interaction tests
- Event serialization/deserialization tests
- Error handling and retry logic tests
- Performance tests for high-volume scenarios
- Integration tests with real Orleans streams
- Kafka integration tests (if configured)

## Error Handling Strategy
- Retry transient failures with exponential backoff
- Dead letter queue for failed events
- Comprehensive error logging
- Circuit breaker pattern for stream failures
- Graceful degradation when streams unavailable
- Event correlation for debugging

## Performance Considerations
- Batch publishing for multiple events
- Async/await patterns throughout
- Stream connection pooling
- Monitor and optimize serialization
- Configure appropriate stream buffers
- Performance counters and metrics

## Configuration Options
```csharp
public class EventPublisherOptions
{
    public string DefaultStreamProvider { get; set; } = "Aevatar";
    public int RetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public int BatchSize { get; set; } = 100;
    public bool EnableDeadLetterQueue { get; set; } = true;
    public TimeSpan StreamHealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}
```

## Integration Points
- Replace CreatorGAgent.PublishEventAsync usage
- Work with existing event hierarchy (EventBase)
- Integrate with Orleans silo configuration
- Support current multi-tenancy patterns
- Compatible with existing logging and monitoring

## Kafka Integration
- Optional Kafka stream provider configuration
- Support for Kafka topics as Orleans streams
- Handle Kafka-specific serialization requirements
- Monitor Kafka broker health
- Support Kafka consumer group management

## Security Considerations
- Validate event types and content
- Ensure proper authorization for event publishing
- Audit event publishing operations
- Protect against event injection attacks
- Support encrypted streams if required

## Monitoring and Observability
- Event publishing metrics (count, rate, latency)
- Stream health monitoring
- Error rate tracking
- Performance dashboard integration
- Distributed tracing support

## Success Metrics
- 99.9% event delivery success rate
- Sub-100ms publishing latency for single events
- Zero data loss during normal operations
- Successful Kafka integration (if configured)
- Seamless Orleans cluster scaling

## Migration Strategy
- Implement alongside existing CreatorGAgent publishing
- Gradually migrate services to use EventPublisher
- Maintain backward compatibility during transition
- Monitor for performance regressions
- Plan rollback strategy if needed

## Priority: High
This service is essential for the event-driven architecture and must be implemented to replace CreatorGAgent event publishing.