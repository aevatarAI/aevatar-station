# Elasticsearch Client Upgrade TODO List

This document outlines all non-production code implementations and TODOs that need to be addressed when upgrading the Elasticsearch client to a stable version with full API compatibility.

## Critical Production Issues

### 1. Schema Validation (Line 153-160)
**Status**: Non-Production Implementation  
**Priority**: High  
**Issue**: Property validation is currently simplified due to API compatibility issues.

```csharp
// Current simplified implementation
foreach (var expectedProp in expectedProperties)
{
    // For now, we'll simplify the property checking due to API compatibility issues
    // In a production environment, you would implement proper property validation here
    _logger.LogDebug("Checking property {PropertyName} in index {IndexName}", 
        expectedProp.Key, indexName);
    
    // TODO: Implement proper property validation when Elasticsearch client API is stabilized
    // The Properties type doesn't have standard dictionary methods in the current client version
}
```

**Required Action**: 
- Implement proper `actualMapping.TryGetValue()` or `actualMapping.ContainsKey()` functionality
- Add type checking for property compatibility
- Validate field mappings (text, keyword, date, etc.)

### 2. Data Reindexing (Line 336-370)
**Status**: Non-Production Implementation  
**Priority**: Critical  
**Issue**: Reindex operations are completely simulated.

```csharp
// Current non-production implementation
private async Task<bool> ReindexDataAsync(string sourceIndex, string destinationIndex)
{
    // For now, we'll simplify the reindex operation due to API compatibility issues
    // In a production environment, you would implement proper reindexing here
    _logger.LogInformation("Reindex operation simulated from {SourceIndex} to {DestinationIndex}", 
        sourceIndex, destinationIndex);
    
    // TODO: Implement proper reindexing when Elasticsearch client API is stabilized
    // var reindexResponse = await _client.ReindexAsync(...);
    
    // For now, assume successful operation
    var created = 0;
    var updated = 0;
}
```

**Required Action**:
- Implement actual `_client.ReindexAsync()` with proper source/destination configuration
- Add script-based data transformation during reindexing
- Handle reindex conflicts and errors
- Implement progress monitoring for large datasets
- Add timeout and batch size configuration

### 3. Index Alias Management (Line 371-389)
**Status**: Non-Production Implementation  
**Priority**: High  
**Issue**: Alias operations are only logged, not executed.

```csharp
// Current non-production implementation
private async Task UpdateIndexAliasAsync(string aliasName, string newIndex, string? oldIndex = null)
{
    // For now, we'll log the alias operation but not implement it due to API compatibility issues
    // In a production environment, you would implement proper alias management here
    _logger.LogInformation("Alias operation logged: {AliasName} -> {NewIndex} (replacing {OldIndex})", 
        aliasName, newIndex, oldIndex ?? "none");
        
    // TODO: Implement proper alias management when Elasticsearch client API is stabilized
}
```

**Required Action**:
- Implement `_client.Indices.UpdateAliasesAsync()` with proper alias actions
- Add atomic alias switching (remove old, add new in single operation)
- Handle alias conflicts and validation
- Implement rollback capability for failed alias updates

### 4. Index Version Detection (Line 393-422)
**Status**: Partial Implementation  
**Priority**: Medium  
**Issue**: Limited version detection with hardcoded maximum version check.

```csharp
// Current limited implementation
// Check for versioned indices by pattern
for (int version = 1; version <= 10; version++) // Check up to version 10
{
    var versionedIndexName = GetVersionedIndexName(baseIndexName, version);
    var versionedExists = await _client.Indices.ExistsAsync(versionedIndexName);
    if (versionedExists.Exists)
    {
        return version;
    }
}
```

**Required Action**:
- Implement proper alias-based version detection instead of pattern matching
- Remove hardcoded version limit (currently 10)
- Use `_client.Indices.GetAliasAsync()` to find current active version
- Add metadata-based version tracking in index settings

## Enhancement TODOs

### 5. Index Cleanup (Line 226)
**Status**: TODO Comment  
**Priority**: Medium  
**Issue**: Old index cleanup is commented out.

```csharp
// Step 5: Clean up old index after successful migration (optional)
// await CleanupOldIndexAsync(oldVersionedIndexName);
```

