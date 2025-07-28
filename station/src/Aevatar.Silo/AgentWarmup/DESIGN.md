# Agent Warmup System Design Document

## Overview

The Agent Warmup System is an intelligent, autonomous solution designed to proactively load Orleans agents into memory during low-load periods to reduce activation latency and prevent MongoDB access spikes during high-load scenarios. The system features automatic agent discovery, flexible strategy assignment, and direct MongoDB integration for minimal configuration overhead.

## Problem Statement

### Primary Issues
- **Cold Start Latency**: Agents not in memory require activation time when first accessed
- **MongoDB Access Spikes**: High concurrent agent activations can overwhelm the database
- **Unpredictable Performance**: First-time agent access has significantly higher latency
- **Resource Contention**: Database connection pool exhaustion during activation bursts
- **Manual Configuration Overhead**: Requiring manual registration of each agent type for warmup

### Goals
- Automatically discover warmup-eligible agent types from assemblies
- Reduce agent activation latency by pre-loading agents during low-load periods
- Prevent MongoDB access spikes through controlled, rate-limited agent activation
- Provide flexible strategy assignment with minimal configuration
- Enable direct MongoDB integration for automatic identifier retrieval
- Maintain system stability and observability throughout the warmup process
- Support both automatic and manual configuration approaches

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Enhanced Agent Warmup System                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────┐ │
│  │ Agent Discovery │  │ Strategy        │  │ MongoDB         │  │ Warmup  │ │
│  │ Service         │  │ Orchestrator    │  │ Integration     │  │ Service │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────┐ │
│  │ Default         │  │ Agent-Specific  │  │ Custom          │  │ Legacy  │ │
│  │ Strategy        │  │ Strategies      │  │ Strategies      │  │ Support │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────┐ │
│  │ Rate Limiting   │  │ Progress        │  │ Orleans         │  │ Config  │ │
│  │ & Concurrency   │  │ Monitoring      │  │ Integration     │  │ System  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Automatic Agent Discovery

#### IAgentDiscoveryService
Automatically discovers warmup-eligible agent types from assemblies:

```csharp
public interface IAgentDiscoveryService
{
    /// <summary>
    /// Discovers all agent types eligible for warmup
    /// </summary>
    IEnumerable<Type> DiscoverWarmupEligibleAgentTypes(IEnumerable<Type>? excludedTypes = null);
    
    /// <summary>
    /// Checks if an agent type is eligible for warmup
    /// </summary>
    bool IsWarmupEligible(Type agentType);
    
    /// <summary>
    /// Gets the identifier type for an agent type
    /// </summary>
    Type GetAgentIdentifierType(Type agentType);
    
    /// <summary>
    /// Gets all discovered agent types with their identifier types
    /// </summary>
    Dictionary<Type, Type> GetAgentTypeMapping();
}
```

**Discovery Criteria**:
- **Base Type**: Must inherit from `GAgentBase` class
- **Storage Attribute**: Must have `[StorageProvider(ProviderName = "PubSubStore")]` attribute
- **Agent Interface**: Must implement Orleans grain interface (IGrainWithGuidKey, etc.)
- **Exclusion List**: Configurable list of types to exclude from warmup

**Implementation Features**:
- Assembly scanning with caching for performance
- Reflection-based attribute and inheritance checking
- Support for multiple identifier types (Guid, string, int, long)
- Configurable base types and required attributes

### 2. MongoDB Integration

#### IMongoDbAgentIdentifierService
Retrieves agent identifiers directly from MongoDB collections:

```csharp
public interface IMongoDbAgentIdentifierService
{
    /// <summary>
    /// Gets agent identifiers from MongoDB for a specific agent type
    /// </summary>
    IAsyncEnumerable<TIdentifier> GetAgentIdentifiersAsync<TIdentifier>(
        Type agentType, 
        int? maxCount = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the MongoDB collection name for an agent type
    /// </summary>
    string GetCollectionName(Type agentType);
    
    /// <summary>
    /// Checks if a collection exists for the agent type
    /// </summary>
    Task<bool> CollectionExistsAsync(Type agentType);
    
    /// <summary>
    /// Gets the count of documents in an agent collection
    /// </summary>
    Task<long> GetAgentCountAsync(Type agentType);
}
```

