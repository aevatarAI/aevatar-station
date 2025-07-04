# TODO-011: Update SubscriptionAppService to Use New Architecture

## Task Overview
Update the existing `SubscriptionAppService` class to use the new architecture services instead of directly interacting with CreatorGAgent grains for event management and subscription operations.

## Description
Refactor the SubscriptionAppService implementation to utilize EventPublisher for event publishing and TypeMetadataService for event metadata, while maintaining existing subscription management functionality. This removes the dependency on CreatorGAgent for event-related operations.

## Acceptance Criteria
- [ ] Replace CreatorGAgent usage with new services
- [ ] Maintain existing subscription API contracts
- [ ] Update event publishing to use EventPublisher service
- [ ] Use TypeMetadataService for available events queries
- [ ] Preserve all subscription management functionality
- [ ] Add comprehensive unit tests for updated methods
- [ ] Update error handling patterns
- [ ] Add integration tests with new services
- [ ] Update logging to reflect new architecture
- [ ] Ensure backward compatibility

## File Locations
- `station/src/Aevatar.Application/Service/SubscriptionAppService.cs` (update existing)
- `station/src/Aevatar.Application/Service/ISubscriptionAppService.cs` (update if needed)

## Current Methods to Update

### Event Management
- `GetAvailableEventsAsync()` - Use TypeMetadataService instead of CreatorGAgent
- `PublishEventAsync()` - Use EventPublisher instead of CreatorGAgent

### Subscription Operations
- `CreateSubscriptionAsync()` - Update validation to use new services
- `CancelSubscriptionAsync()` - Update agent validation
- `GetSubscriptionAsync()` - Update with new service patterns

## Implementation Details

### Updated Constructor and Dependencies
```csharp
public class SubscriptionAppService : ISubscriptionAppService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ITypeMetadataService _typeMetadataService;
    private readonly IAgentDiscoveryService _discoveryService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscriptionAppService> _logger;
    
    public SubscriptionAppService(
        IEventPublisher eventPublisher,
        ITypeMetadataService typeMetadataService,
        IAgentDiscoveryService discoveryService,
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionAppService> logger)
    {
        _eventPublisher = eventPublisher;
        _typeMetadataService = typeMetadataService;
        _discoveryService = discoveryService;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }
}
```

### GetAvailableEventsAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent
public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid agentId)
{
    var agent = _clusterClient.GetGrain<ICreatorGAgent>(agentId);
    var agentState = await agent.GetAgentAsync();
    
    return agentState.EventInfoList?.Select(eventInfo => new EventDescriptionDto
    {
        EventType = eventInfo.EventType.Name,
        Description = eventInfo.Description
    }).ToList() ?? new List<EventDescriptionDto>();
}

// AFTER: Using TypeMetadataService
public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid agentId)
{
    try
    {
        // Get agent information to determine type
        var agentInfo = await _discoveryService.FindAgentByIdAsync(agentId);
        if (agentInfo == null)
        {
            throw new BusinessException($"Agent {agentId} not found", "AGENT_NOT_FOUND");
        }
        
        // Get type metadata for capabilities
        var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(agentInfo.AgentType);
        if (typeMetadata == null)
        {
            throw new BusinessException($"Agent type {agentInfo.AgentType} not found", "AGENT_TYPE_NOT_FOUND");
        }
        
        return typeMetadata.Capabilities.Select(capability => new EventDescriptionDto
        {
            EventType = capability,
            Description = GetEventDescription(capability)
        }).ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting available events for agent {AgentId}", agentId);
        throw;
    }
}
```

### PublishEventAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent
public async Task PublishEventAsync(PublishEventDto dto)
{
    try
    {
        var agent = _clusterClient.GetGrain<ICreatorGAgent>(dto.AgentId);
        
        // Create event instance from DTO
        var eventInstance = CreateEventInstance(dto.EventType, dto.EventData);
        
        await agent.PublishEventAsync(eventInstance);
        
        _logger.LogInformation("Event published successfully: {EventType} to agent {AgentId}", 
            dto.EventType, dto.AgentId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error publishing event {EventType} to agent {AgentId}", 
            dto.EventType, dto.AgentId);
        throw;
    }
}

// AFTER: Using EventPublisher
public async Task PublishEventAsync(PublishEventDto dto)
{
    try
    {
        // Validate agent exists
        var agentExists = await _discoveryService.AgentExistsAsync(dto.AgentId);
        if (!agentExists)
        {
            throw new BusinessException($"Agent {dto.AgentId} not found", "AGENT_NOT_FOUND");
        }
        
        // Create event instance from DTO
        var eventInstance = CreateEventInstance(dto.EventType, dto.EventData);
        
        // Publish via EventPublisher service
        await _eventPublisher.PublishEventAsync(eventInstance, dto.AgentId.ToString());
        
        _logger.LogInformation("Event published successfully: {EventType} to agent {AgentId}", 
            dto.EventType, dto.AgentId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error publishing event {EventType} to agent {AgentId}", 
            dto.EventType, dto.AgentId);
        throw;
    }
}
```

### CreateSubscriptionAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent for validation
public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionInput input)
{
    // Validate agent exists
    var agent = _clusterClient.GetGrain<ICreatorGAgent>(input.AgentId);
    var agentState = await agent.GetAgentAsync();
    
    // Rest of subscription creation logic...
}