**Required Action**:
- Implement `CleanupOldIndexAsync()` method
- Add configurable retention policy for old indices
- Implement safe cleanup with confirmation checks
- Add option to keep old indices for rollback purposes

### 6. Backwards Compatibility Warning (Line 506)
**Status**: Warning Implementation  
**Priority**: Low  
**Issue**: Direct index creation bypasses versioning system.

```csharp
// This method is kept for backwards compatibility but now uses the versioned approach
_logger.LogWarning("CreateIndexAsync called directly - consider using versioned approach");
```

**Required Action**:
- Consider deprecating direct `CreateIndexAsync()` method
- Update all callers to use versioned approach
- Add migration guide for existing code

## API Compatibility Issues

### 7. Properties Type Limitations
**Affected Methods**: `ValidateSchemaCompatibilityAsync()`  
**Issue**: Elasticsearch Properties type doesn't implement standard dictionary interfaces  
**Workaround**: Using simplified validation  
**Required**: Wait for client library update or implement custom property inspection

### 8. Index Method Parameter Issues
**Affected Methods**: `ReindexDataAsync()`  
**Issue**: `Index()` method signature incompatibility  
**Workaround**: Using string parameters directly  
**Required**: Update to proper IndexName construction when API stabilizes

### 9. Alias Action Type Issues
**Affected Methods**: `UpdateIndexAliasAsync()`  
**Issue**: Alias action types (AddAction, RemoveAction) not compatible  
**Workaround**: Operations are logged only  
**Required**: Implement proper alias actions when API is fixed

## Performance and Scalability TODOs

### 10. Schema Version Calculation
**Current Implementation**: Simple hash-based versioning  
**Enhancement Needed**: 
- More sophisticated schema versioning
- Semantic versioning support
- Breaking vs non-breaking change detection

### 11. Bulk Operations Optimization
**Current Implementation**: Basic bulk operations  
**Enhancement Needed**:
- Configurable batch sizes
- Parallel processing for large datasets
- Better error handling and retry logic

### 12. Caching Strategy
**Current Implementation**: Basic memory caching for index checks  
**Enhancement Needed**:
- Distributed caching for multi-instance scenarios
- Cache invalidation strategies
- Performance monitoring and metrics

## Testing Requirements

### 13. Integration Tests
**Status**: Missing  
**Required**:
- Test schema migration scenarios
- Test index versioning workflows
- Test alias switching operations
- Test error handling and rollback scenarios

### 14. Performance Tests
**Status**: Missing  
**Required**:
- Large dataset migration testing
- Concurrent operation testing
- Memory usage monitoring during migrations

## Documentation TODOs

### 15. Migration Guide
**Status**: Missing  
**Required**:
- Step-by-step upgrade guide
- Breaking changes documentation
- Configuration examples

### 16. Operational Runbook
**Status**: Missing  
**Required**:
- Monitoring and alerting setup
- Troubleshooting guide
- Recovery procedures

## Configuration Enhancements

### 17. Migration Settings
**Status**: Hardcoded values  
**Required**:
- Configurable migration timeouts
- Batch size configuration
- Retry policies
- Rollback thresholds

### 18. Index Settings
**Status**: Basic configuration  
**Required**:
- Environment-specific shard/replica settings
- Refresh interval optimization
- Memory allocation tuning

---

## Priority Matrix

| Priority | Category | Count | Impact |
|----------|----------|-------|---------|
| Critical | Data Migration | 1 | Data loss risk |
| High | Schema Validation, Alias Management | 2 | Feature incomplete |
| Medium | Version Detection, Cleanup | 2 | Operational efficiency |
| Low | Warnings, Documentation | 11+ | Code quality |

## Estimated Implementation Timeline

- **Phase 1 (Critical)**: 2-3 weeks - Data reindexing implementation
- **Phase 2 (High)**: 1-2 weeks - Schema validation and alias management  
- **Phase 3 (Medium)**: 1 week - Version detection and cleanup
- **Phase 4 (Enhancement)**: 2-3 weeks - Performance optimizations and testing

**Total Estimated Effort**: 6-9 weeks for full production readiness 