**Collection Naming Convention**:
- Default: `{namespace}.{agentTypeName}`
- Example: `Aevatar.GAgents.UserGAgent` → collection `"Aevatar.GAgents.UserGAgent"`
- Custom: `agents_{typename_lowercase}` (implemented in GetCustomCollectionName method)
- Configurable via Orleans storage configuration

**Features**:
- Direct MongoDB collection access
- Streaming identifier retrieval for memory efficiency
- Configurable batch sizes and limits
- Error handling for missing collections
- Support for multiple identifier types

### 3. Enhanced Strategy System

#### IAgentWarmupStrategy (Enhanced)
Decoupled strategy interface supporting multiple agent types:

```csharp
public interface IAgentWarmupStrategy<TIdentifier> : IAgentWarmupStrategy
{
    /// <summary>
    /// Strategy name for identification
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Agent types this strategy applies to (empty = applies to all)
    /// </summary>
    IEnumerable<Type> ApplicableAgentTypes { get; }
    
    /// <summary>
    /// Priority for execution order (higher = earlier)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Generates agent identifiers for a specific agent type
    /// </summary>
    IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates agent reference for a specific agent type and identifier
    /// </summary>
    IGrain CreateAgentReference(IGrainFactory agentFactory, Type agentType, TIdentifier identifier);
    
    /// <summary>
    /// Checks if strategy applies to an agent type
    /// </summary>
    bool AppliesTo(Type agentType);
}
```

#### DefaultAgentWarmupStrategy
Automatic strategy that applies to all agent types not covered by specific strategies:

```csharp
public class DefaultAgentWarmupStrategy<TIdentifier> : BaseAgentWarmupStrategy<TIdentifier>
{
    public override string Name => "DefaultStrategy";
    public override IEnumerable<Type> ApplicableAgentTypes => Enumerable.Empty<Type>(); // Applies to all
    public override int Priority => 0; // Lowest priority
    
    public override async IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType, 
        CancellationToken cancellationToken = default)
    {
        // Uses MongoDB service to retrieve identifiers automatically
        await foreach (var identifier in _mongoDbService.GetAgentIdentifiersAsync<TIdentifier>(agentType, cancellationToken))
        {
            yield return identifier;
        }
    }
}
```

#### PredefinedAgentWarmupStrategy
Strategy for warming up specific known agents with predefined identifiers:

```csharp
// Use PredefinedAgentWarmupStrategy for specific known agents
services.AddPredefinedAgentWarmupStrategy<UserGAgent, Guid>("CriticalUsers", criticalUserIds);
services.AddPredefinedAgentWarmupStrategy<SystemGAgent, Guid>("CriticalSystems", criticalSystemIds);
services.AddPredefinedAgentWarmupStrategy<OrderGAgent, Guid>("HighPriorityOrders", importantOrderIds);
```

#### SampleBasedAgentWarmupStrategy
Strategy that randomly samples a percentage of agents from MongoDB collections:

```csharp
public class SampleBasedAgentWarmupStrategy<TIdentifier> : BaseAgentWarmupStrategy<TIdentifier>
{
    public override string Name => _name;
    public override IEnumerable<Type> ApplicableAgentTypes => new[] { _agentType };
    public override int Priority => 75; // Higher priority than default strategy
    
    public override async IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get all identifiers from MongoDB
        // 2. Randomly sample specified percentage using Fisher-Yates shuffle
        // 3. Return sampled identifiers with batch delays
    }
}
```

**Features**:
- **Random Sampling**: Uses Fisher-Yates shuffle algorithm for true randomness
- **Configurable Ratio**: Sample any percentage from 0.1% to 100%
- **Deterministic Testing**: Optional random seed for reproducible results
- **Memory Efficient**: Loads all identifiers then samples in-memory
- **MongoDB Integration**: Retrieves identifiers directly from collections
- **All Identifier Types**: Supports Guid, string, int, long identifiers

