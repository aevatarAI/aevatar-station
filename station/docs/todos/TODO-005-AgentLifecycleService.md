# TODO-005: Create AgentLifecycleService Interface and Implementation

## Task Overview
Create the `AgentLifecycleService` that centralizes agent CRUD operations, replacing CreatorGAgent's factory responsibilities with a dedicated service.

## Description
Implement the service that handles agent creation, updates, deletion, and retrieval. This service acts as the primary interface for agent lifecycle management, coordinating between type metadata, agent factory, and direct agent access.

## Acceptance Criteria
- [ ] Create `IAgentLifecycleService` interface
- [ ] Implement `AgentLifecycleService` class
- [ ] Create supporting request/response models
- [ ] Add agent validation logic
- [ ] Implement error handling and logging
- [ ] Add comprehensive unit tests
- [ ] Create integration tests with mock dependencies
- [ ] Ensure proper async/await patterns
- [ ] Add performance monitoring hooks

## File Locations
- `station/src/Aevatar.Application/Services/IAgentLifecycleService.cs`
- `station/src/Aevatar.Application/Services/AgentLifecycleService.cs`
- `station/src/Aevatar.Application/Models/CreateAgentRequest.cs`
- `station/src/Aevatar.Application/Models/UpdateAgentRequest.cs`
- `station/src/Aevatar.Application/Models/AgentInfo.cs`

## Implementation Details

### IAgentLifecycleService Interface
```csharp
public interface IAgentLifecycleService
{
    Task<AgentInfo> CreateAgentAsync(CreateAgentRequest request);
    Task<AgentInfo> UpdateAgentAsync(Guid agentId, UpdateAgentRequest request);
    Task DeleteAgentAsync(Guid agentId);
    Task<AgentInfo> GetAgentAsync(Guid agentId);
    Task<List<AgentInfo>> GetUserAgentsAsync(Guid userId);
    Task SendEventToAgentAsync(Guid agentId, EventBase @event);
    Task<AgentInfo> AddSubAgentAsync(Guid parentId, Guid childId);
    Task<AgentInfo> RemoveSubAgentAsync(Guid parentId, Guid childId);
    Task<AgentInfo> RemoveAllSubAgentsAsync(Guid parentId);
}
```

### Core Dependencies
- `ITypeMetadataService` for agent type validation
- `IAgentFactory` for agent creation
- `IEventPublisher` for event sending
- `IElasticsearchClient` for agent querying
- `ILogger<AgentLifecycleService>` for logging

### Key Operations

#### Agent Creation Flow
1. Validate agent type via TypeMetadataService
2. Create agent configuration
3. Use AgentFactory to create and initialize agent
4. Return AgentInfo with combined metadata

#### Agent Update Flow
1. Query Elasticsearch for existing agent state
2. Get agent grain via AgentFactory
3. Execute update operations on agent
4. Return updated AgentInfo

#### Agent Deletion Flow
1. Verify agent exists via Elasticsearch
2. Get agent grain and execute deletion
3. Handle cleanup and state transitions

## Request/Response Models

### CreateAgentRequest
```csharp
public class CreateAgentRequest
{
    public Guid UserId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}
```

### UpdateAgentRequest
```csharp
public class UpdateAgentRequest
{
    public string Name { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}
```

### AgentInfo
```csharp
public class AgentInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public List<string> Capabilities { get; set; }
    public AgentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
}
```

## Dependencies
- `ITypeMetadataService` (TODO-004)
- `IAgentFactory` (TODO-007)
- `IEventPublisher` (TODO-006)
- `AgentInstanceState` (TODO-003)
- Elasticsearch client
- Orleans cluster client

## Testing Requirements
- Unit tests for each CRUD operation
- Mock all external dependencies
- Test error scenarios and edge cases
- Validate request/response transformations
- Test async operation cancellation
- Performance tests for batch operations
- Integration tests with real dependencies

## Error Handling Strategy
- Validate all input parameters
- Handle agent type not found scenarios
- Manage Elasticsearch query failures
- Deal with Orleans grain activation issues
- Provide meaningful error messages
- Log all operations for troubleshooting

## Performance Considerations
- Batch operations for bulk agent queries
- Cache frequently accessed agent metadata
- Optimize Elasticsearch queries
- Use async/await properly throughout
- Consider pagination for large result sets
- Monitor and log operation timings

## Security Considerations
- Validate user access to agents (multi-tenancy)
- Sanitize input parameters
- Ensure proper authorization checks
- Audit all agent lifecycle operations
- Protect against injection attacks

## Integration Points
- Replace CreatorGAgent usage in AgentService
- Work with existing API controllers
- Integrate with current authentication/authorization
- Support existing multi-tenancy patterns
- Maintain compatibility with SignalR hubs

## Success Metrics
- All CRUD operations complete successfully
- Performance comparable or better than CreatorGAgent
- Zero data corruption during operations
- Proper error handling in all scenarios
- Successful integration with dependent services

## Migration Strategy
- Implement alongside existing CreatorGAgent initially
- Gradually replace usage in service layer
- Maintain API compatibility during transition
- Plan for parallel operation during migration
- Document breaking changes

## Priority: High
This service is central to replacing CreatorGAgent and must be implemented before updating the service layer.