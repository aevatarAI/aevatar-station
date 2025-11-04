using System;
using Aevatar.EventSourcing.MongoDB.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;

namespace Aevatar.EventSourcing.MongoDB.Collections;

/// <summary>
/// MongoDB collection for event sourcing that follows Orleans.Providers.MongoDB patterns.
/// Inherits from CollectionBase and implements automatic index creation for event sourcing collections.
/// </summary>
public class EventSourcingCollection : CollectionBase<BsonDocument>, IEventSourcingCollection
{
    private readonly string _collectionName;
    private readonly MongoDbStorageOptions _options;
    private readonly ILogger<EventSourcingCollection> _logger;

    public EventSourcingCollection(
        IMongoClient mongoClient,
        string databaseName,
        string collectionName,
        MongoDbStorageOptions options,
        Action<MongoCollectionSettings> collectionConfigurator,
        bool createShardKey,
        ILogger<EventSourcingCollection> logger)
        : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
    {
        _collectionName = collectionName;
        _options = options;
        _logger = logger;
    }

    protected override string CollectionName()
    {
        return _collectionName;
    }

    /// <summary>
    /// Gets the MongoDB collection with automatic index management.
    /// This is a public wrapper around the protected Collection property from CollectionBase.
    /// </summary>
    public virtual IMongoCollection<BsonDocument> GetCollection()
    {
        return Collection;
    }

    protected override void SetupCollection(IMongoCollection<BsonDocument> collection)
    {
        if (!_options.CreateIndexesOnInitialization)
        {
            _logger.LogDebug("Index creation is disabled for collection {CollectionName}", _collectionName);
            return;
        }

        // Create default indexes for event sourcing if no custom indexes are specified
        if (_options.Indexes?.Count == 0)
        {
            CreateDefaultEventSourcingIndexes(collection);
        }
        else
        {
            // Create custom indexes specified in options
            CreateCustomIndexes(collection);
        }
    }

    private void CreateDefaultEventSourcingIndexes(IMongoCollection<BsonDocument> collection)
    {
        _logger.LogDebug("Creating default event sourcing indexes for collection {CollectionName}", _collectionName);

        // Index 1: GrainId (ascending) - for single grain queries
        var grainIdDefinition = Index.Ascending("GrainId");
        var grainIdIndexName = GenerateMongoDbIndexName(new[] { ("GrainId", 1) });
        CreateIndexSafely(collection, grainIdDefinition, grainIdIndexName);

        // Index 2: GrainId + Version (compound) - for efficient version range queries
        var grainVersionDefinition = Index
            .Ascending("GrainId")
            .Descending("Version");
        var grainVersionIndexName = GenerateMongoDbIndexName(new[] { ("GrainId", 1), ("Version", -1) });
        CreateIndexSafely(collection, grainVersionDefinition, grainVersionIndexName);
    }

    private void CreateCustomIndexes(IMongoCollection<BsonDocument> collection)
    {
        if (_options.Indexes == null) return;

        _logger.LogDebug("Creating {IndexCount} custom indexes for collection {CollectionName}", 
            _options.Indexes.Count, _collectionName);

        foreach (var indexDef in _options.Indexes)
        {
            try
            {
                var indexBuilder = Index;
                IndexKeysDefinition<BsonDocument>? combinedDefinition = null;

                foreach (var key in indexDef.Keys)
                {
                    var keyDefinition = key.Direction == Options.SortDirection.Ascending
                        ? indexBuilder.Ascending(key.FieldName)
                        : indexBuilder.Descending(key.FieldName);

                    combinedDefinition = combinedDefinition == null 
                        ? keyDefinition 
                        : indexBuilder.Combine(combinedDefinition, keyDefinition);
                }

                if (combinedDefinition != null)
                {
                    // Use MongoDB default naming convention if no name is explicitly provided
                    var indexName = string.IsNullOrEmpty(indexDef.Name) 
                        ? GenerateMongoDbIndexName(indexDef.Keys.Select(k => 
                            (k.FieldName, k.Direction == Options.SortDirection.Ascending ? 1 : -1)).ToArray())
                        : indexDef.Name;
                    
                    CreateIndexSafely(collection, combinedDefinition, indexName, indexDef.Options);
                }
            }
            catch (Exception ex)
            {
                if (_options.IgnoreIndexConflicts)
                {
                    _logger.LogWarning(ex, "Failed to create custom index {IndexName} for collection {CollectionName}, but continuing due to IgnoreIndexConflicts setting", 
                        indexDef.Name, _collectionName);
                }
                else
                {
                    _logger.LogError(ex, "Failed to create custom index {IndexName} for collection {CollectionName}", 
                        indexDef.Name, _collectionName);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Generates MongoDB-style index names following the convention: fieldname_1_field2_-1
    /// </summary>
    /// <param name="fields">Array of tuples containing field name and direction (1 for ascending, -1 for descending)</param>
    /// <returns>MongoDB-style index name</returns>
    private static string GenerateMongoDbIndexName((string fieldName, int direction)[] fields)
    {
        return string.Join("_", fields.SelectMany(f => new[] { f.fieldName, f.direction.ToString() }));
    }

    private void CreateIndexSafely(IMongoCollection<BsonDocument> collection, 
        IndexKeysDefinition<BsonDocument> indexDefinition, 
        string indexName, 
        CreateIndexOptions? customOptions = null)
    {
        try
        {
            var options = customOptions ?? new CreateIndexOptions { Name = indexName };
            if (options.Name == null) options.Name = indexName;

            collection.Indexes.CreateOne(
                new CreateIndexModel<BsonDocument>(indexDefinition, options));

            _logger.LogDebug("Successfully created index {IndexName} for collection {CollectionName}", 
                indexName, _collectionName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict")
        {
            // Follow Orleans pattern: retry without options if there's a conflict
            if (_options.IgnoreIndexConflicts)
            {
                try
                {
                    collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexDefinition));
                    _logger.LogDebug("Created index {IndexName} without options after conflict for collection {CollectionName}", 
                        indexName, _collectionName);
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning(ex2, "Failed to create index {IndexName} even without options for collection {CollectionName}", 
                        indexName, _collectionName);
                }
            }
            else
            {
                _logger.LogError(ex, "Index options conflict for {IndexName} in collection {CollectionName}", 
                    indexName, _collectionName);
                throw;
            }
        }
        catch (Exception ex)
        {
            if (_options.IgnoreIndexConflicts)
            {
                _logger.LogWarning(ex, "Failed to create index {IndexName} for collection {CollectionName}, but continuing due to IgnoreIndexConflicts setting", 
                    indexName, _collectionName);
            }
            else
            {
                _logger.LogError(ex, "Failed to create index {IndexName} for collection {CollectionName}", 
                    indexName, _collectionName);
                throw;
            }
        }
    }
} 