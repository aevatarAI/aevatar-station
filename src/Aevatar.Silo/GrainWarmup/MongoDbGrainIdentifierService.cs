using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Service for retrieving grain identifiers directly from MongoDB collections
/// </summary>
public class MongoDbGrainIdentifierService : IMongoDbGrainIdentifierService
{
    private readonly MongoDbIntegrationConfiguration _config;
    private readonly ILogger<MongoDbGrainIdentifierService> _logger;
    private readonly IMongoDatabase _database;

    public MongoDbGrainIdentifierService(
        IOptions<GrainWarmupConfiguration> options,
        ILogger<MongoDbGrainIdentifierService> logger,
        IConfiguration configuration)
    {
        _config = options.Value.MongoDbIntegration;
        _logger = logger;
        
        // Create our own MongoDB client to avoid ABP dependency conflicts
        var connectionString = configuration.GetSection("Orleans:MongoDBClient").Value;
        var databaseName = configuration.GetSection("Orleans:DataBase").Value;
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async IAsyncEnumerable<TIdentifier> GetGrainIdentifiersAsync<TIdentifier>(
        Type grainType, 
        int? maxCount = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collectionName = GetCollectionName(grainType);
        
        // Check if collection exists first
        if (!await CollectionExistsAsync(grainType))
        {
            _logger.LogWarning("Collection {CollectionName} does not exist for grain type {GrainType}", 
                collectionName, grainType.Name);
            yield break;
        }

        IAsyncEnumerable<TIdentifier> identifiers;
        try
        {
            identifiers = GetIdentifiersFromCollection<TIdentifier>(collectionName, maxCount, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up identifier retrieval from collection {CollectionName} for grain type {GrainType}", 
                collectionName, grainType.Name);
            yield break;
        }

        await foreach (var identifier in identifiers)
        {
            yield return identifier;
        }
    }

    private async IAsyncEnumerable<TIdentifier> GetIdentifiersFromCollection<TIdentifier>(
        string collectionName, 
        int? maxCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        
        var filter = Builders<BsonDocument>.Filter.Empty;
        var projection = Builders<BsonDocument>.Projection.Include("_id");
        
        var findOptions = new FindOptions<BsonDocument, BsonDocument>
        {
            Projection = projection,
            BatchSize = _config.BatchSize
        };

        if (maxCount.HasValue)
        {
            findOptions.Limit = maxCount.Value;
        }

        using var cursor = await collection.FindAsync(filter, findOptions, cancellationToken);
        
        var processedCount = 0;
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var document in cursor.Current)
            {
                if (maxCount.HasValue && processedCount >= maxCount.Value)
                    yield break;

                var identifier = ExtractIdentifier<TIdentifier>(document);
                if (identifier != null)
                {
                    yield return identifier;
                    processedCount++;
                }
            }
        }

        _logger.LogDebug("Retrieved {Count} identifiers from collection {CollectionName}", 
            processedCount, collectionName);
    }

    public string GetCollectionName(Type grainType)
    {
        var baseName = _config.CollectionNamingStrategy.ToLowerInvariant() switch
        {
            "typename" => grainType.Name,
            "fulltypename" => grainType.FullName ?? grainType.Name,
            "custom" => GetCustomCollectionName(grainType),
            _ => grainType.FullName ?? grainType.Name
        };

        // Prepend collection prefix if configured
        return string.IsNullOrEmpty(_config.CollectionPrefix) 
            ? baseName 
            : $"{_config.CollectionPrefix}{baseName}";
    }

    public async Task<bool> CollectionExistsAsync(Type grainType)
    {
        try
        {
            var collectionName = GetCollectionName(grainType);
            _logger.LogInformation("Checking if collection {CollectionName} exists", collectionName);
            var collections = await _database.ListCollectionNamesAsync();
            
            // Convert to list and check
            var collectionList = await collections.ToListAsync();
            return collectionList.Contains(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if collection exists for grain type {GrainType}", grainType.Name);
            return false;
        }
    }

    public async Task<long> GetGrainCountAsync(Type grainType)
    {
        try
        {
            var collectionName = GetCollectionName(grainType);
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            
            return await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grain count for collection {CollectionName}", GetCollectionName(grainType));
            return 0;
        }
    }

    private TIdentifier? ExtractIdentifier<TIdentifier>(BsonDocument document)
    {
        try
        {
            if (!document.TryGetValue("_id", out var idValue))
                return default;

            // MongoDB _id format is "{graintypestring-lower-case}/{identifier}"
            // e.g., "testdbgagent/99f2e278ae5e4a759075b15d64b4e749"
            if (!idValue.IsString)
            {
                _logger.LogWarning("Expected MongoDB _id to be string in format 'graintype/identifier', but got {IdType}", 
                    idValue.BsonType);
                return default;
            }

            var idString = idValue.AsString;
            var parts = idString.Split('/', 2); // Split into max 2 parts
            
            if (parts.Length != 2)
            {
                _logger.LogWarning("MongoDB _id '{IdString}' does not match expected format 'graintype/identifier'", 
                    idString);
                return default;
            }

            var grainTypePart = parts[0]; // e.g., "testdbgagent"
            var identifierPart = parts[1]; // e.g., "99f2e278ae5e4a759075b15d64b4e749"

            // Convert the identifier part to the requested type
            if (typeof(TIdentifier) == typeof(Guid))
            {
                if (Guid.TryParse(identifierPart, out var guid))
                {
                    return (TIdentifier)(object)guid;
                }
                else
                {
                    _logger.LogWarning("Could not parse identifier part '{IdentifierPart}' as Guid from _id '{IdString}'", 
                        identifierPart, idString);
                    return default;
                }
            }
            else if (typeof(TIdentifier) == typeof(string))
            {
                // For string identifiers, return the identifier part directly
                return (TIdentifier)(object)identifierPart;
            }
            else if (typeof(TIdentifier) == typeof(long))
            {
                if (long.TryParse(identifierPart, out var longValue))
                {
                    return (TIdentifier)(object)longValue;
                }
                else
                {
                    _logger.LogWarning("Could not parse identifier part '{IdentifierPart}' as long from _id '{IdString}'", 
                        identifierPart, idString);
                    return default;
                }
            }
            else if (typeof(TIdentifier) == typeof(int))
            {
                if (int.TryParse(identifierPart, out var intValue))
                {
                    return (TIdentifier)(object)intValue;
                }
                else
                {
                    _logger.LogWarning("Could not parse identifier part '{IdentifierPart}' as int from _id '{IdString}'", 
                        identifierPart, idString);
                    return default;
                }
            }

            _logger.LogWarning("Unsupported identifier type {IdentifierType} for _id '{IdString}'", 
                typeof(TIdentifier).Name, idString);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting identifier from document");
            return default;
        }
    }

    private string GetCustomCollectionName(Type grainType)
    {
        // Default custom naming strategy - can be overridden by configuration
        // Format: grains_{typename_lowercase}
        return $"grains_{grainType.Name.ToLowerInvariant()}";
    }
} 