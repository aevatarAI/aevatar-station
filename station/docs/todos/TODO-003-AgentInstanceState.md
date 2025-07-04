# TODO-003: Create AgentInstanceState Class Using IMetaDataState

## Task Overview
Create the `AgentInstanceState` class that implements `IMetaDataState` and inherits from `StateBase` for automatic Elasticsearch projection.

## Description
Implement the concrete state class that will replace `CreatorGAgentState` throughout the system. This class serves as the bridge between Orleans event sourcing and Elasticsearch projection.

## Acceptance Criteria
- [ ] Create `AgentInstanceState` class implementing `IMetaDataState`
- [ ] Inherit from `StateBase` for automatic projection
- [ ] Add Orleans serialization attributes
- [ ] Include `AgentStatus` enum definition
- [ ] Ensure proper field mapping for Elasticsearch
- [ ] Add validation and business rules
- [ ] Create comprehensive unit tests
- [ ] Verify Elasticsearch projection works correctly

## File Locations
- `station/src/Aevatar.Application.Grains/States/AgentInstanceState.cs`
- `station/src/Aevatar.Application.Grains/Enums/AgentStatus.cs`

## Implementation Details

### AgentInstanceState Class
```csharp
[GenerateSerializer]
public class AgentInstanceState : StateBase, IMetaDataState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string AgentType { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public Dictionary<string, string> Properties { get; set; } = new();
    [Id(5)] public GrainId AgentGrainId { get; set; }
    [Id(6)] public DateTime CreateTime { get; set; }
    [Id(7)] public AgentStatus Status { get; set; }
    [Id(8)] public DateTime LastActivity { get; set; }
}
```

### AgentStatus Enum
```csharp
[GenerateSerializer]
public enum AgentStatus
{
    [Id(0)] Initializing,
    [Id(1)] Active,
    [Id(2)] Inactive,
    [Id(3)] Error,
    [Id(4)] Deleted
}
```

### Key Requirements
- Must use Orleans serialization attributes correctly
- Properties should be initialized to safe defaults
- Thread-safe property access
- Elasticsearch-friendly field names and types
- Validation rules for required fields

## Dependencies
- `IMetaDataState` interface (TODO-002)
- Existing `StateBase` class
- Orleans serialization framework
- Elasticsearch projection pipeline

## Testing Requirements
- Unit tests for state transitions via Apply method
- Serialization/deserialization tests
- Elasticsearch projection integration tests
- Property validation tests
- Thread safety tests for concurrent access

## Integration Points
- Must work with existing StateBase projection pipeline
- Should be compatible with current Elasticsearch index structure
- Must integrate with Orleans grain lifecycle
- Needs to support existing multi-tenancy patterns

## Success Metrics
- State projections appear correctly in Elasticsearch
- All Orleans serialization works without errors
- Performance comparable to current CreatorGAgentState
- Zero data loss during state transitions
- Successful integration with existing monitoring

## Migration Considerations
- Plan for data migration from CreatorGAgentState
- Maintain backward compatibility during transition
- Consider versioning strategy for schema changes
- Document breaking changes and migration path

## Priority: High
This class is central to the new architecture and must be implemented before other services can be built.