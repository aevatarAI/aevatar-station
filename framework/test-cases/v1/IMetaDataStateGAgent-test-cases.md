# IMetaDataStateGAgent Test Cases

## CreateAgentAsync Method
### Valid Agent Creation
#### Standard Creation with All Required Parameters
##### Agent created with valid id, userId, name, agentType and null properties
###### Expected Result
- AgentCreatedEvent is raised with correct values
- Event contains all provided parameters
- Properties dictionary is empty (not null)
- AgentGrainId matches GetGrainId() result
- InitialStatus is AgentStatus.Creating
- ConfirmEvents is called once

##### Agent created with valid parameters and initial properties
###### Expected Result
- AgentCreatedEvent contains provided properties
- Properties are stored as provided
- All other event fields populated correctly

#### Boundary Value Analysis
##### Agent created with empty string name
###### Expected Result
- Event is raised with empty name
- No validation error (interface doesn't validate)

##### Agent created with very long name (1000 characters)
###### Expected Result
- Event is raised with full name
- No truncation occurs

#### Error Guessing
##### Agent created with special characters in properties
###### Expected Result
- Properties with special characters are preserved
- No encoding issues occur

### Invalid Scenarios
#### Null Parameter Handling
##### CreateAgentAsync called with null agentType
###### Expected Result
- Event is raised with null agentType
- No null reference exception

## UpdateStatusAsync Method
### Valid Status Updates
#### Status Change with Reason
##### Update from Creating to Active with reason
###### Expected Result
- AgentStatusChangedEvent raised
- OldStatus captures current state status
- NewStatus is the provided value
- Reason is included in event
- StatusChangeTime is set to current UTC time

##### Update status without reason (null)
###### Expected Result
- Event raised with null reason
- All other fields populated correctly

#### Boundary Value Analysis
##### Update with very long reason (10000 characters)
###### Expected Result
- Full reason text is preserved
- No truncation or errors

#### State Transition Testing
##### Update from each status to every other status
###### Expected Result
- All status transitions work correctly
- OldStatus always reflects current state

## UpdatePropertiesAsync Method
### Merge Behavior Testing
#### Properties Merge (merge=true)
##### Add new properties to existing ones
###### Expected Result
- UpdatedProperties contains only new/changed properties
- RemovedProperties is empty list
- WasMerged is true
- UpdateTime is current UTC

##### Update existing properties with merge
###### Expected Result
- Only changed properties in UpdatedProperties
- No properties in RemovedProperties
- Existing unchanged properties not included

#### Replace Behavior Testing
##### Replace all properties (merge=false)
###### Expected Result
- UpdatedProperties contains all new properties
- RemovedProperties contains keys not in new properties
- WasMerged is false

##### Replace with empty dictionary
###### Expected Result
- UpdatedProperties is empty
- RemovedProperties contains all previous property keys

### Boundary Value Analysis
#### Empty and Null Dictionaries
##### Update with empty dictionary (merge=true)
###### Expected Result
- No event raised or empty UpdatedProperties
- RemovedProperties is empty

##### Update with null dictionary
###### Expected Result
- Handles gracefully without null reference exception

## RecordActivityAsync Method
### Activity Recording
#### With Activity Type
##### Record activity with specific type
###### Expected Result
- AgentActivityUpdatedEvent raised
- ActivityType matches provided value
- ActivityTime is current UTC
- AgentId and UserId from current state

#### Without Activity Type
##### Record activity with null type
###### Expected Result
- ActivityType is empty string (not null)
- All other fields populated correctly

### Boundary Value Analysis
##### Record with very long activity type
###### Expected Result
- Full activity type preserved
- No truncation

## SetPropertyAsync Method
### Single Property Updates
#### Add New Property
##### Set property that doesn't exist
###### Expected Result
- Calls UpdatePropertiesAsync with single-item dictionary
- Merge parameter is true
- Property is added to state

#### Update Existing Property
##### Set property that already exists
###### Expected Result
- UpdatePropertiesAsync called with new value
- Only specified property updated

### Special Cases
#### Property Key Edge Cases
##### Set property with empty key
###### Expected Result
- Property set with empty key
- No validation error

##### Set property with special characters in key
###### Expected Result
- Property key preserved exactly
- No encoding issues

## RemovePropertyAsync Method
### Property Removal
#### Remove Existing Property
##### Remove property that exists
###### Expected Result
- UpdatePropertiesAsync called with all properties except removed
- Merge parameter is false
- Property no longer in state

#### Remove Non-existent Property
##### Remove property that doesn't exist
###### Expected Result
- No error thrown
- State remains unchanged
- UpdatePropertiesAsync still called

## BatchUpdateAsync Method
### Single Update Scenarios
#### Status Only Update
##### Batch update with only status change
###### Expected Result
- Only AgentStatusChangedEvent raised
- No properties event
- ConfirmEvents called once

#### Properties Only Update
##### Batch update with only properties
###### Expected Result
- Only AgentPropertiesUpdatedEvent raised
- No status event
- Merge behavior respects parameter

### Combined Updates
#### Status and Properties Together
##### Update both status and properties in one call
###### Expected Result
- Both events raised in order
- Status event first, then properties
- Single ConfirmEvents call
- All parameters respected

### Null Parameter Handling
#### All Parameters Null
##### Batch update with all null parameters
###### Expected Result
- No events raised
- ConfirmEvents still called
- No errors thrown

#### Mixed Null and Valid Parameters
##### Status null, properties provided
###### Expected Result
- Only properties event raised
- Status unchanged

## Concurrency and State Consistency
### Concurrent Method Calls
#### Multiple Updates in Parallel
##### Two UpdateStatusAsync calls simultaneously
###### Expected Result
- Both events raised
- Events processed in order
- Final state reflects both updates

### State Modification During Operation
#### State Changed Between GetState Calls
##### State modified externally during method execution
###### Expected Result
- Event uses state at time of GetState call
- No race conditions

## Performance Testing
### Large Data Volumes
#### Update with Large Property Dictionary
##### Update with 1000 properties
###### Expected Result
- All properties processed
- Reasonable performance
- No memory issues

### Rapid Sequential Calls
#### Many Updates in Quick Succession
##### 100 status updates in loop
###### Expected Result
- All events raised and confirmed
- No event loss
- State remains consistent