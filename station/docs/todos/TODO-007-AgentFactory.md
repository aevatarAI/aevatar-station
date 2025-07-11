# TODO-007: Create AgentFactory Service for Standardized Agent Creation

## Task Overview
Create the `AgentFactory` service that provides standardized agent creation and configuration, working with the TypeMetadataService to validate agent types and create properly configured Orleans grains.

## Description
Implement the factory service that abstracts agent grain creation complexity. This service validates agent types against metadata, creates proper grain IDs, and returns configured agent instances ready for initialization.

## Acceptance Criteria
- [ ] Create `IAgentFactory` interface
- [ ] Implement `AgentFactory` class
- [ ] Add agent type validation
- [ ] Create proper Orleans grain ID generation
- [ ] Support agent configuration patterns
- [ ] Add error handling for unknown agent types
- [ ] Create comprehensive unit tests
- [ ] Add integration tests with real agent types
- [ ] Support dynamic agent type discovery
- [ ] Add performance monitoring

## File Locations
- `station/src/Aevatar.Application/Services/IAgentFactory.cs`
- `station/src/Aevatar.Application/Services/AgentFactory.cs`
- `station/src/Aevatar.Application/Models/AgentConfiguration.cs`

## Implementation Details

### IAgentFactory Interface
```csharp
public interface IAgentFactory
{
    Task<IGAgent> CreateAgentAsync(string agentType, AgentConfiguration config);
    Task<bool> SupportsAgentTypeAsync(string agentType);
    Task<List<string>> GetSupportedAgentTypesAsync();
    Task<T> CreateTypedAgentAsync<T>(AgentConfiguration config) where T : IGAgent;
}
```

### AgentConfiguration Model
```csharp
public class AgentConfiguration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> RequiredCapabilities { get; set; } = new();
    public string TenantId { get; set; }
}
```

### Core Dependencies
- `IGrainFactory` for Orleans grain creation
- `ITypeMetadataService` for agent type validation
- `ILogger<AgentFactory>` for logging

### Key Features

#### Agent Type Validation
- Verify agent type exists in metadata service
- Check required capabilities are supported
- Validate configuration parameters
- Ensure proper grain interface mapping

#### Grain ID Generation
- Create consistent grain IDs using agent type and instance ID
- Support tenant isolation in grain IDs
- Handle special characters and formatting
- Ensure uniqueness across cluster

#### Configuration Support
- Map generic configuration to agent-specific properties
- Validate required configuration parameters
- Provide default values where appropriate
- Support configuration inheritance patterns

## Grain Creation Strategy

### Standard Grain Creation
```csharp
public async Task<IGAgent> CreateAgentAsync(string agentType, AgentConfiguration config)
{
    // 1. Validate agent type
    var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(agentType);
    if (typeMetadata == null)
        throw new InvalidOperationException($"Unknown agent type: {agentType}");
    
    // 2. Create grain ID
    var grainId = GrainId.Create(agentType, config.Id.ToString());
    
    // 3. Get grain reference
    var agent = _grainFactory.GetGrain<IGAgent>(grainId);
    
    return agent;
}
```

### Typed Agent Creation
```csharp
public async Task<T> CreateTypedAgentAsync<T>(AgentConfiguration config) where T : IGAgent
{
    var agentType = typeof(T).Name;
    var agent = await CreateAgentAsync(agentType, config);
    return (T)agent;
}
```

## Dependencies
- `ITypeMetadataService` (TODO-004)
- Orleans `IGrainFactory`
- Agent type metadata
- Orleans grain interfaces

## Testing Requirements
- Unit tests for grain creation with mocked dependencies
- Agent type validation tests
- Grain ID generation tests
- Configuration mapping tests
- Error handling tests for invalid types
- Performance tests for bulk creation
- Integration tests with real agent types

## Error Handling Strategy
- Validate agent type before grain creation
- Handle grain activation failures
- Provide meaningful error messages
- Log all creation attempts and failures
- Support graceful degradation
- Implement retry logic for transient failures

## Performance Considerations
- Cache type metadata lookups
- Optimize grain ID generation
- Support bulk creation operations
- Monitor grain activation times
- Use async patterns throughout
- Consider grain warming strategies

## Configuration Validation
```csharp
public class AgentConfigurationValidator
{
    public async Task<ValidationResult> ValidateAsync(string agentType, AgentConfiguration config)
    {
        var result = new ValidationResult();
        
        // Validate required fields
        if (string.IsNullOrEmpty(config.Name))
            result.AddError("Agent name is required");
            
        // Validate agent type capabilities
        var metadata = await _typeMetadataService.GetTypeMetadataAsync(agentType);
        foreach (var capability in config.RequiredCapabilities)
        {
            if (!metadata.Capabilities.Contains(capability))
                result.AddError($"Agent type {agentType} does not support capability {capability}");
        }
        
        return result;
    }
}
```

## Grain ID Patterns
- Format: `{agentType}_{tenantId}_{instanceId}`
- Example: `BusinessAgent_tenant123_agent456`
- Support for special characters escaping
- Consistent across cluster nodes
- Compatible with Orleans routing

## Integration Points
- Used by AgentLifecycleService for agent creation
- Integrates with TypeMetadataService for validation
- Works with Orleans grain infrastructure
- Supports existing multi-tenancy patterns
- Compatible with current grain lifecycle

## Multi-Tenancy Support
- Include tenant information in grain IDs
- Validate tenant access during creation
- Support tenant-specific configuration
- Ensure proper isolation between tenants
- Audit creation events per tenant

## Monitoring and Diagnostics
- Track agent creation rates and success rates
- Monitor grain activation times
- Log configuration validation failures
- Provide health check endpoints
- Support diagnostic queries

## Security Considerations
- Validate user permissions for agent type creation
- Sanitize configuration parameters
- Audit all agent creation operations
- Prevent unauthorized agent type usage
- Secure configuration parameter handling

## Success Metrics
- 100% successful agent creation for valid requests
- Sub-50ms grain creation time
- Zero grain ID collisions
- Successful integration with all agent types
- Proper error handling for all failure scenarios

## Future Extensibility
- Support for agent type plugins
- Dynamic agent type registration
- Agent template and blueprint support
- Advanced configuration inheritance
- Agent lifecycle hooks

## Priority: High
This service is essential for standardized agent creation and must be implemented before the AgentLifecycleService can function properly.