**Usage Examples**:
```csharp
// Sample 10% of users randomly
services.AddSampleBasedAgentWarmupStrategy<UserGAgent, Guid>("UserSample", 0.1);

// Sample 5% of products with deterministic seed for testing
services.AddSampleBasedAgentWarmupStrategy<ProductGAgent, Guid>("ProductSample", 0.05, randomSeed: 12345);

// Sample 20% of orders with custom batch size
services.AddSampleBasedAgentWarmupStrategy<OrderGAgent, Guid>("OrderSample", 0.2, batchSize: 50);

// Sample 1% of large datasets efficiently
services.AddSampleBasedAgentWarmupStrategy<LogGAgent, string>("LogSample", 0.01);
```

**Use Cases**:
- **Load Testing**: Warm up a representative sample for performance testing
- **Gradual Rollout**: Start with small percentage, increase over time
- **Resource Management**: Limit warmup impact on large collections
- **Statistical Sampling**: Get random representative subset of agents
- **Development/Staging**: Use smaller samples in non-production environments

### 4. Strategy Orchestration

#### IAgentWarmupOrchestrator
Manages strategy execution order and agent type assignment:

```csharp
public interface IAgentWarmupOrchestrator<TIdentifier>
{
    /// <summary>
    /// Plans warmup execution based on discovered agents and registered strategies
    /// </summary>
    WarmupExecutionPlan CreateExecutionPlan(
        IEnumerable<Type> agentTypes, 
        IEnumerable<IAgentWarmupStrategy> strategies);
    
    /// <summary>
    /// Executes the warmup plan
    /// </summary>
    Task ExecuteWarmupPlanAsync(WarmupExecutionPlan plan, CancellationToken cancellationToken = default);
}

public class WarmupExecutionPlan
{
    public List<StrategyExecution> StrategyExecutions { get; set; } = new();
    public List<Type> UnassignedAgentTypes { get; set; } = new();
}

public class StrategyExecution
{
    public IAgentWarmupStrategy Strategy { get; set; }
    public List<Type> TargetAgentTypes { get; set; } = new();
    public int Priority { get; set; }
}
```

**Execution Flow**:
1. **Discovery Phase**: Find all warmup-eligible agent types
2. **Strategy Mapping**: Assign agent types to strategies based on applicability
3. **Priority Ordering**: Sort strategies by priority (specific strategies first)
4. **Execution**: Run strategies in order, then default strategy for remaining types
5. **Monitoring**: Track progress across all strategies and agent types

### 5. Enhanced Configuration System

#### AgentWarmupConfiguration (Enhanced)
```csharp
public class AgentWarmupConfiguration
{
    // Existing configuration properties...
    
    /// <summary>
    /// Automatic agent discovery configuration
    /// </summary>
    public AutoDiscoveryConfiguration AutoDiscovery { get; set; } = new();
    
    /// <summary>
    /// Default strategy configuration
    /// </summary>
    public DefaultStrategyConfiguration DefaultStrategy { get; set; } = new();
    
    /// <summary>
    /// MongoDB integration configuration
    /// </summary>
    public MongoDbIntegrationConfiguration MongoDbIntegration { get; set; } = new();
}

public class AutoDiscoveryConfiguration
{
    public bool Enabled { get; set; } = true;
    public string BaseTypeName { get; set; } = "GAgentBase";
    public List<string> RequiredAttributes { get; set; } = new() { "StorageProvider" };
    public string StorageProviderName { get; set; } = "PubSubStore";
    public List<string> ExcludedAgentTypes { get; set; } = new();
    public List<string> IncludedAssemblies { get; set; } = new();
    public bool CacheDiscoveredTypes { get; set; } = true;
}

public class DefaultStrategyConfiguration
{
    public bool Enabled { get; set; } = true;
    public string IdentifierSource { get; set; } = "MongoDB"; // MongoDB, Predefined, Range
    public int MaxIdentifiersPerType { get; set; } = 1000;
    public int Priority { get; set; } = 0;
}

public class MongoDbIntegrationConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionNamingStrategy { get; set; } = "FullTypeName"; // FullTypeName, TypeName, Custom
    public int BatchSize { get; set; } = 100;
    public int QueryTimeoutMs { get; set; } = 30000;
    public string CollectionPrefix { get; set; } = string.Empty;
}
```

