# TODO-010: Update AgentService to Use New Architecture Services

## Task Overview
Update the existing `AgentService` class to use the new architecture services (AgentLifecycleService, AgentDiscoveryService, etc.) instead of directly interacting with CreatorGAgent grains.

## Description
Refactor the AgentService implementation to utilize the new service layer while maintaining the same public API. This involves replacing all CreatorGAgent interactions with calls to the appropriate new services, ensuring a clean separation of concerns and improved testability.

## Acceptance Criteria
- [ ] Replace all CreatorGAgent usage with new services
- [ ] Maintain existing public API contracts
- [ ] Update dependency injection configuration
- [ ] Preserve all existing functionality
- [ ] Add comprehensive unit tests for updated methods
- [ ] Ensure performance is maintained or improved
- [ ] Update error handling to use new service patterns
- [ ] Add integration tests with new services
- [ ] Update logging to reflect new architecture
- [ ] Maintain backward compatibility during transition

## File Locations
- `station/src/Aevatar.Application/Service/AgentService.cs` (update existing)
- `station/src/Aevatar.Application/Service/IAgentService.cs` (update if needed)

## Current AgentService Methods to Update

### Agent CRUD Operations
- `CreateAgentAsync()` - Use AgentLifecycleService
- `UpdateAgentAsync()` - Use AgentLifecycleService  
- `DeleteAgentAsync()` - Use AgentLifecycleService
- `GetAgentAsync()` - Use AgentLifecycleService or AgentDiscoveryService
- `GetAgentListAsync()` - Use AgentDiscoveryService

### Agent Relationship Management
- `AddSubAgentAsync()` - Use AgentLifecycleService
- `RemoveSubAgentAsync()` - Use AgentLifecycleService
- `RemoveAllSubAgentAsync()` - Use AgentLifecycleService
- `GetAgentRelationshipAsync()` - Use AgentDiscoveryService

### Event Management
- `PublishEventAsync()` - Use EventPublisher
- `UpdateAvailableEventsAsync()` - Use TypeMetadataService

## Implementation Details

### Updated Constructor and Dependencies
```csharp
public class AgentService : IAgentService
{
    private readonly IAgentLifecycleService _lifecycleService;
    private readonly IAgentDiscoveryService _discoveryService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITypeMetadataService _typeMetadataService;
    private readonly ILogger<AgentService> _logger;
    
    public AgentService(
        IAgentLifecycleService lifecycleService,
        IAgentDiscoveryService discoveryService,
        IEventPublisher eventPublisher,
        ITypeMetadataService typeMetadataService,
        ILogger<AgentService> logger)
    {
        _lifecycleService = lifecycleService;
        _discoveryService = discoveryService;
        _eventPublisher = eventPublisher;
        _typeMetadataService = typeMetadataService;
        _logger = logger;
    }
}
```

### CreateAgentAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent
public async Task<AgentDto> CreateAgentAsync(CreateAgentInput input)
{
    var agentData = new AgentData
    {
        UserId = input.UserId,
        AgentType = input.AgentType,
        Name = input.Name,
        Properties = input.Properties,
        BusinessAgentGrainId = Guid.NewGuid()
    };
    
    var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(agentData.BusinessAgentGrainId);
    await creatorAgent.CreateAgentAsync(agentData);
    
    var state = await creatorAgent.GetAgentAsync();
    return MapToDto(state);
}

// AFTER: Using new services
public async Task<AgentDto> CreateAgentAsync(CreateAgentInput input)
{
    var request = new CreateAgentRequest
    {
        UserId = input.UserId,
        AgentType = input.AgentType,
        Name = input.Name,
        Properties = input.Properties
    };
    
    var agentInfo = await _lifecycleService.CreateAgentAsync(request);
    return MapToDto(agentInfo);
}
```

### GetAgentListAsync Method Update
```csharp
// BEFORE: Manual grain queries
public async Task<List<AgentDto>> GetAgentListAsync(GetAgentListInput input)
{
    // Complex logic to get all agent grains and query their states
    // Performance issues with large numbers of agents
}

// AFTER: Using AgentDiscoveryService
public async Task<List<AgentDto>> GetAgentListAsync(GetAgentListInput input)
{
    var query = new AgentDiscoveryQuery
    {
        UserId = input.UserId,
        Status = input.Status,
        Skip = input.Skip,
        Take = input.Take,
        SortBy = input.SortBy,
        SortOrder = input.SortOrder
    };
    
    var agents = await _discoveryService.FindAgentsAsync(query);
    return agents.Select(MapToDto).ToList();
}
```

### PublishEventAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent
public async Task PublishEventAsync(PublishEventInput input)
{
    var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(input.AgentId);
    await creatorAgent.PublishEventAsync(input.Event);
}

// AFTER: Using EventPublisher
public async Task PublishEventAsync(PublishEventInput input)
{
    await _eventPublisher.PublishEventAsync(input.Event, input.AgentId.ToString());
}
```

