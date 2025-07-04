# TODO-012: Create Elasticsearch Mapping for AgentInstanceState

## Task Overview
Create the Elasticsearch index mapping for `AgentInstanceState` to ensure optimal performance for agent discovery queries and proper field type handling for the AgentDiscoveryService.

## Description
Design and implement the Elasticsearch mapping configuration that supports efficient querying of agent instances by type, user, status, capabilities, and other criteria. This mapping is critical for the performance of the AgentDiscoveryService and must be optimized for the query patterns used in agent discovery.

## Acceptance Criteria
- [ ] Create Elasticsearch index mapping for AgentInstanceState
- [ ] Optimize field types for discovery query patterns
- [ ] Add proper analyzers for text fields
- [ ] Configure dynamic mapping for Properties field
- [ ] Create index templates for automatic mapping application
- [ ] Add index lifecycle management policies
- [ ] Create mapping migration scripts
- [ ] Add comprehensive integration tests
- [ ] Document mapping decisions and performance characteristics
- [ ] Verify compatibility with existing state projection pipeline

## File Locations
- `station/src/Aevatar.Infrastructure/Elasticsearch/Mappings/agent-instance-mapping.json`
- `station/src/Aevatar.Infrastructure/Elasticsearch/IndexManagement/AgentInstanceIndexManager.cs`
- `station/src/Aevatar.Infrastructure/Elasticsearch/Templates/agent-instance-template.json`
- `station/docs/elasticsearch-mapping-guide.md`

## Elasticsearch Mapping Design

### Core Mapping Structure
```json
{
  "mappings": {
    "properties": {
      "id": {
        "type": "keyword",
        "doc_values": true
      },
      "userId": {
        "type": "keyword",
        "doc_values": true
      },
      "agentType": {
        "type": "keyword",
        "doc_values": true
      },
      "name": {
        "type": "text",
        "fields": {
          "keyword": {
            "type": "keyword",
            "ignore_above": 256
          },
          "search": {
            "type": "text",
            "analyzer": "agent_name_analyzer"
          }
        }
      },
      "status": {
        "type": "keyword",
        "doc_values": true
      },
      "createTime": {
        "type": "date",
        "format": "strict_date_optional_time||epoch_millis"
      },
      "lastActivity": {
        "type": "date",
        "format": "strict_date_optional_time||epoch_millis"
      },
      "agentGrainId": {
        "type": "keyword",
        "index": false,
        "doc_values": false
      },
      "properties": {
        "type": "object",
        "dynamic": true,
        "properties": {
          "string_props": {
            "type": "object",
            "dynamic_templates": [
              {
                "strings_as_keywords": {
                  "match_mapping_type": "string",
                  "mapping": {
                    "type": "keyword",
                    "ignore_above": 256
                  }
                }
              }
            ]
          },
          "numeric_props": {
            "type": "object",
            "dynamic_templates": [
              {
                "integers": {
                  "match_mapping_type": "long",
                  "mapping": {
                    "type": "long"
                  }
                }
              },
              {
                "doubles": {
                  "match_mapping_type": "double",
                  "mapping": {
                    "type": "double"
                  }
                }
              }
            ]
          }
        }
      }
    }
  },
  "settings": {
    "number_of_shards": 3,
    "number_of_replicas": 1,
    "refresh_interval": "1s",
    "analysis": {
      "analyzer": {
        "agent_name_analyzer": {
          "type": "custom",
          "tokenizer": "standard",
          "filter": [
            "lowercase",
            "asciifolding",
            "trim"
          ]
        }
      }
    }
  }
}
```

### Index Template Configuration
```json
{
  "index_patterns": ["agent-instances-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "1s",
      "index.lifecycle.name": "agent-instances-policy",
      "index.lifecycle.rollover_alias": "agent-instances"
    },
    "mappings": {
      "_source": {
        "enabled": true
      },
      "properties": {
        // ... same as above mapping
      }
    }
  },
  "priority": 200,
  "version": 1
}
```

## Key Design Decisions

### Field Type Optimization
- **keyword** for exact match fields (id, userId, agentType, status)
- **date** for time-based queries with proper format
- **text + keyword** multi-field for name (search + exact match)
- **object** with dynamic mapping for flexible properties

### Query Pattern Optimization
- doc_values enabled for aggregation and sorting fields
- keyword fields for term queries and filtering
- date fields optimized for range queries
- dynamic templates for unknown property types

### Performance Considerations
- Disable indexing for fields that are only stored (agentGrainId)
- Use appropriate analyzers for text search
- Configure shard count based on expected data volume
- Optimize refresh interval for near real-time requirements

## Implementation Details

