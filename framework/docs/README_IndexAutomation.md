# Automatic Index Creation for MongoDB Event Sourcing Collections

This document explains how to use the automatic index creation feature for MongoDB event sourcing collections in the Aevatar framework.

## Overview

Previously, MongoDB event sourcing collections were created with only the default `_id` index, requiring manual index creation for optimal query performance. This feature automates index creation during collection initialization using the `EventSourcingCollectionFactory` pattern with real MongoDB instances for both production and testing scenarios.

## Features

- **Automatic Default Indexes**: Provides optimized indexes for common event sourcing query patterns
- **Custom Index Configuration**: Allows defining custom indexes through configuration
- **Graceful Error Handling**: Handles index conflicts and creation failures gracefully
- **Configurable**: Can be enabled/disabled and customized per storage provider

## Default Indexes

When `CreateDefaultIndexes` is enabled, the following indexes are automatically created:

1. **GrainId_1**: `{ "GrainId": 1 }` - For single grain queries
2. **GrainId_1_Version_-1**: `{ "GrainId": 1, "Version": -1 }` - For efficient version-based range queries

Index names follow MongoDB's default naming convention: `fieldname_1_field2_-1` where 1 indicates ascending and -1 indicates descending sort order.

## Configuration

### Basic Configuration

```csharp
services.Configure<MongoDbStorageOptions>(options =>
{
    options.ClientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
    options.Database = "EventSourcingDatabase";
    
    // Enable automatic index creation (default: true)
    options.CreateIndexesOnInitialization = true;
    
    // Enable default indexes (default: true)
    options.CreateDefaultIndexes = true;
    
    // Ignore index conflicts (default: true)
    options.IgnoreIndexConflicts = true;
});
```

### Custom Index Configuration

```csharp
services.Configure<MongoDbStorageOptions>(options =>
{
    options.ClientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
    options.Database = "EventSourcingDatabase";
    
    // Define custom indexes
    options.Indexes = new List<IndexDefinition>
    {
        // Single field index with explicit name
        new IndexDefinition
        {
            Fields = new[] { new IndexKey("Timestamp", 1) },
            Options = new CreateIndexOptions { Name = "TimestampIndex" }
        },
        
        // Compound index using MongoDB default naming (will be named "GrainType_1_Version_1")
        new IndexDefinition
        {
            Fields = new[] { 
                new IndexKey("GrainType", 1), 
                new IndexKey("Version", 1) 
            }
            // No explicit name provided - will use MongoDB default: "GrainType_1_Version_1"
        },
        
        // Custom index with options
        new IndexDefinition
        {
            Fields = new[] { new IndexKey("OptionalField", 1) },
            Options = new CreateIndexOptions { 
                Name = "SparseIndex",
                Sparse = true 
            }
        }
    };
});
```

### Advanced Configuration

```csharp
services.Configure<MongoDbStorageOptions>(options =>
{
    options.ClientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
    options.Database = "EventSourcingDatabase";
    
    // Disable default indexes and use only custom ones
    options.CreateDefaultIndexes = false;
    
    // Custom performance-optimized indexes
    options.Indexes = new List<IndexDefinition>
    {
        // Optimized for range queries by grain and version
        new IndexDefinition
        {
            Fields = new[]
            {
                new IndexKey("GrainId", 1),
                new IndexKey("Version", 1)
            },
            Options = new CreateIndexOptions
            {
                Name = "OptimizedRangeIndex",
                Background = true
            }
        },
        
        // Text search index for event content
        new IndexDefinition
        {
            Fields = new[] { new IndexKey("Content", 1) },
            Options = new CreateIndexOptions
            {
                Name = "ContentTextIndex"
            }
        }
    };
});
```

## Performance Benefits

The automatic index creation provides significant performance improvements for common event sourcing operations:

### Before (Only `_id` index)
```javascript
// Query: Find events for a specific grain
db.collection.find({"GrainId": "MyGrain/12345678-1234-1234-1234-123456789abc"})
// Result: Collection scan - slow for large collections

// Query: Find events for grain in version range  
db.collection.find({"GrainId": "MyGrain/12345678-1234-1234-1234-123456789abc", "Version": {$gte: 10, $lte: 20}})
// Result: Collection scan - very slow
```