## Key Design Patterns

### 1. Automatic Discovery Pattern
- **Assembly Scanning**: Reflection-based type discovery with caching
- **Attribute-Based Filtering**: Configurable criteria for agent eligibility
- **Lazy Loading**: Types discovered on first access, cached thereafter
- **Extensible Criteria**: Support for custom discovery rules

### 2. Strategy Orchestration Pattern
- **Priority-Based Execution**: Strategies execute in priority order
- **Agent Type Assignment**: Automatic assignment based on strategy applicability
- **Fallback Strategy**: Default strategy handles unassigned agent types
- **Conflict Resolution**: Higher priority strategies take precedence

### 3. MongoDB Integration Pattern
- **Direct Collection Access**: Bypass Orleans for identifier retrieval
- **Streaming Results**: Memory-efficient identifier enumeration
- **Collection Naming**: Configurable naming strategies
- **Error Resilience**: Graceful handling of missing collections

### 4. Configuration-Driven Pattern
- **Zero Configuration**: Works out-of-the-box with sensible defaults
- **Progressive Enhancement**: Add specific strategies as needed
- **Backward Compatibility**: Existing configurations continue to work
- **Environment-Specific**: Different settings for dev/staging/production

## MongoDB Integration Details

### Collection Naming Strategies

All collection names are automatically prefixed to match PubSubStore configuration. The prefix follows the pattern: `hostId.IsNullOrEmpty() ? "StreamStorage" : $"Stream{hostId}"`.

#### Default Strategy (FullTypeName)
```csharp
// Agent type: Aevatar.GAgents.UserGAgent
// Without prefix: "Aevatar.GAgents.UserGAgent"
// With prefix: "StreamStorage.Aevatar.GAgents.UserGAgent"
// With hostId "dev": "Streamdev.Aevatar.GAgents.UserGAgent"
```

#### Type Name Strategy
```csharp
// Agent type: Aevatar.GAgents.UserGAgent  
// Without prefix: "UserGAgent"
// With prefix: "StreamStorage.UserGAgent"
// With hostId "dev": "Streamdev.UserGAgent"
```

#### Custom Strategy
```csharp
// Custom collection naming is implemented directly in MongoDbAgentIdentifierService
// via GetCustomCollectionName method for simplicity
private string GetCustomCollectionName(Type agentType)
{
    // Default custom naming strategy - can be enhanced in future
    // Format: agents_{typename_lowercase}
    return $"agents_{agentType.Name.ToLowerInvariant()}";
    // Result with prefix: "StreamStorage.agents_usergagent"
}
```

#### Collection Prefix Configuration
```csharp
// Automatic prefix (matches PubSubStore configuration)
services.AddAgentWarmup(config =>
{
    // Prefix is automatically set based on Host:HostId configuration
    // Uses PubSubStore pattern: "StreamStorage" or "Stream{hostId}"
    // No manual configuration needed
});

// Manual prefix override
services.AddAgentWarmup(config =>
{
    config.MongoDbIntegration.CollectionPrefix = "CustomPrefix";
});
```

### MongoDB Document _id Format

The MongoDB collections store agent documents with a specific `_id` format that combines the agent type and identifier:

#### _id Format Pattern
```
{agenttypestring-lower-case}/{identifier}
```

#### Examples
```csharp
// TestDbGAgent with Guid identifier
_id: "testdbgagent/99f2e278ae5e4a759075b15d64b4e749"

// UserGAgent with Guid identifier  
_id: "usergagent/a1b2c3d4-e5f6-7890-abcd-ef1234567890"

// OrderGAgent with string identifier
_id: "ordergagent/order-12345"

// ProductGAgent with long identifier
_id: "productgagent/9876543210"
```

