# LLM Configuration Centralization Proposal

## Problem Statement

The current AIGAgent implementation stores LLM configuration (including sensitive API keys) directly in the agent's state via `AIGAgentStateBase.LLM`. This creates significant operational challenges in production environments:

1. **Configuration Updates**: Changing LLM configurations requires modifying every individual agent's state
2. **Security Concerns**: API keys are duplicated across all agent instances
3. **Memory Inefficiency**: Full LLM configurations are stored in every agent state
4. **Maintenance Overhead**: No centralized way to manage LLM provider configurations

## Current Architecture Analysis

### State Storage Model
```csharp
[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }           // Resolved full config (problematic)
    [Id(1)] public string? SystemLLM { get; set; }       // Reference key (good)
    // ... other fields
}
```

### Configuration Resolution
The system currently uses a **dual storage model**:
- `SystemLLM`: Stores string key reference (e.g., "AzureOpenAI-GPT4")
- `LLM`: Stores the **resolved full LLMConfig object** (including API keys)

Even when using system configurations, the full resolved config is duplicated into every agent's state.

### System Configuration Store
```csharp
public class SystemLLMConfigOptions
{
    public Dictionary<string, LLMConfig>? SystemLLMConfigs { get; set; }
}
```

## Proposed Solution

### 1. State Schema Evolution

Replace resolved configuration storage with reference-only approach:

```csharp
[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    // REMOVE: [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(0)] public string? LLMConfigKey { get; set; }    // Replace with reference key
    [Id(1)] public string? SystemLLM { get; set; }       // Keep as-is for compatibility
    // ... rest unchanged
}
```

### 2. Dynamic Configuration Resolution

Replace static config storage with runtime resolution:

```csharp
private LLMConfig? GetLLMConfig()
{
    // Priority 1: LLMConfigKey (new format)
    if (!State.LLMConfigKey.IsNullOrEmpty())
    {
        return ResolveSystemConfig(State.LLMConfigKey);
    }
    
    // Priority 2: SystemLLM (existing format)
    if (!State.SystemLLM.IsNullOrEmpty())
    {
        return ResolveSystemConfig(State.SystemLLM);
    }
    
    // Priority 3: Fallback to old resolved config (backwards compatibility)
    return State.LLM;
}

private LLMConfig? ResolveSystemConfig(string key)
{
    var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
    systemConfigs.Value.SystemLLMConfigs?.TryGetValue(key, out var config);
    return config;
}
```

### 3. Automatic Migration Strategy

Implement seamless migration during grain activation:

```csharp
public override async Task OnActivateAsync()
{
    // Check if old state format (has resolved LLM config)
    if (State.LLM != null && State.LLMConfigKey == null)
    {
        // Find matching system config by comparing LLMConfig objects
        var matchingKey = FindMatchingSystemLLMKey(State.LLM);
        if (matchingKey != null)
        {
            State.LLMConfigKey = matchingKey;
            State.LLM = null;  // Remove resolved config
            await RaiseEvent(new LLMConfigMigratedEvent { NewKey = matchingKey });
            await ConfirmEvents();
        }
    }
    
    await base.OnActivateAsync();
}

private string? FindMatchingSystemLLMKey(LLMConfig agentConfig)
{
    var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
    return systemConfigs.Value.SystemLLMConfigs?
        .FirstOrDefault(kvp => kvp.Value.Equal(agentConfig))
        .Key;
}
```

### 4. Updated Initialization Logic

Modify configuration storage to use references:

```csharp
private void UpdateLLMConfiguration(LLMConfigDto llmConfigDto)
{
    if (!llmConfigDto.SystemLLM.IsNullOrWhiteSpace())
    {
        // System LLM - store reference only
        State.SystemLLM = llmConfigDto.SystemLLM;
        State.LLMConfigKey = null;
        State.LLM = null;  // Don't store resolved config
    }
    else if (llmConfigDto.SelfLLMConfig != null)
    {
        // Self-provided LLM - still store resolved config for user-provided configs
        State.LLM = llmConfigDto.SelfLLMConfig.ConvertToLLMConfig();
        State.SystemLLM = null;
        State.LLMConfigKey = null;
    }
}
```

