# IMetaDataStateEventRaiser Integration Test Cases

## GAgentBase Integration Testing

### Orleans Event Sourcing Integration

#### Agent Creation Integration

##### TestMetaDataAgent implements IMetaDataStateEventRaiser and creates agent through Orleans event sourcing
###### Expected Result
- Agent grain is created successfully using Orleans TestKit
- CreateAgentAsync method raises AgentCreatedEvent
- Event is persisted through Orleans event sourcing
- Agent state is updated with correct properties (Id, UserId, Name, AgentType)
- Agent status is set to Creating
- Orleans grain state reflects the metadata changes

##### TestMetaDataAgent handles CreateAgentAsync with null properties
###### Expected Result
- AgentCreatedEvent is raised with empty properties dictionary
- Agent state Properties field is initialized as empty dictionary
- No exceptions are thrown during Orleans event processing

#### Status Update Integration

##### TestMetaDataAgent updates status from Creating to Active through Orleans event sourcing
###### Expected Result
- UpdateStatusAsync method raises AgentStatusChangedEvent
- Event contains correct OldStatus (Creating) and NewStatus (Active)
- Orleans event sourcing persists the status change
- Agent state Status field is updated to Active
- LastActivity timestamp is updated

##### TestMetaDataAgent updates status with reason through Orleans event sourcing
###### Expected Result
- AgentStatusChangedEvent contains the provided reason
- Status change is persisted with reason metadata
- Agent state reflects the new status

#### Property Update Integration

##### TestMetaDataAgent updates properties through Orleans event sourcing with merge=true
###### Expected Result
- UpdatePropertiesAsync method raises AgentPropertiesUpdatedEvent
- Event contains correct UpdatedProperties and WasMerged=true
- Orleans event sourcing persists the property changes
- Agent state Properties dictionary is updated/merged correctly
- Existing properties are preserved when merging

##### TestMetaDataAgent updates properties through Orleans event sourcing with merge=false
###### Expected Result
- AgentPropertiesUpdatedEvent has WasMerged=false
- Agent state Properties dictionary is replaced entirely
- Previous properties are cleared when not merging

##### TestMetaDataAgent removes property through Orleans event sourcing
###### Expected Result
- RemovePropertyAsync method raises AgentPropertiesUpdatedEvent
- Event contains property key in RemovedProperties list
- Orleans event sourcing persists the property removal
- Agent state Properties dictionary no longer contains the key

#### Activity Recording Integration

##### TestMetaDataAgent records activity through Orleans event sourcing
###### Expected Result
- RecordActivityAsync method raises AgentActivityUpdatedEvent
- Event contains correct ActivityType and ActivityTime
- Orleans event sourcing persists the activity record
- Agent state LastActivity timestamp is updated

##### TestMetaDataAgent records activity with null activity type
###### Expected Result
- AgentActivityUpdatedEvent uses default "activity" type
- Activity is properly recorded with default type

#### Batch Update Integration

##### TestMetaDataAgent performs batch update of status and properties through Orleans event sourcing
###### Expected Result
- BatchUpdateAsync method raises multiple events in sequence
- AgentStatusChangedEvent is raised first
- AgentPropertiesUpdatedEvent is raised second
- AgentActivityUpdatedEvent is raised third with "batch_update" type
- All events are persisted through Orleans event sourcing
- Agent state reflects all changes from the batch operation

##### TestMetaDataAgent performs batch update with only status
###### Expected Result
- Only AgentStatusChangedEvent and AgentActivityUpdatedEvent are raised
- No property update event is raised
- Orleans event sourcing processes both events correctly

##### TestMetaDataAgent performs batch update with only properties
###### Expected Result
- Only AgentPropertiesUpdatedEvent and AgentActivityUpdatedEvent are raised
- No status change event is raised
- Orleans event sourcing processes both events correctly

#### Event Sourcing Consistency

##### TestMetaDataAgent maintains event sourcing consistency across deactivation and reactivation
###### Expected Result
- Agent state is correctly restored from event log after deactivation
- All metadata events are replayed in correct order
- Agent state after reactivation matches state before deactivation
- No data loss occurs during grain lifecycle

##### TestMetaDataAgent handles concurrent operations correctly
###### Expected Result
- Multiple concurrent metadata operations are serialized correctly
- Event sourcing maintains consistency under concurrent load
- No race conditions occur in event processing
- Agent state remains consistent across concurrent operations

### Interface Implementation Verification

#### IMetaDataStateEventRaiser Method Delegation

##### TestMetaDataAgent correctly delegates RaiseEvent to Orleans event sourcing
###### Expected Result
- RaiseEvent method calls Orleans grain's RaiseEvent<T> method
- Events are properly typed and serialized
- Event sourcing system receives and processes events

##### TestMetaDataAgent correctly delegates ConfirmEvents to Orleans event sourcing
###### Expected Result
- ConfirmEvents method calls Orleans grain's ConfirmEvents method
- Event sourcing system commits pending events
- State changes are persisted to storage

##### TestMetaDataAgent correctly returns current state via GetState
###### Expected Result
- GetState method returns current Orleans grain state
- State reflects all applied metadata events
- State is properly typed as MetaDataStateBase

##### TestMetaDataAgent correctly returns GrainId via GetGrainId
###### Expected Result
- GetGrainId method returns Orleans grain's GrainId
- GrainId is properly formatted and unique
- GrainId matches the grain instance identifier

### Error Handling Integration

#### Exception Handling in Orleans Context

##### TestMetaDataAgent handles exceptions during event processing
###### Expected Result
- Exceptions during event application are caught and logged
- Orleans grain lifecycle is not disrupted
- Agent state remains consistent despite errors
- Error information is available for debugging

##### TestMetaDataAgent handles storage provider failures
###### Expected Result
- Storage failures are handled gracefully
- Agent continues to function with in-memory state
- Error conditions are logged appropriately
- Recovery is possible when storage is restored

### Performance Integration

#### Orleans Event Sourcing Performance

##### TestMetaDataAgent maintains acceptable performance under load
###### Expected Result
- Metadata operations complete within acceptable time limits
- Event sourcing doesn't introduce significant overhead
- Agent can handle reasonable concurrent operation load
- Memory usage remains stable during extended operation

##### TestMetaDataAgent efficiently handles large property dictionaries
###### Expected Result
- Large property updates are processed efficiently
- Event serialization handles large payloads
- Orleans storage can accommodate large state objects
- Performance degrades gracefully with data size increases