#### Format Components
- **Agent Type Part**: Lowercase agent type name (e.g., "testdbgagent", "usergagent")
- **Separator**: Forward slash "/" character
- **Identifier Part**: The actual agent identifier as string representation
  - **Guid**: Formatted as string without hyphens (e.g., "99f2e278ae5e4a759075b15d64b4e749")
  - **String**: Direct string value (e.g., "order-12345")
  - **Long/Int**: Numeric value as string (e.g., "9876543210")

#### Identifier Extraction Process
```csharp
// Parse _id string: "testdbgagent/99f2e278ae5e4a759075b15d64b4e749"
var parts = idString.Split('/', 2);
var agentTypePart = parts[0];     // "testdbgagent"
var identifierPart = parts[1];   // "99f2e278ae5e4a759075b15d64b4e749"

// Convert identifier part to appropriate type
if (typeof(TIdentifier) == typeof(Guid))
{
    if (Guid.TryParse(identifierPart, out var guid))
        return guid; // Parsed Guid from string
}
```

#### Error Handling
- **Invalid Format**: Documents not matching "{type}/{id}" pattern are logged and skipped
- **Parse Failures**: Identifiers that cannot be converted to target type are logged and skipped
- **Missing _id**: Documents without `_id` field are logged and skipped
- **Type Mismatches**: Clear error messages for unsupported identifier types

#### Performance Considerations
- **String Operations**: Efficient string splitting with limit to avoid unnecessary allocations
- **Type Conversion**: Direct parsing methods (Guid.TryParse, long.TryParse) for optimal performance
- **Error Logging**: Structured logging with context for debugging identifier extraction issues
- **Memory Efficiency**: Minimal string allocations during parsing process

This format ensures consistent agent identification across the Orleans ecosystem while maintaining compatibility with MongoDB's document structure and indexing capabilities.

### Identifier Retrieval

#### Automatic Type Detection
```csharp
// Analyzes agent interface to determine identifier type
if (typeof(IGrainWithGuidKey).IsAssignableFrom(agentType))
    return typeof(Guid);
else if (typeof(IGrainWithStringKey).IsAssignableFrom(agentType))
    return typeof(string);
// ... etc
```

#### MongoDB Query Optimization
```csharp
// Efficient identifier-only queries
var identifiers = await collection
    .Find(FilterDefinition<BsonDocument>.Empty)
    .Project(Builders<BsonDocument>.Projection.Include("_id"))
    .Limit(maxCount)
    .ToListAsync();
```

## Usage Examples

### Zero Configuration Setup
```csharp
// In Program.cs - Minimal setup
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.AddAgentWarmup(); // Uses all defaults
});

// Automatically discovers all GAgentBase agents with PubSubStore
// Uses default strategy with MongoDB identifier retrieval
// No manual configuration required
```

### Selective Configuration
```csharp
// Custom configuration with exclusions
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.AddAgentWarmup(config =>
    {
        config.AutoDiscovery.ExcludedAgentTypes.Add("Aevatar.GAgents.SystemGAgent");
        config.DefaultStrategy.MaxIdentifiersPerType = 500;
        config.MongoDbIntegration.BatchSize = 50;
    });
});
```

### Hybrid Approach (Automatic + Manual)
```csharp
// Combine automatic discovery with specific strategies
services.AddAgentWarmup(config =>
{
    config.AutoDiscovery.Enabled = true;
    config.DefaultStrategy.Enabled = true;
});

// Add specific strategies for important agents
services.AddPredefinedAgentWarmupStrategy<UserGAgent, Guid>("ImportantUsers", importantUserIds);
services.AddPredefinedAgentWarmupStrategy<OrderGAgent, Guid>("ImportantOrders", importantOrderIds);

// Add sampling strategies for load testing
services.AddSampleBasedAgentWarmupStrategy<ProductGAgent, Guid>("ProductSample", 0.1); // 10% sample
services.AddSampleBasedAgentWarmupStrategy<CategoryGAgent, Guid>("CategorySample", 0.2); // 20% sample
```

