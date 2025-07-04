# TODO-015: Create Migration Guide from CreatorGAgent

## Task Overview
Create a comprehensive migration guide that documents the process of transitioning from the current CreatorGAgent architecture to the new service-based architecture, including step-by-step procedures, rollback plans, and validation steps.

## Description
Develop detailed documentation and migration scripts to guide the transition from CreatorGAgent to the new architecture. This includes data migration procedures, service deployment strategies, validation steps, and rollback procedures to ensure a safe and successful migration.

## Acceptance Criteria
- [ ] Create step-by-step migration documentation
- [ ] Develop data migration scripts for agent state
- [ ] Create feature flag implementation for gradual rollout
- [ ] Document rollback procedures and criteria
- [ ] Create validation scripts to verify migration success
- [ ] Develop monitoring and alerting for migration process
- [ ] Create training materials for development team
- [ ] Document API compatibility and breaking changes
- [ ] Create troubleshooting guide for common issues
- [ ] Validate migration process in staging environment

## File Locations
- `station/docs/migration/CreatorGAgent-Migration-Guide.md`
- `station/docs/migration/Data-Migration-Scripts.md`
- `station/scripts/migration/migrate-creator-agent-data.sql`
- `station/scripts/migration/validate-migration.ps1`
- `station/src/Aevatar.Application/Migration/CreatorGAgentMigrationService.cs`
- `station/src/Aevatar.Application/Migration/FeatureFlags.cs`

## Migration Strategy Overview

### Phase 1: Preparation and Validation
1. **Assessment**: Analyze current CreatorGAgent usage
2. **Testing**: Validate new services in isolated environment
3. **Documentation**: Update all documentation and runbooks
4. **Training**: Train development team on new architecture

### Phase 2: Parallel Deployment
1. **Infrastructure**: Deploy new services alongside existing ones
2. **Feature Flags**: Implement toggles for gradual migration
3. **Monitoring**: Set up enhanced monitoring and alerting
4. **Validation**: Verify new services work correctly

### Phase 3: Gradual Migration
1. **Pilot**: Migrate specific agent types or user segments
2. **Monitor**: Watch for performance and error metrics
3. **Expand**: Gradually increase migration scope
4. **Validate**: Continuous validation of migrated data

### Phase 4: Complete Transition
1. **Final Migration**: Move remaining agents to new architecture
2. **Cleanup**: Remove CreatorGAgent code and infrastructure
3. **Optimization**: Tune performance and configurations
4. **Documentation**: Update final documentation

## Migration Guide Documentation

### CreatorGAgent-Migration-Guide.md
```markdown
# CreatorGAgent to New Architecture Migration Guide

## Overview
This guide provides step-by-step instructions for migrating from the CreatorGAgent architecture to the new service-based architecture that eliminates the proxy layer while maintaining all functionality.

## Pre-Migration Checklist

### Infrastructure Requirements
- [ ] Elasticsearch cluster configured and accessible
- [ ] Orleans cluster upgraded to support new grains
- [ ] New service dependencies installed
- [ ] Feature flag system implemented
- [ ] Monitoring and alerting configured

### Data Backup
- [ ] Backup CreatorGAgent state data
- [ ] Backup Orleans grain storage
- [ ] Backup Elasticsearch indices
- [ ] Create restore procedures

### Testing Validation
- [ ] All new services pass unit tests
- [ ] Integration tests pass with test data
- [ ] Performance benchmarks established
- [ ] Load testing completed

## Migration Steps

### Step 1: Deploy New Services (Parallel)
```bash
# Deploy new services without activating them
kubectl apply -f deployment/new-architecture/
kubectl apply -f deployment/elasticsearch/
kubectl apply -f deployment/feature-flags/

