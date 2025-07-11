# TODO-016: Implement IMetaDataStateGAgent Interface

## Task Overview
Implement the `IMetaDataStateGAgent<TState>` helper interface that provides default method implementations for common event-raising patterns in GAgent-based systems.

## Description
Create a helper interface that works alongside GAgentBase to reduce boilerplate code when raising common metadata events. This interface uses .NET 8+ default interface methods and is optional for agents to implement.

## Acceptance Criteria
- [ ] Create `IMetaDataStateGAgent<TState>` interface in Aevatar.MetaData project
- [ ] Write failing unit tests FIRST for all default methods (TDD Red phase)
- [ ] Implement all default methods to make tests pass (TDD Green phase)
- [ ] Refactor implementation while keeping tests green (TDD Refactor phase)
- [ ] Ensure interface works alongside GAgentBase without modifying it
- [ ] Add comprehensive XML documentation
- [ ] Create integration tests with sample agents
- [ ] Write developer documentation as part of the implementation

## File Location
- `framework/src/Aevatar.MetaData/IMetaDataStateGAgent.cs`

## Implementation Details

### Interface Structure
```csharp
namespace Aevatar.MetaData
{
    /// <summary>
    /// Provides default implementations for common event-raising operations on metadata state.
    /// </summary>
    /// <typeparam name="TState">The state type that implements IMetaDataState</typeparam>
    public interface IMetaDataStateGAgent<TState> : IStateGAgent<TState> where TState : IMetaDataState
    {
        // Required methods that implementing class must provide
        void RaiseEvent(MetaDataStateLogEvent @event);
        Task ConfirmEvents();
        TState GetState();
        GrainId GetGrainId();
        
        // Default method implementations
        async Task CreateAgentAsync(Guid id, Guid userId, string name, string agentType, Dictionary<string, string> properties = null) { }
        async Task UpdateStatusAsync(AgentStatus newStatus, string reason = null) { }
        async Task UpdatePropertiesAsync(Dictionary<string, string> properties, bool merge = true) { }
        async Task RecordActivityAsync(string activityType = null) { }
        async Task SetPropertyAsync(string key, string value) { }
        async Task RemovePropertyAsync(string key) { }
        async Task BatchUpdateAsync(AgentStatus? newStatus = null, Dictionary<string, string> properties = null, bool mergeProperties = true, string statusReason = null) { }
    }
}
```

## Dependencies
- IMetaDataState interface (from TODO-002)
- MetaDataStateLogEvent and derived event classes
- GAgentBase public methods (from TODO-021)
- IStateGAgent<TState> interface

## Testing Requirements (TDD Approach)

### Unit Tests (Write FIRST)
- [ ] CreateAgentAsync - valid creation, null properties, event verification
- [ ] UpdateStatusAsync - with/without reason, old status capture, timestamp
- [ ] UpdatePropertiesAsync - merge/replace behavior, empty/null dictionaries
- [ ] RecordActivityAsync - with/without activity type, timestamp update
- [ ] SetPropertyAsync - add/update properties, special characters
- [ ] RemovePropertyAsync - existing/non-existent properties
- [ ] BatchUpdateAsync - status only, properties only, both, all null
- [ ] Edge cases - concurrent calls, state modifications, exceptions
- [ ] Performance benchmarks vs manual event creation

### Integration Tests
- [ ] Sample agent implementing the interface
- [ ] Test with real GAgentBase inheritance
- [ ] Orleans TestKit integration
- [ ] End-to-end event flow validation

## Documentation Requirements
- [ ] Comprehensive XML documentation on all public members
- [ ] Developer guide with overview and motivation
- [ ] Implementation tutorial with step-by-step examples
- [ ] API reference with method signatures and usage
- [ ] Best practices and common patterns
- [ ] Troubleshooting guide

## Success Metrics
- All tests written FIRST and passing (TDD compliance)
- 90%+ code coverage
- Reduces boilerplate code by 70%+ for common operations
- Zero impact on existing GAgentBase functionality
- Intellisense properly shows available methods
- Developer documentation reviewed and approved
- Examples compile and run successfully

## Notes
- This is a helper interface that agents optionally implement
- Does NOT modify GAgentBase or require framework changes
- Follows composition over inheritance principle
- Provides flexibility for custom event scenarios

## Priority: High
Essential for improving developer experience with the new metadata system.