### Index Manager Class
```csharp
public class AgentInstanceIndexManager
{
    private readonly IElasticsearchClient _client;
    private readonly ILogger<AgentInstanceIndexManager> _logger;
    private const string IndexAlias = "agent-instances";
    private const string IndexPattern = "agent-instances-{0:yyyy.MM}";
    
    public async Task CreateIndexAsync()
    {
        var indexName = string.Format(IndexPattern, DateTime.UtcNow);
        
        var createIndexRequest = new CreateIndexRequest(indexName)
        {
            Mappings = GetAgentInstanceMapping(),
            Settings = GetIndexSettings()
        };
        
        var response = await _client.Indices.CreateAsync(createIndexRequest);
        
        if (!response.IsValid)
        {
            _logger.LogError("Failed to create index {IndexName}: {Error}", 
                indexName, response.DebugInformation);
            throw new ElasticsearchException($"Failed to create index: {response.DebugInformation}");
        }
        
        // Add to alias
        await AddToAliasAsync(indexName);
    }
    
    private TypeMapping GetAgentInstanceMapping()
    {
        return new TypeMapping
        {
            Properties = new Properties
            {
                ["id"] = new KeywordProperty { DocValues = true },
                ["userId"] = new KeywordProperty { DocValues = true },
                ["agentType"] = new KeywordProperty { DocValues = true },
                ["name"] = new TextProperty
                {
                    Fields = new Properties
                    {
                        ["keyword"] = new KeywordProperty { IgnoreAbove = 256 },
                        ["search"] = new TextProperty { Analyzer = "agent_name_analyzer" }
                    }
                },
                ["status"] = new KeywordProperty { DocValues = true },
                ["createTime"] = new DateProperty 
                { 
                    Format = "strict_date_optional_time||epoch_millis" 
                },
                ["lastActivity"] = new DateProperty 
                { 
                    Format = "strict_date_optional_time||epoch_millis" 
                },
                ["agentGrainId"] = new KeywordProperty 
                { 
                    Index = false, 
                    DocValues = false 
                },
                ["properties"] = new ObjectProperty 
                { 
                    Dynamic = true,
                    Properties = GetPropertiesMapping()
                }
            }
        };
    }
}
```

### Migration Strategy
```csharp
public class ElasticsearchMigrationService
{
    public async Task MigrateToNewMappingAsync()
    {
        // 1. Create new index with updated mapping
        var newIndexName = $"agent-instances-v2-{DateTime.UtcNow:yyyy.MM.dd}";
        await _indexManager.CreateIndexAsync(newIndexName, GetUpdatedMapping());
        
        // 2. Reindex data from old index to new index
        var reindexRequest = new ReindexRequest
        {
            Source = new ReindexSource { Index = "agent-instances-v1" },
            Destination = new ReindexDestination { Index = newIndexName }
        };
        
        await _client.ReindexAsync(reindexRequest);
        
        // 3. Update alias to point to new index
        await UpdateAliasAsync("agent-instances", newIndexName);
        
        // 4. Verify data integrity
        await VerifyMigrationAsync(newIndexName);
        
        // 5. Clean up old index (after verification period)
        // await DeleteOldIndexAsync("agent-instances-v1");
    }
}
```

## Testing Requirements
- Unit tests for mapping creation and validation
- Integration tests with real Elasticsearch instance
- Performance tests for common query patterns
- Migration tests for data integrity
- Index lifecycle tests
- Field mapping validation tests
- Query performance benchmarks

## Query Performance Optimization

### Common Query Patterns
```json
{
  "query": {
    "bool": {
      "must": [
        { "term": { "userId": "user123" } },
        { "terms": { "agentType": ["BusinessAgent", "TaskAgent"] } }
      ],
      "filter": [
        { "term": { "status": "Active" } },
        { "range": { "lastActivity": { "gte": "2023-01-01" } } }
      ]
    }
  },
  "sort": [
    { "lastActivity": { "order": "desc" } }
  ]
}
```

### Index Optimization
- Use filter context for exact matches to enable caching
- Sort by doc_values enabled fields
- Use terms queries for multiple value matching
- Implement query result caching where appropriate

## Monitoring and Maintenance

### Index Health Monitoring
```csharp
public class IndexHealthService
{
    public async Task<IndexHealth> GetIndexHealthAsync()
    {
        var healthResponse = await _client.Cluster.HealthAsync(h => h
            .Index("agent-instances")
            .WaitForStatus(WaitForStatus.Yellow)
            .Timeout("30s"));
        
        return new IndexHealth
        {
            Status = healthResponse.Status,
            NumberOfNodes = healthResponse.NumberOfNodes,
            ActiveShards = healthResponse.ActiveShards,
            RelocatingShards = healthResponse.RelocatingShards
        };
    }
}
```

### Performance Metrics
- Query execution time by query type
- Index size and growth rate
- Shard distribution and balance
- Cache hit rates for common queries
- Indexing throughput and latency

## Security Considerations
- Configure appropriate access controls for index
- Encrypt data at rest if required
- Implement field-level security if needed
- Audit index access and modifications
- Secure inter-node communication

## Dependencies
- `AgentInstanceState` class (TODO-003)
- Elasticsearch client configuration
- Existing state projection pipeline
- Index lifecycle management policies

## Success Metrics
- Sub-100ms query response time for simple searches
- Support for 100,000+ agent instances
- 99.9% query success rate
- Successful integration with AgentDiscoveryService
- Zero data loss during mapping migrations

## Documentation Requirements
- Mapping design decisions and rationale
- Query pattern optimization guide
- Migration procedures and rollback plans
- Performance tuning recommendations
- Troubleshooting guide for common issues

## Priority: Medium
This should be implemented after AgentInstanceState (TODO-003) is complete and before AgentDiscoveryService (TODO-008) implementation.