# Verify deployments
kubectl get pods -l app=agent-services
kubectl logs -l app=type-metadata-service
```

### Step 2: Enable Feature Flags
```csharp
// Enable new architecture for testing
await featureFlagService.EnableFlagAsync("NewAgentArchitecture.TypeMetadata", true);
await featureFlagService.EnableFlagAsync("NewAgentArchitecture.Discovery", true);
```

### Step 3: Migrate Agent Type Metadata
```bash
# Run type metadata extraction
dotnet run --project Aevatar.Migration -- extract-types
# Expected output: Found 15 agent types, extracted 127 capabilities
```

### Step 4: Migrate Agent Instance Data
```bash
# Migrate CreatorGAgentState to AgentInstanceState
dotnet run --project Aevatar.Migration -- migrate-instances --batch-size 1000
# Monitor progress and validate data integrity
```

### Step 5: Enable New Architecture for Pilot Users
```csharp
// Enable for specific tenant/users
await featureFlagService.EnableFlagForUsersAsync("NewAgentArchitecture.Full", 
    new[] { "pilot-user-1", "pilot-user-2" });
```

### Step 6: Gradual Rollout
```bash
# Increase rollout percentage gradually
./scripts/migration/gradual-rollout.sh --percentage 10
./scripts/migration/gradual-rollout.sh --percentage 25
./scripts/migration/gradual-rollout.sh --percentage 50
./scripts/migration/gradual-rollout.sh --percentage 100
```

### Step 7: Cleanup Old Architecture
```bash
# Remove CreatorGAgent resources
kubectl delete deployment creator-gagent-service
kubectl delete configmap creator-gagent-config
# Clean up old grain storage
dotnet run --project Aevatar.Migration -- cleanup-old-grains
```
```

### Data Migration Implementation

```csharp
public class CreatorGAgentMigrationService
{
    private readonly IElasticsearchClient _elasticsearchClient;
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<CreatorGAgentMigrationService> _logger;
    
    public async Task<MigrationResult> MigrateAgentDataAsync(MigrationOptions options)
    {
        var result = new MigrationResult();
        var batchSize = options.BatchSize;
        var totalMigrated = 0;
        
        try
        {
            // 1. Get all CreatorGAgent IDs
            var agentIds = await GetAllCreatorGAgentIdsAsync();
            _logger.LogInformation("Found {Count} CreatorGAgents to migrate", agentIds.Count);
            
            // 2. Process in batches
            for (int i = 0; i < agentIds.Count; i += batchSize)
            {
                var batch = agentIds.Skip(i).Take(batchSize).ToList();
                var batchResult = await MigrateBatchAsync(batch);
                
                result.SuccessCount += batchResult.SuccessCount;
                result.FailureCount += batchResult.FailureCount;
                result.Errors.AddRange(batchResult.Errors);
                
                totalMigrated += batchResult.SuccessCount;
                
                _logger.LogInformation("Migrated batch {BatchNumber}: {Success}/{Total} successful", 
                    (i / batchSize) + 1, batchResult.SuccessCount, batch.Count);
                
                // Pause between batches to avoid overwhelming the system
                await Task.Delay(options.BatchDelayMs);
            }
            
            // 3. Validate migration
            await ValidateMigrationAsync(result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed after migrating {Count} agents", totalMigrated);
            result.Errors.Add($"Migration failed: {ex.Message}");
            return result;
        }
    }
    
    private async Task<BatchMigrationResult> MigrateBatchAsync(List<Guid> agentIds)
    {
        var result = new BatchMigrationResult();
        var agentStates = new List<AgentInstanceState>();
        
        // 1. Read CreatorGAgent states
        foreach (var agentId in agentIds)
        {
            try
            {
                var creatorGAgent = _grainFactory.GetGrain<ICreatorGAgent>(agentId);
                var state = await creatorGAgent.GetAgentAsync();
                
                if (state == null)
                {
                    result.Errors.Add($"Agent {agentId} returned null state");
                    result.FailureCount++;
                    continue;
                }
                
                // 2. Convert to new format
                var agentState = ConvertToAgentInstanceState(state);
                agentStates.Add(agentState);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read CreatorGAgent {AgentId}", agentId);
                result.Errors.Add($"Failed to read agent {agentId}: {ex.Message}");
                result.FailureCount++;
            }
        }
        
        // 3. Bulk index to Elasticsearch
        if (agentStates.Any())
        {
            try
            {
                var bulkResponse = await IndexAgentStatesBulkAsync(agentStates);
                
                if (bulkResponse.IsValid)
                {
                    result.SuccessCount = agentStates.Count;
                }
                else
                {
                    result.FailureCount = agentStates.Count;
                    result.Errors.Add($"Bulk indexing failed: {bulkResponse.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk indexing failed for batch");
                result.FailureCount = agentStates.Count;
                result.Errors.Add($"Bulk indexing failed: {ex.Message}");
            }
        }
        
        return result;
    }
    
    private AgentInstanceState ConvertToAgentInstanceState(CreatorGAgentState oldState)
    {
        return new AgentInstanceState
        {
            Id = oldState.Id,
            UserId = oldState.UserId,
            AgentType = oldState.AgentType,
            Name = oldState.Name,
            Properties = ConvertProperties(oldState.Properties),
            AgentGrainId = oldState.BusinessAgentGrainId,
            CreateTime = oldState.CreateTime,
            Status = ConvertStatus(oldState),
            LastActivity = DateTime.UtcNow // Set current time as last activity
        };
    }
    
    private Dictionary<string, string> ConvertProperties(Dictionary<string, object> oldProperties)
    {
        if (oldProperties == null) return new Dictionary<string, string>();
        
        return oldProperties.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );
    }
    
    private AgentStatus ConvertStatus(CreatorGAgentState oldState)
    {
        // Determine status based on old state properties
        if (oldState.UserId == Guid.Empty) return AgentStatus.Deleted;
        if (string.IsNullOrEmpty(oldState.AgentType)) return AgentStatus.Error;
        return AgentStatus.Active;
    }
}
```