// AFTER: Using AgentDiscoveryService for validation
public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionInput input)
{
    try
    {
        // Validate agent exists and is accessible
        var agentInfo = await _discoveryService.FindAgentByIdAsync(input.AgentId);
        if (agentInfo == null)
        {
            throw new BusinessException($"Agent {input.AgentId} not found", "AGENT_NOT_FOUND");
        }
        
        // Validate event type is supported by agent
        var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(agentInfo.AgentType);
        if (!typeMetadata.Capabilities.Contains(input.EventType))
        {
            throw new BusinessException(
                $"Agent type {agentInfo.AgentType} does not support event {input.EventType}", 
                "UNSUPPORTED_EVENT_TYPE");
        }
        
        // Create subscription
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            AgentId = input.AgentId,
            EventType = input.EventType,
            SubscriberId = input.SubscriberId,
            CreatedAt = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };
        
        await _subscriptionRepository.CreateAsync(subscription);
        
        _logger.LogInformation("Subscription created: {SubscriptionId} for agent {AgentId}", 
            subscription.Id, input.AgentId);
        
        return MapToDto(subscription);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating subscription for agent {AgentId}", input.AgentId);
        throw;
    }
}
```

### CancelSubscriptionAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent for validation
public async Task CancelSubscriptionAsync(Guid subscriptionId)
{
    var subscription = await _subscriptionRepository.GetAsync(subscriptionId);
    if (subscription == null)
    {
        throw new BusinessException($"Subscription {subscriptionId} not found", "SUBSCRIPTION_NOT_FOUND");
    }
    
    // Validate agent still exists
    var agent = _clusterClient.GetGrain<ICreatorGAgent>(subscription.AgentId);
    await agent.GetAgentAsync(); // This would throw if agent doesn't exist
    
    // Cancel subscription logic...
}

// AFTER: Using AgentDiscoveryService for validation
public async Task CancelSubscriptionAsync(Guid subscriptionId)
{
    try
    {
        var subscription = await _subscriptionRepository.GetAsync(subscriptionId);
        if (subscription == null)
        {
            throw new BusinessException($"Subscription {subscriptionId} not found", "SUBSCRIPTION_NOT_FOUND");
        }
        
        // Validate agent still exists (optional - subscription can exist even if agent is deleted)
        var agentExists = await _discoveryService.AgentExistsAsync(subscription.AgentId);
        if (!agentExists)
        {
            _logger.LogWarning("Cancelling subscription {SubscriptionId} for non-existent agent {AgentId}", 
                subscriptionId, subscription.AgentId);
        }
        
        // Update subscription status
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        
        await _subscriptionRepository.UpdateAsync(subscription);
        
        _logger.LogInformation("Subscription cancelled: {SubscriptionId}", subscriptionId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
        throw;
    }
}
```

## Helper Methods
```csharp
private string GetEventDescription(string eventType)
{
    // Map event types to human-readable descriptions
    return eventType switch
    {
        "TaskCompleted" => "Triggered when a task is completed",
        "TaskFailed" => "Triggered when a task fails",
        "AgentStatusChanged" => "Triggered when agent status changes",
        _ => $"Event of type {eventType}"
    };
}

private EventBase CreateEventInstance(string eventType, Dictionary<string, object> eventData)
{
    // Create event instances based on type and data
    // This logic should be extracted to a separate service for reusability
    return eventType switch
    {
        "TaskCompleted" => new TaskCompletedEvent { /* map from eventData */ },
        "TaskFailed" => new TaskFailedEvent { /* map from eventData */ },
        _ => throw new ArgumentException($"Unknown event type: {eventType}")
    };
}
```

## Dependencies
- `IEventPublisher` (TODO-006)
- `ITypeMetadataService` (TODO-004)
- `IAgentDiscoveryService` (TODO-008)
- Existing subscription repository and models

## Testing Requirements
- Unit tests for each updated method
- Mock all new service dependencies
- Test event publishing through EventPublisher
- Test agent validation using discovery service
- Test type metadata integration
- Error handling and edge case tests
- Integration tests with real services
- Performance comparison with original implementation

## Error Handling Updates
```csharp
// Standardize error handling across methods
private void HandleServiceError(Exception ex, string operation, object context)
{
    _logger.LogError(ex, "Error during {Operation}: {Context}", operation, context);
    
    switch (ex)
    {
        case BusinessException:
            throw; // Re-throw business exceptions as-is
        case TimeoutException:
            throw new BusinessException("Service temporarily unavailable", "SERVICE_TIMEOUT");
        case HttpRequestException:
            throw new BusinessException("External service unavailable", "EXTERNAL_SERVICE_ERROR");
        default:
            throw new BusinessException($"Error during {operation}", "INTERNAL_ERROR");
    }
}
```

## Performance Considerations
- Compare performance with CreatorGAgent approach
- Monitor EventPublisher performance
- Optimize TypeMetadataService queries
- Cache frequently accessed type metadata
- Consider async patterns for all operations
- Profile memory usage changes

## Integration Points
- Maintain compatibility with existing subscription workflows
- Work with current API controllers
- Integrate with existing authentication/authorization
- Support current multi-tenancy patterns
- Compatible with existing monitoring and logging

## Dependency Injection Updates
```csharp
// In ServiceCollectionExtensions
services.AddScoped<IEventPublisher, EventPublisher>();
services.AddScoped<ITypeMetadataService, TypeMetadataService>();
services.AddScoped<IAgentDiscoveryService, AgentDiscoveryService>();

// Remove direct CreatorGAgent dependencies if no longer needed
```

## Success Metrics
- All subscription functionality preserved
- Event publishing performance maintained
- Zero API breaking changes
- Improved error handling and logging
- Reduced coupling to Orleans grains
- Successful integration with new services

## Migration Strategy
- Implement updates alongside existing code
- Use feature flags for gradual rollout
- Test thoroughly in staging environment
- Monitor subscription success rates
- Plan rollback strategy
- Clean up old code after successful migration

## Priority: Medium
This should be completed after the core services are implemented and AgentService updates are tested.