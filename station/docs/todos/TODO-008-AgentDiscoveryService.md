# TODO-008: Create AgentDiscoveryService for Elasticsearch-based Discovery

## Task Overview
Create the `AgentDiscoveryService` that combines type-based capability filtering with Elasticsearch instance queries, implementing the two-tier discovery architecture from the AgentRegistry-ElasticSearch-Lite design.

## Description
Implement the service that provides efficient agent discovery by first filtering agent types by capabilities (fast, in-memory) and then querying Elasticsearch for instances of eligible types (scalable). This eliminates the data duplication issues and provides superior performance compared to the current approach.

## Acceptance Criteria
- [ ] Create `IAgentDiscoveryService` interface
- [ ] Implement `AgentDiscoveryService` class
- [ ] Create `AgentDiscoveryQuery` model
- [ ] Add capability-based type filtering
- [ ] Implement Elasticsearch instance queries
- [ ] Add result combination logic
- [ ] Support pagination and sorting
- [ ] Create comprehensive unit tests
- [ ] Add integration tests with Elasticsearch
- [ ] Add performance benchmarks
- [ ] Support complex query scenarios

## File Locations
- `station/src/Aevatar.Application/Services/IAgentDiscoveryService.cs`
- `station/src/Aevatar.Application/Services/AgentDiscoveryService.cs`
- `station/src/Aevatar.Application/Models/AgentDiscoveryQuery.cs`
- `station/src/Aevatar.Application/Models/AgentDiscoveryResult.cs`

## Implementation Details

### IAgentDiscoveryService Interface
```csharp
public interface IAgentDiscoveryService
{
    Task<List<AgentInfo>> FindAgentsAsync(AgentDiscoveryQuery query);
    Task<AgentDiscoveryResult> FindAgentsWithPaginationAsync(AgentDiscoveryQuery query);
    Task<List<AgentInfo>> FindAgentsByUserAsync(Guid userId, AgentStatus? status = null);
    Task<List<AgentInfo>> FindAgentsByCapabilityAsync(string capability, Guid? userId = null);
    Task<AgentInfo> FindAgentByIdAsync(Guid agentId);
    Task<bool> AgentExistsAsync(Guid agentId);
}
```

### AgentDiscoveryQuery Model
```csharp
public class AgentDiscoveryQuery
{
    public Guid? UserId { get; set; }
    public List<string> RequiredCapabilities { get; set; } = new();
    public List<string> AgentTypes { get; set; } = new();
    public AgentStatus? Status { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public Dictionary<string, object> PropertyFilters { get; set; } = new();
    
    // Pagination
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    
    // Sorting
    public string SortBy { get; set; } = "LastActivity";
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;
}
```

### Core Dependencies
- `ITypeMetadataService` for capability filtering
- `IElasticsearchClient` for instance queries
- `ILogger<AgentDiscoveryService>` for logging

### Two-Tier Discovery Algorithm

#### Step 1: Type Filtering (In-Memory)
```csharp
public async Task<List<AgentInfo>> FindAgentsAsync(AgentDiscoveryQuery query)
{
    // Step 1: Filter agent types by capabilities (fast, in-memory)
    var eligibleTypes = new List<string>();
    
    if (query.RequiredCapabilities.Any())
    {
        foreach (var capability in query.RequiredCapabilities)
        {
            var typesWithCapability = await _typeMetadataService
                .GetTypesByCapabilityAsync(capability);
            eligibleTypes.AddRange(typesWithCapability.Select(t => t.AgentType));
        }
        eligibleTypes = eligibleTypes.Distinct().ToList();
    }
    else if (query.AgentTypes.Any())
    {
        eligibleTypes = query.AgentTypes;
    }
    else
    {
        // Get all agent types if no specific filtering
        var allTypes = await _typeMetadataService.GetAllTypesAsync();
        eligibleTypes = allTypes.Select(t => t.AgentType).ToList();
    }
    
    // Step 2: Query Elasticsearch for instances of eligible types
    return await QueryElasticsearchInstances(query, eligibleTypes);
}
```

#### Step 2: Instance Querying (Elasticsearch)
```csharp
private async Task<List<AgentInfo>> QueryElasticsearchInstances(
    AgentDiscoveryQuery query, List<string> eligibleTypes)
{
    var searchRequest = new SearchRequest<AgentInstanceState>
    {
        Query = BuildElasticsearchQuery(query, eligibleTypes),
        Sort = BuildSortCriteria(query),
        From = query.Skip,
        Size = query.Take
    };
    
    var response = await _elasticsearchClient.SearchAsync<AgentInstanceState>(searchRequest);
    
    // Step 3: Combine with type metadata
    return await CombineWithTypeMetadata(response.Documents);
}
```