### Feature Flag Implementation

```csharp
public class FeatureFlags
{
    public const string NEW_AGENT_ARCHITECTURE = "NewAgentArchitecture";
    public const string NEW_TYPE_METADATA = "NewAgentArchitecture.TypeMetadata";
    public const string NEW_DISCOVERY_SERVICE = "NewAgentArchitecture.Discovery";
    public const string NEW_EVENT_PUBLISHER = "NewAgentArchitecture.EventPublisher";
    public const string NEW_LIFECYCLE_SERVICE = "NewAgentArchitecture.Lifecycle";
}

public class MigrationService
{
    private readonly IFeatureFlagService _featureFlagService;
    
    public async Task<bool> ShouldUseNewArchitectureAsync(Guid userId)
    {
        // Check global flag first
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlags.NEW_AGENT_ARCHITECTURE))
        {
            return false;
        }
        
        // Check user-specific rollout
        return await _featureFlagService.IsEnabledForUserAsync(
            FeatureFlags.NEW_AGENT_ARCHITECTURE, userId);
    }
    
    public async Task<bool> ShouldUseNewServiceAsync(string serviceName, Guid userId)
    {
        var flagName = $"{FeatureFlags.NEW_AGENT_ARCHITECTURE}.{serviceName}";
        
        return await _featureFlagService.IsEnabledAsync(flagName) &&
               await _featureFlagService.IsEnabledForUserAsync(flagName, userId);
    }
}
```

### Validation Scripts