### Advanced Configuration
```csharp
services.AddAgentWarmup(config =>
{
    // Custom discovery criteria
    config.AutoDiscovery.BaseTypeName = "CustomAgentBase";
    config.AutoDiscovery.RequiredAttributes.Add("WarmupEligible");
    
    // Custom MongoDB settings
    config.MongoDbIntegration.CollectionNamingStrategy = "TypeName";
    config.MongoDbIntegration.CollectionPrefix = "CustomPrefix"; // Override automatic prefix
    config.MongoDbIntegration.BatchSize = 200;
    
    // Performance tuning
    config.MaxConcurrency = 20;
    config.MongoDbRateLimit.MaxOperationsPerSecond = 100;
});

// Add sample-based strategies for different environments
#if DEBUG
// Development: Use smaller samples for faster startup
services.AddSampleBasedAgentWarmupStrategy<UserGAgent, Guid>("DevUserSample", 0.01); // 1%
services.AddSampleBasedAgentWarmupStrategy<ProductGAgent, Guid>("DevProductSample", 0.02); // 2%
#elif STAGING
// Staging: Use moderate samples for realistic testing
services.AddSampleBasedAgentWarmupStrategy<UserGAgent, Guid>("StagingUserSample", 0.1); // 10%
services.AddSampleBasedAgentWarmupStrategy<ProductGAgent, Guid>("StagingProductSample", 0.05); // 5%
#else
// Production: Use larger samples for performance
services.AddSampleBasedAgentWarmupStrategy<UserGAgent, Guid>("ProdUserSample", 0.2); // 20%
services.AddSampleBasedAgentWarmupStrategy<ProductGAgent, Guid>("ProdProductSample", 0.1); // 10%
#endif

// Custom collection naming is handled internally by MongoDbAgentIdentifierService
// No additional registration required - can be enhanced in future if needed
```

## Execution Flow

### 1. Initialization Phase
```
1. Scan assemblies for agent types
2. Filter by base type and attributes
3. Apply exclusion list
4. Determine identifier types
5. Cache discovered types
```

### 2. Strategy Planning Phase
```
1. Collect all registered strategies
2. Group agent types by applicable strategies
3. Identify unassigned agent types
4. Create default strategy assignments
5. Sort by priority
```

### 3. Execution Phase
```
1. Execute high-priority strategies first
2. Process agent-specific strategies
3. Execute default strategy for remaining types
4. Apply rate limiting and concurrency controls
5. Monitor progress and handle errors
```

### 4. Monitoring Phase
```
1. Track progress per strategy and agent type
2. Aggregate statistics
3. Log performance metrics
4. Report completion status
```

## Performance Considerations

### Discovery Performance
- **Assembly Scanning**: Cached results, scan only once at startup
- **Reflection Optimization**: Compiled expressions for repeated operations
- **Parallel Processing**: Concurrent type analysis where possible

### MongoDB Performance
- **Connection Pooling**: Reuse connections across operations
- **Batch Processing**: Retrieve identifiers in configurable batches
- **Index Optimization**: Ensure proper indexing on identifier fields
- **Query Optimization**: Projection-only queries for identifier retrieval

### Memory Efficiency
- **Streaming Identifiers**: IAsyncEnumerable for large datasets
- **Lazy Loading**: Load identifiers only when needed
- **Garbage Collection**: Proper disposal of resources

## Error Handling and Resilience

### Discovery Errors
- **Assembly Load Failures**: Log and continue with available assemblies
- **Type Analysis Errors**: Skip problematic types, continue discovery
- **Attribute Missing**: Graceful handling of missing required attributes

### MongoDB Errors
- **Connection Failures**: Retry with exponential backoff
- **Collection Missing**: Log warning, skip agent type
- **Query Timeouts**: Configurable timeouts with fallback behavior
- **Authentication Issues**: Clear error messages and guidance

### Strategy Errors
- **Strategy Failures**: Isolate failures, continue with other strategies
- **Agent Activation Errors**: Retry individual agents, track failures
- **Resource Exhaustion**: Circuit breaker pattern for protection

## Migration Guide

### From Manual to Automatic Configuration

#### Before (Manual Configuration)
```csharp
// Old approach - manual registration
services.AddPredefinedAgentWarmupStrategy<UserGAgent, Guid>("Users", userIds);
services.AddPredefinedAgentWarmupStrategy<OrderGAgent, Guid>("Orders", orderIds);
services.AddPredefinedAgentWarmupStrategy<ProductGAgent, Guid>("Products", productIds);
```

