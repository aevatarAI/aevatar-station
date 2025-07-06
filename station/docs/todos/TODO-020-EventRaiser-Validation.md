# TODO-020: Add Validation to IMetaDataStateEventRaiser Default Methods

## Task Overview
Implement parameter validation and error handling in the IMetaDataStateEventRaiser interface's default methods.

## Description
Add comprehensive validation logic to ensure data integrity and provide meaningful error messages when invalid parameters are passed to the default methods.

## Acceptance Criteria
- [ ] Add parameter validation to all default methods
- [ ] Create custom exception types for validation errors
- [ ] Implement consistent validation patterns
- [ ] Add validation configuration options
- [ ] Create unit tests for all validation scenarios
- [ ] Document validation rules

## Validation Requirements

### CreateAgentAsync Validation
```csharp
async Task CreateAgentAsync(Guid id, Guid userId, string name, string agentType, Dictionary<string, string> properties = null)
{
    // Validations:
    if (id == Guid.Empty) 
        throw new ArgumentException("Agent ID cannot be empty", nameof(id));
    
    if (userId == Guid.Empty) 
        throw new ArgumentException("User ID cannot be empty", nameof(userId));
    
    if (string.IsNullOrWhiteSpace(name)) 
        throw new ArgumentException("Agent name cannot be null or empty", nameof(name));
    
    if (string.IsNullOrWhiteSpace(agentType)) 
        throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));
    
    if (name.Length > 255) 
        throw new ArgumentException("Agent name cannot exceed 255 characters", nameof(name));
    
    // Validate agent doesn't already exist
    if (GetState().Id != Guid.Empty)
        throw new InvalidOperationException("Agent has already been created");
}
```

### UpdateStatusAsync Validation
- [ ] Validate status transition rules
- [ ] Check if status change is allowed
- [ ] Validate reason length if provided
- [ ] Ensure agent exists (Id != Empty)

### UpdatePropertiesAsync Validation
- [ ] Validate property keys (no nulls, valid format)
- [ ] Validate property values (length limits)
- [ ] Check total properties count limit
- [ ] Validate reserved property names

### Custom Validation Attributes
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class ValidateParametersAttribute : Attribute
{
    public bool ThrowOnValidationError { get; set; } = true;
    public bool LogValidationErrors { get; set; } = true;
}
```

## Exception Types
```csharp
public class MetaDataValidationException : Exception
{
    public string ParameterName { get; }
    public object AttemptedValue { get; }
    public ValidationRule FailedRule { get; }
}

public class StateTransitionException : Exception
{
    public AgentStatus FromStatus { get; }
    public AgentStatus ToStatus { get; }
    public string Reason { get; }
}
```

## Validation Rules Configuration
```csharp
public class MetaDataValidationOptions
{
    public int MaxNameLength { get; set; } = 255;
    public int MaxPropertyKeyLength { get; set; } = 100;
    public int MaxPropertyValueLength { get; set; } = 1000;
    public int MaxPropertiesCount { get; set; } = 100;
    public bool AllowEmptyProperties { get; set; } = true;
    public string[] ReservedPropertyKeys { get; set; } = { "_id", "_type", "_version" };
    public Dictionary<AgentStatus, AgentStatus[]> AllowedStatusTransitions { get; set; }
}
```

## Validation Scenarios to Test
- [ ] Empty/null string parameters
- [ ] Guid.Empty for ID parameters
- [ ] String length exceeding limits
- [ ] Invalid characters in property keys
- [ ] Reserved property key usage
- [ ] Invalid status transitions
- [ ] Concurrent modification scenarios
- [ ] Property count limits
- [ ] Unicode and special characters

## Performance Considerations
- [ ] Cache validation rules
- [ ] Optimize regex patterns
- [ ] Lazy load validation configuration
- [ ] Minimal allocations in hot paths

## Integration Points
- [ ] Logging validation failures
- [ ] Metrics for validation errors
- [ ] Custom validation extensibility
- [ ] Localization of error messages

## Success Metrics
- Zero invalid data in event store
- Clear error messages for developers
- No performance regression
- Validation can be disabled for testing
- Easy to extend validation rules

## Notes
- Balance between safety and performance
- Consider making some validations optional
- Provide clear guidance on validation rules
- Allow for future extensibility

## Priority: Medium
Important for data integrity but not blocking initial implementation.