## Elasticsearch Query Building

### Complex Query Construction
```csharp
private QueryContainer BuildElasticsearchQuery(
    AgentDiscoveryQuery query, List<string> eligibleTypes)
{
    var queries = new List<QueryContainer>();
    
    // Agent type filtering
    if (eligibleTypes.Any())
    {
        queries.Add(Query.Terms(t => t.Field(f => f.AgentType).Terms(eligibleTypes)));
    }
    
    // User filtering
    if (query.UserId.HasValue)
    {
        queries.Add(Query.Term(t => t.Field(f => f.UserId).Value(query.UserId.Value)));
    }
    
    // Status filtering
    if (query.Status.HasValue)
    {
        queries.Add(Query.Term(t => t.Field(f => f.Status).Value(query.Status.Value)));
    }
    
    // Date range filtering
    if (query.CreatedAfter.HasValue)
    {
        queries.Add(Query.DateRange(d => d
            .Field(f => f.CreateTime)
            .GreaterThanOrEquals(query.CreatedAfter.Value)));
    }
    
    if (query.LastActivityAfter.HasValue)
    {
        queries.Add(Query.DateRange(d => d
            .Field(f => f.LastActivity)
            .GreaterThanOrEquals(query.LastActivityAfter.Value)));
    }
    
    // Property filtering
    foreach (var propertyFilter in query.PropertyFilters)
    {
        queries.Add(Query.Term(t => t
            .Field($"properties.{propertyFilter.Key}")
            .Value(propertyFilter.Value)));
    }
    
    return Query.Bool(b => b.Must(queries));
}
```

## Dependencies
- `ITypeMetadataService` (TODO-004)
- `AgentInstanceState` (TODO-003)
- Elasticsearch client
- Orleans configuration

## Testing Requirements
- Unit tests with mocked dependencies
- Capability filtering logic tests
- Elasticsearch query building tests
- Result combination and mapping tests
- Pagination and sorting tests
- Performance tests with large datasets
- Integration tests with real Elasticsearch
- Complex query scenario tests

## Performance Considerations
- In-memory type filtering for speed
- Optimized Elasticsearch queries
- Proper field mapping and indexing
- Pagination to limit result sets
- Caching for frequently accessed type metadata
- Query result caching for repeated searches
- Monitoring query execution times

## Elasticsearch Mapping Requirements
```json
{
  "mappings": {
    "properties": {
      "id": { "type": "keyword" },
      "userId": { "type": "keyword" },
      "agentType": { "type": "keyword" },
      "name": { "type": "text", "fields": { "keyword": { "type": "keyword" } } },
      "status": { "type": "keyword" },
      "createTime": { "type": "date" },
      "lastActivity": { "type": "date" },
      "properties": { "type": "object", "dynamic": true }
    }
  }
}
```

## Pagination and Sorting
```csharp
public class AgentDiscoveryResult
{
    public List<AgentInfo> Agents { get; set; } = new();
    public long TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool HasMore => Skip + Take < TotalCount;
}
```

## Error Handling Strategy
- Handle Elasticsearch connection failures
- Graceful degradation when type metadata unavailable
- Timeout handling for long-running queries
- Retry logic for transient failures
- Comprehensive error logging
- Fallback strategies for critical scenarios

## Security Considerations
- Multi-tenant data isolation
- User access validation
- Query parameter sanitization
- Prevent information leakage through queries
- Audit discovery operations
- Rate limiting for expensive queries

## Integration Points
- Replace agent discovery logic in existing services
- Work with current multi-tenancy patterns
- Integrate with API controllers
- Support existing authentication/authorization
- Compatible with current monitoring systems

## Monitoring and Observability
- Query execution time metrics
- Result count and pagination metrics
- Error rate monitoring
- Elasticsearch health monitoring
- Performance dashboard integration
- Slow query logging

## Success Metrics
- Sub-100ms response time for simple queries
- Support for 10,000+ agent instances
- 99.9% query success rate
- Accurate result combining with type metadata
- Efficient memory usage for type filtering

## Future Enhancements
- Real-time agent status updates
- Advanced search with fuzzy matching
- Geospatial agent discovery
- Machine learning-based recommendations
- Search result ranking and scoring
- Agent similarity matching

## Priority: High
This service is essential for efficient agent discovery and replaces the query capabilities currently provided through CreatorGAgent.