#### After (Automatic Configuration)
```csharp
// New approach - automatic discovery
services.AddAgentWarmup(); // Discovers and warms up all eligible agents automatically
```

#### Hybrid Approach (Best of Both)
```csharp
// Automatic discovery + specific strategies for important agents
services.AddAgentWarmup(config =>
{
    config.AutoDiscovery.Enabled = true;
    config.DefaultStrategy.Enabled = true;
});

// Override default strategy for critical agents
services.AddPredefinedAgentWarmupStrategy<UserGAgent, Guid>("CriticalUsers", criticalUserIds);
services.AddPredefinedAgentWarmupStrategy<SystemGAgent, Guid>("CriticalSystems", criticalSystemIds);
```

### Configuration Migration
```json
{
  "AgentWarmup": {
    // Legacy settings (still supported)
    "Enabled": true,
    "MaxConcurrency": 10,
    
    // New automatic discovery settings
    "AutoDiscovery": {
      "Enabled": true,
      "ExcludedAgentTypes": ["Aevatar.GAgents.SystemGAgent"]
    },
    
    // New default strategy settings
    "DefaultStrategy": {
      "Enabled": true,
      "MaxIdentifiersPerType": 1000
    },
    
    // New MongoDB integration settings
    "MongoDbIntegration": {
      "BatchSize": 100,
      "CollectionNamingStrategy": "FullTypeName",
      "CollectionPrefix": "StreamStorage" // Automatically set based on Host:HostId (PubSubStore pattern)
    }
  }
}
```

## Future Enhancements

### Intelligent Warmup
- **Usage Pattern Analysis**: Warm up frequently accessed agents first
- **Machine Learning**: Predict optimal warmup timing and agent selection
- **Adaptive Strategies**: Adjust warmup behavior based on system performance

### Advanced MongoDB Integration
- **Change Stream Monitoring**: React to new agents added to collections
- **Sharding Support**: Handle sharded MongoDB deployments
- **Aggregation Pipelines**: Complex identifier selection logic

### Multi-Silo Coordination
- **Distributed Warmup**: Coordinate warmup across multiple silos
- **Load Balancing**: Distribute warmup load based on silo capacity
- **Conflict Resolution**: Handle overlapping warmup operations

### Enhanced Monitoring
- **Metrics Export**: Prometheus/OpenTelemetry integration
- **Health Checks**: Warmup system health monitoring
- **Performance Analytics**: Detailed warmup performance analysis

## Conclusion

The enhanced Agent Warmup System provides a fully autonomous, intelligent solution for proactive agent loading in Orleans applications. Key improvements include:

**Automation Benefits**:
- **Zero Configuration**: Works out-of-the-box with intelligent defaults
- **Automatic Discovery**: Finds eligible agents without manual registration
- **Self-Adapting**: Automatically includes new agent types as they're added
- **MongoDB Integration**: Direct identifier retrieval without manual specification

**Flexibility Benefits**:
- **Strategy Decoupling**: Strategies can apply to multiple agent types
- **Priority System**: Control execution order for optimal performance
- **Hybrid Approach**: Mix automatic and manual configuration as needed
- **Extensible Design**: Easy to add custom strategies and discovery rules

**Performance Benefits**:
- **Intelligent Execution**: Specific strategies first, then default for remaining types
- **MongoDB Optimization**: Direct collection access with efficient queries
- **Rate Limiting**: Prevents database overload during warmup
- **Progressive Scaling**: Gradual increase in warmup intensity

**Operational Benefits**:
- **Reduced Maintenance**: Minimal configuration updates required
- **Error Resilience**: Graceful handling of missing collections and failed agents
- **Comprehensive Monitoring**: Detailed progress tracking and performance metrics
- **Backward Compatibility**: Existing configurations continue to work

This design enables applications to achieve consistent, predictable performance by eliminating cold start penalties while protecting critical database resources from overload scenarios, all with minimal configuration and maintenance overhead. 