### GetAvailableEventsAsync Method Update
```csharp
// BEFORE: Using CreatorGAgent state
public async Task<List<EventTypeDto>> GetAvailableEventsAsync(Guid agentId)
{
    var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(agentId);
    var state = await creatorAgent.GetAgentAsync();
    return state.EventInfoList.Select(MapToEventTypeDto).ToList();
}

// AFTER: Using TypeMetadataService
public async Task<List<EventTypeDto>> GetAvailableEventsAsync(Guid agentId)
{
    var agentInfo = await _discoveryService.FindAgentByIdAsync(agentId);
    var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(agentInfo.AgentType);
    
    return typeMetadata.Capabilities.Select(capability => new EventTypeDto
    {
        EventType = capability,
        Description = GetEventDescription(capability)
    }).ToList();
}
```

## Dependencies
- `IAgentLifecycleService` (TODO-005)
- `IAgentDiscoveryService` (TODO-008)
- `IEventPublisher` (TODO-006)
- `ITypeMetadataService` (TODO-004)
- Existing DTOs and input models

## Testing Requirements
- Unit tests for each updated method
- Mock all new service dependencies
- Verify same output as original implementation
- Test error handling with new services
- Performance comparison tests
- Integration tests with real services
- Backward compatibility tests

## Data Mapping Updates
```csharp
// Update mapping methods to work with new models
private AgentDto MapToDto(AgentInfo agentInfo)
{
    return new AgentDto
    {
        Id = agentInfo.Id,
        UserId = agentInfo.UserId,
        AgentType = agentInfo.AgentType,
        Name = agentInfo.Name,
        Properties = agentInfo.Properties,
        Capabilities = agentInfo.Capabilities,
        Status = agentInfo.Status,
        CreatedAt = agentInfo.CreatedAt,
        LastActivity = agentInfo.LastActivity
    };
}

// Remove mapping from CreatorGAgentState
private AgentDto MapToDto(CreatorGAgentState state) // DELETE THIS METHOD
```

## Error Handling Updates
```csharp
// Update error handling to work with new service exceptions
public async Task<AgentDto> GetAgentAsync(Guid agentId)
{
    try
    {
        var agentInfo = await _lifecycleService.GetAgentAsync(agentId);
        return MapToDto(agentInfo);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Agent not found"))
    {
        throw new BusinessException($"Agent {agentId} not found", "AGENT_NOT_FOUND");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving agent {AgentId}", agentId);
        throw new BusinessException("Error retrieving agent", "AGENT_RETRIEVAL_ERROR");
    }
}
```

## Performance Considerations
- Compare performance with original CreatorGAgent implementation
- Monitor Elasticsearch query performance
- Optimize service call patterns
- Consider caching for frequently accessed data
- Batch operations where possible
- Profile memory usage changes

## Dependency Injection Updates
```csharp
// In ServiceCollectionExtensions or Startup
services.AddScoped<IAgentLifecycleService, AgentLifecycleService>();
services.AddScoped<IAgentDiscoveryService, AgentDiscoveryService>();
services.AddScoped<IEventPublisher, EventPublisher>();
services.AddScoped<ITypeMetadataService, TypeMetadataService>();

// Remove CreatorGAgent dependencies
// services.AddSingleton<IClusterClient>(...) // Keep if needed for other purposes
```

## Migration Strategy
- Implement new methods alongside existing ones
- Use feature flags to switch between implementations
- Test thoroughly in staging environment
- Monitor performance and error rates
- Gradual rollout with rollback capability
- Clean up old code after successful migration

## Logging Updates
```csharp
// Update logging to reflect new service architecture
_logger.LogInformation("Creating agent using AgentLifecycleService: {AgentType}", request.AgentType);
_logger.LogInformation("Agent discovery query: {Query}", JsonSerializer.Serialize(query));
_logger.LogInformation("Publishing event via EventPublisher: {EventType}", @event.GetType().Name);
```

## Success Metrics
- All existing functionality preserved
- Performance maintained or improved
- Zero API contract breaking changes
- Successful unit and integration tests
- Reduced code complexity and coupling
- Improved error handling and logging

## Rollback Plan
- Keep original implementation in separate methods
- Use feature flags to switch implementations
- Maintain original dependencies during transition
- Document rollback procedures
- Monitor error rates and performance metrics

## Priority: Medium
This task should be completed after the core services (TODO-002 through TODO-008) are implemented and tested.