## Implementation Details

### Migration Event
```csharp
[GenerateSerializer]
public class LLMConfigMigratedEvent : StateLogEventBase<TStateLogEvent>
{
    [Id(0)] public required string NewKey { get; set; }
    [Id(1)] public DateTime MigrationTime { get; set; } = DateTime.UtcNow;
}
```

### State Transition Handler
```csharp
protected override void GAgentTransitionState(object @event)
{
    switch (@event)
    {
        case LLMConfigMigratedEvent migrationEvent:
            ApplyLLMConfigMigration(migrationEvent);
            break;
        default:
            base.GAgentTransitionState(@event);
            break;
    }
}

private void ApplyLLMConfigMigration(LLMConfigMigratedEvent @event)
{
    State.LLMConfigKey = @event.NewKey;
    State.LLM = null;  // Clear old resolved config
    _logger.LogInformation("LLM configuration migrated to key: {Key}", @event.NewKey);
}
```

## Migration Strategy

### Phase 1: Deploy with Backwards Compatibility
- Deploy new code with migration logic
- Existing agents continue working with old state format
- No immediate changes to agent behavior

### Phase 2: Automatic Migration
- Agents migrate automatically when activated
- Migration happens gradually as agents are accessed
- Zero downtime migration process

### Phase 3: Steady State
- All active agents use reference-based configuration
- System configurations can be updated centrally
- Memory usage reduced significantly

### Phase 4: Cleanup (Future)
- Optional: Remove old `LLM` field in future major version
- Maintain backwards compatibility for defined period

## Benefits

### 1. Production Operational Benefits
- **Centralized Configuration**: Update LLM settings in one place
- **Zero Downtime Updates**: Configuration changes don't require agent restarts
- **Security**: API keys stored centrally, not duplicated
- **Consistency**: All agents using same system config are guaranteed identical

### 2. Performance Benefits
- **Memory Efficiency**: No configuration duplication in agent states
- **Faster Activation**: Less data to deserialize from state storage
- **Reduced Storage**: Smaller state objects in persistent storage

### 3. Maintenance Benefits
- **Simplified Management**: Single source of truth for system configurations
- **Audit Trail**: Configuration changes tracked centrally
- **Testing**: Easier to test configuration changes in isolation

## Backwards Compatibility Guarantees

1. **Existing Agents**: Continue working without modification
2. **API Compatibility**: No changes to InitializeDto or public interfaces
3. **State Format**: Old state format supported indefinitely
4. **Migration Safety**: Automatic migration with fallback to old format
5. **Zero Breaking Changes**: Gradual migration without service interruption

## Risk Analysis

### Low Risk
- **Automatic Migration**: Happens gradually, can be monitored
- **Fallback Mechanism**: Old format still works if migration fails
- **Incremental Deployment**: Can be rolled out progressively

### Mitigation Strategies
- **Monitoring**: Track migration success/failure rates
- **Rollback Plan**: Can revert to old behavior if needed
- **Testing**: Comprehensive testing of migration logic
- **Gradual Rollout**: Deploy to subset of agents first

## Configuration Resolution Priority

1. **LLMConfigKey** (new reference format)
2. **SystemLLM** (existing reference format)  
3. **LLM** (old resolved format - backwards compatibility)

This ensures maximum compatibility while providing the benefits of centralized configuration management.

## Conclusion

This proposal provides a clean migration path from the current state-based configuration storage to a centralized, reference-based approach. The solution maintains full backwards compatibility while solving the operational challenges of managing LLM configurations in production environments.

The automatic migration ensures zero downtime and gradual adoption, while the centralized configuration management provides the operational benefits needed for production deployments.