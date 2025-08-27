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

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Service for retrieving agent identifiers directly from MongoDB collections
/// </summary>
public class MongoDbAgentIdentifierService : IMongoDbAgentIdentifierService
{
    private readonly MongoDbIntegrationConfiguration _config;
    private readonly ILogger<MongoDbAgentIdentifierService> _logger;
    private readonly IMongoDatabase _database;

    public MongoDbAgentIdentifierService(
        IOptions<AgentWarmupConfiguration> options,
        ILogger<MongoDbAgentIdentifierService> logger,
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

    public async IAsyncEnumerable<TIdentifier> GetAgentIdentifiersAsync<TIdentifier>(
        Type agentType, 
        int? maxCount = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collectionName = GetCollectionName(agentType);
        
        // Check if collection exists first
        if (!await CollectionExistsAsync(agentType))
        {
            _logger.LogWarning("Collection {CollectionName} does not exist for agent type {AgentType}", 
                collectionName, agentType.Name);
            yield break;
        }

        IAsyncEnumerable<TIdentifier> identifiers;
        try
        {
            identifiers = GetIdentifiersFromCollection<TIdentifier>(collectionName, maxCount, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up identifier retrieval from collection {CollectionName} for agent type {AgentType}", 
                collectionName, agentType.Name);
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

    public string GetCollectionName(Type agentType)
    {
        var baseName = _config.CollectionNamingStrategy.ToLowerInvariant() switch
        {
            "typename" => agentType.Name,
            "fulltypename" => agentType.FullName ?? agentType.Name,
            "custom" => GetCustomCollectionName(agentType),
            _ => agentType.FullName ?? agentType.Name
        };

        // Prepend collection prefix if configured
        return string.IsNullOrEmpty(_config.CollectionPrefix) 
            ? baseName 
            : $"{_config.CollectionPrefix}{baseName}";
    }

    public async Task<bool> CollectionExistsAsync(Type agentType)
    {
        try
        {
            var collectionName = GetCollectionName(agentType);
            _logger.LogInformation("Checking if collection {CollectionName} exists", collectionName);
            var collections = await _database.ListCollectionNamesAsync();
            
            // Convert to list and check
            var collectionList = await collections.ToListAsync();
            return collectionList.Contains(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if collection exists for agent type {AgentType}", agentType.Name);
            return false;
        }
    }

    public async Task<long> GetAgentCountAsync(Type agentType)
    {
        try
        {
            var collectionName = GetCollectionName(agentType);
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            
            return await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent count for collection {CollectionName}", GetCollectionName(agentType));
            return 0;
        }
    }

    private TIdentifier? ExtractIdentifier<TIdentifier>(BsonDocument document)
    {
        try
        {
            if (!document.TryGetValue("_id", out var idValue))
                return default;

            // MongoDB _id format is "{agenttypestring-lower-case}/{identifier}"
            // e.g., "testdbgagent/99f2e278ae5e4a759075b15d64b4e749"
            if (!idValue.IsString)
            {
                _logger.LogWarning("Expected MongoDB _id to be string in format 'agenttype/identifier', but got {IdType}", 
                    idValue.BsonType);
                return default;
            }

            var idString = idValue.AsString;
            var parts = idString.Split('/', 2); // Split into max 2 parts
            
            if (parts.Length != 2)
            {
                _logger.LogWarning("MongoDB _id '{IdString}' does not match expected format 'agenttype/identifier'", 
                    idString);
                return default;
            }

            var agentTypePart = parts[0]; // e.g., "testdbgagent"
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

    private string GetCustomCollectionName(Type agentType)
    {
        // Default custom naming strategy - can be overridden by configuration
        // Format: agents_{typename_lowercase}
        return $"agents_{agentType.Name.ToLowerInvariant()}";
    }
} 