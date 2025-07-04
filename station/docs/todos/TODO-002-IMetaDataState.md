# TODO-002: Create IMetaDataState Interface and Event Classes

## Task Overview
Create the `IMetaDataState` interface with default Apply implementation using .NET 8+ default interface methods, along with related event classes for agent metadata state management.

## Description
Implement the foundational interface and event classes that will replace `CreatorGAgentState` with better separation of concerns and automatic event sourcing support.

## Acceptance Criteria
- [ ] Create `IMetaDataState` interface with default Apply method
- [ ] Create base `MetaDataStateLogEvent` class
- [ ] Create specific event classes: `AgentCreatedEvent`, `AgentStatusChangedEvent`, `AgentPropertiesUpdatedEvent`, `AgentActivityUpdatedEvent`
- [ ] Add Orleans serialization attributes
- [ ] Ensure compatibility with existing event sourcing pipeline
- [ ] Add comprehensive unit tests

## File Locations
- `framework/src/Aevatar.Core/Abstractions/IMetaDataState.cs`
- `framework/src/Aevatar.Core/Events/MetaDataStateLogEvent.cs`
- `framework/src/Aevatar.Core/Events/AgentCreatedEvent.cs`
- `framework/src/Aevatar.Core/Events/AgentStatusChangedEvent.cs`
- `framework/src/Aevatar.Core/Events/AgentPropertiesUpdatedEvent.cs`
- `framework/src/Aevatar.Core/Events/AgentActivityUpdatedEvent.cs`

## Implementation Details

### IMetaDataState Interface
```csharp
public interface IMetaDataState
{
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string AgentType { get; set; }
    string Name { get; set; }
    Dictionary<string, string> Properties { get; set; }
    GrainId AgentGrainId { get; set; }
    DateTime CreateTime { get; set; }
    AgentStatus Status { get; set; }
    DateTime LastActivity { get; set; }
    
    // Default Apply method implementation (.NET 8+ feature)
    void Apply(MetaDataStateLogEvent @event) { /* implementation */ }
}
```

### Event Classes
All event classes must:
- Inherit from `MetaDataStateLogEvent`
- Use `[GenerateSerializer]` attribute
- Use `[Id(n)]` attributes for properties
- Follow Orleans serialization best practices

## Dependencies
- Orleans serialization framework
- Existing `StateLogEventBase<T>` pattern
- `AgentStatus` enum

## Testing Requirements
- Unit tests for default Apply method with all event types
- Serialization/deserialization tests
- Event application state transition tests
- Null handling and edge case tests

## Success Metrics
- All tests pass
- Compatible with existing event sourcing pipeline
- Performance comparable to current implementation
- No serialization issues in Orleans cluster

## Notes
- This interface replaces functionality from `CreatorGAgentState`
- Must maintain compatibility with existing Orleans infrastructure
- Focus on immutability and thread safety
- Consider future extensibility for additional metadata

## Priority: High
This is foundational for the entire architecture replacement.