```csharp
public class MigrationValidator
{
    public async Task<ValidationResult> ValidateMigrationAsync()
    {
        var result = new ValidationResult();
        
        // 1. Count comparison
        var oldCount = await CountCreatorGAgentsAsync();
        var newCount = await CountAgentInstancesAsync();
        
        result.AddCheck("Agent Count", oldCount == newCount, 
            $"Old: {oldCount}, New: {newCount}");
        
        // 2. Sample data verification
        var sampleAgents = await GetSampleAgentsAsync(10);
        foreach (var agent in sampleAgents)
        {
            var migrationValid = await ValidateAgentMigrationAsync(agent.Id);
            result.AddCheck($"Agent {agent.Id}", migrationValid,
                migrationValid ? "Data matches" : "Data mismatch");
        }
        
        // 3. Capability verification
        var capabilityCheck = await ValidateCapabilitiesAsync();
        result.AddCheck("Capabilities", capabilityCheck.IsValid, capabilityCheck.Message);
        
        // 4. Performance validation
        var performanceCheck = await ValidatePerformanceAsync();
        result.AddCheck("Performance", performanceCheck.IsAcceptable, 
            $"Query time: {performanceCheck.AverageQueryTime}ms");
        
        return result;
    }
    
    private async Task<bool> ValidateAgentMigrationAsync(Guid agentId)
    {
        try
        {
            // Get data from old system
            var creatorGAgent = _grainFactory.GetGrain<ICreatorGAgent>(agentId);
            var oldState = await creatorGAgent.GetAgentAsync();
            
            // Get data from new system
            var discoveryService = _serviceProvider.GetRequiredService<IAgentDiscoveryService>();
            var newAgent = await discoveryService.FindAgentByIdAsync(agentId);
            
            if (oldState == null && newAgent == null) return true;
            if (oldState == null || newAgent == null) return false;
            
            // Compare key fields
            return oldState.Id == newAgent.Id &&
                   oldState.UserId == newAgent.UserId &&
                   oldState.AgentType == newAgent.AgentType &&
                   oldState.Name == newAgent.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate agent {AgentId}", agentId);
            return false;
        }
    }
}
```

### Rollback Procedures

```markdown
# Rollback Procedures

## When to Rollback
- Migration failure rate > 5%
- Performance degradation > 20%
- Critical functionality broken
- Data corruption detected

## Rollback Steps

### 1. Immediate Rollback (Emergency)
```bash
# Disable new architecture immediately
kubectl patch configmap feature-flags --patch '{"data":{"NewAgentArchitecture":"false"}}'
kubectl rollout restart deployment/agent-service

# Restore service traffic to old architecture
kubectl patch service agent-service --patch '{"spec":{"selector":{"version":"old"}}}'
```

### 2. Data Rollback
```bash
# Restore from backup if needed
kubectl exec -it elasticsearch-0 -- elasticsearch-snapshot restore backup-pre-migration

# Restore Orleans grain storage
./scripts/restore-grain-storage.sh --backup-date 2023-12-01
```

### 3. Verification
```bash
# Verify old system is working
./scripts/health-check.sh --system old
./scripts/validate-functionality.sh
```
```

## Monitoring and Alerting

### Migration Metrics
- Migration progress percentage
- Error rate during migration
- Data consistency validation results
- Performance comparison (old vs new)
- Feature flag activation rates

### Alert Conditions
- Migration failure rate > 5%
- Data inconsistency detected
- Performance degradation > 20%
- Error spike in new services
- Failed validation checks

## Training Materials

### Developer Training Topics
1. New architecture overview and benefits
2. Service interaction patterns
3. Debugging and troubleshooting
4. Performance monitoring
5. Common migration issues

### Operations Training Topics
1. Deployment procedures
2. Monitoring and alerting setup
3. Rollback procedures
4. Performance tuning
5. Incident response

## Dependencies
- All new architecture services (TODO-002 through TODO-013)
- Feature flag system
- Monitoring and alerting infrastructure
- Backup and restore procedures
- Test environments for validation

## Success Metrics
- Zero data loss during migration
- Migration completed within planned timeframe
- Performance maintained or improved
- Zero critical incidents during migration
- Team successfully trained on new architecture

## Risk Mitigation
- Comprehensive testing in staging
- Gradual rollout with feature flags
- Automated rollback triggers
- 24/7 monitoring during migration
- Clear escalation procedures

## Priority: Low
This should be the final task, completed after all services are implemented and thoroughly tested.