### After (With default indexes)
```javascript
// Query: Find events for a specific grain
db.collection.find({"GrainId": "MyGrain/12345678-1234-1234-1234-123456789abc"})
// Result: Index scan using GrainId_1 - fast

// Query: Find events for grain in version range
db.collection.find({"GrainId": "MyGrain/12345678-1234-1234-1234-123456789abc", "Version": {$gte: 10, $lte: 20}})
// Result: Index scan using GrainId_1_Version_-1 - very fast
```

## Migration

### Existing Collections

The feature is backward compatible. Existing collections will automatically get the new indexes created on first access:

```csharp
// No code changes needed - indexes will be created automatically
var grainId = Guid.NewGuid().ToString();
var logConsistentGrain = grainFactory.GetGrain<IMyEventSourcingGrain>(grainId);
await logConsistentGrain.DoSomething(); // Indexes created during first storage access
```

### Gradual Migration

You can enable the feature gradually:

```csharp
// Phase 1: Enable only for new storage providers
services.Configure<MongoDbStorageOptions>("NewEventStore", options => 
{
    options.CreateIndexesOnInitialization = true;
    // ... other options
});

// Phase 2: Existing providers can be migrated later
services.Configure<MongoDbStorageOptions>("ExistingEventStore", options => 
{
    options.CreateIndexesOnInitialization = true; // Enable when ready
    // ... other options
});
```

## Troubleshooting

### Index Creation Failures

Index creation failures are logged but don't prevent the application from starting:

```csharp
// Check logs for index creation status
// INFO: Successfully created 2 indexes for collection MyEventStore
// WARN: Index GrainId_1 already exists with different options, skipping creation
// ERROR: Failed to create index CustomIndex: <error details>
```

### Disable Index Creation

If you need to disable automatic index creation:

```csharp
services.Configure<MongoDbStorageOptions>(options =>
{
    options.CreateIndexesOnInitialization = false; // Disable automatic creation
    options.CreateDefaultIndexes = false;          // Disable default indexes
});
```

### Manual Index Management

You can still create indexes manually if needed:

```csharp
// Access the underlying MongoDB collection if needed
var database = mongoClient.GetDatabase("EventSourcingDatabase");
var collection = database.GetCollection<BsonDocument>("MyCollectionName");

await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("CustomField")));
```

## Best Practices

1. **Use Default Indexes**: They cover most common event sourcing query patterns
2. **Use GUID-based Grain IDs**: Use `Guid.NewGuid().ToString()` for grain primary keys to ensure uniqueness and avoid conflicts
3. **Monitor Performance**: Use MongoDB profiler to identify slow queries that might need additional indexes
4. **Index Maintenance**: Consider index maintenance in your deployment process
5. **Test Configuration**: Test index configuration in development before production deployment
6. **Background Creation**: Use `Background = true` for indexes on large existing collections
7. **Real MongoDB Testing**: Use `AevatarMongoDbFixture` for tests instead of mocking MongoDB operations

## Example: Complete Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<MongoDbStorageOptions>(options =>
    {
        options.ClientSettings = MongoClientSettings.FromConnectionString(
            Configuration.GetConnectionString("MongoDB"));
        options.Database = "ProductionEventStore";
        
        // Enable automatic index creation with defaults
        options.CreateIndexesOnInitialization = true;
        options.CreateDefaultIndexes = true;
        options.IgnoreIndexConflicts = true;
        
        // Add application-specific indexes
        options.Indexes = new List<IndexDefinition>
        {
            // Index for tenant-based queries
            new IndexDefinition
            {
                Fields = new[] { 
                    new IndexKey("TenantId", 1), 
                    new IndexKey("GrainId", 1) 
                },
                Options = new CreateIndexOptions { Name = "TenantGrainIndex" }
            },
            
            // Index for time-based queries
            new IndexDefinition
            {
                Fields = new[] { new IndexKey("CreatedAt", 1) },
                Options = new CreateIndexOptions { Name = "CreatedAtIndex" }
            },
            
            // Sparse index for optional correlationId
            new IndexDefinition
            {
                Fields = new[] { new IndexKey("CorrelationId", 1) },
                Options = new CreateIndexOptions { 
                    Name = "CorrelationIndex",
                    Sparse = true, 
                    Background = true 
                }
            }
        };
    });
}
```

This automated index creation feature significantly improves the out-of-box performance of MongoDB event sourcing while maintaining flexibility for custom indexing strategies.