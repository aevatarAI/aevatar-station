using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.EventSourcing.Core.Snapshot;
using Aevatar.StateExport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;

namespace Aevatar.Silo.Grains.StateExport;

/// <summary>
/// Grain that exports State data using Orleans serialization
/// </summary>
public class StateExportGrain : Grain, IStateExportGrain
{
    private readonly ILogger<StateExportGrain> _logger;
    private readonly IGrainStateSerializer _serializer;
    private readonly IMongoDatabase _database;
    private readonly Dictionary<string, Type> _stateTypeCache = new();

    public StateExportGrain(
        ILogger<StateExportGrain> logger,
        IGrainStateSerializer serializer,
        IConfiguration configuration)
    {
        _logger = logger;
        _serializer = serializer;
        
        var connectionString = configuration["StateExport:ConnectionString"] 
            ?? configuration["Orleans:MongoDBClient"]
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["StateExport:Database"] 
            ?? configuration["Orleans:DataBase"]
            ?? "AevatarDb";
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        
        // Test connection and log actual database
        try
        {
            var serverInfo = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            var actualDb = _database.RunCommand<BsonDocument>(new BsonDocument("dbStats", 1));
            var dbName = actualDb.GetValue("db", databaseName).AsString;
            
            _logger.LogInformation("StateExportGrain initialized: Database={Database}, Connection={Connection}", 
                dbName, connectionString.Split('@').LastOrDefault() ?? connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify MongoDB connection: {Connection}. Will attempt to use anyway.", connectionString);
            // Don't throw - allow lazy connection
        }
    }
    
    public async Task<List<StateCollectionInfo>> GetCollectionsAsync()
    {
        var result = new List<StateCollectionInfo>();
        
        var collectionNames = await _database.ListCollectionNamesAsync();
        var names = await collectionNames.ToListAsync();
        
        // Support "Stream" and "Orleans" prefixed collections
        foreach (var name in names.Where(n => n.StartsWith("Stream") || n.StartsWith("Orleans")))
        {
            var collection = _database.GetCollection<BsonDocument>(name);
            // Use EstimatedDocumentCountAsync for better performance on large collections
            var count = await collection.EstimatedDocumentCountAsync();
            
            result.Add(new StateCollectionInfo
            {
                CollectionName = name,
                TypeName = ExtractTypeName(name),
                Count = count
            });
        }
        
        return result.OrderBy(c => c.TypeName).ToList();
    }

    public async Task<StateExportResult> ExportAsync(string collectionName, int skip, int limit)
    {
        _logger.LogInformation("Exporting {Collection} skip={Skip} limit={Limit}", 
            collectionName, skip, limit);
        
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var typeName = ExtractTypeName(collectionName);
        
        // Determine storage type based on collection prefix
        var isOrleansGrain = collectionName.StartsWith("Orleans", StringComparison.OrdinalIgnoreCase);
        var isEventSourcingSnapshot = collectionName.StartsWith("Stream", StringComparison.OrdinalIgnoreCase);
        
        
        FilterDefinition<BsonDocument> stateFilter;
        if (isOrleansGrain)
        {
            // Orleans Grain: accept both direct BSON fields and binary data format
            // Exclude EventSourcing log format (has Log + GlobalVersion)
            stateFilter = Builders<BsonDocument>.Filter.Not(
                Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Exists("_doc.Log", true),
                    Builders<BsonDocument>.Filter.Exists("_doc.GlobalVersion", true)
                )
            );
        }
        else if (isEventSourcingSnapshot)
        {
            // EventSourcing: filter for State format (has _doc.data), exclude Log format
            stateFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("_doc.data", true),
                Builders<BsonDocument>.Filter.Not(
                    Builders<BsonDocument>.Filter.Exists("_doc.Log", true)
                )
            );
        }
        else
        {
            // Unknown format, try to read all
            stateFilter = Builders<BsonDocument>.Filter.Empty;
        }
        
        // Fetch documents matching filter (removed TotalCount calculation for performance)
        // Fetch limit+1 to determine HasMore efficiently
        var documents = await collection.Find(stateFilter)
            .Project(Builders<BsonDocument>.Projection.Include("_id").Include("_doc").Include("_etag"))
            .Skip(skip)
            .Limit(limit + 1)
            .ToListAsync();
        
        // Find State type once for all documents
        var stateType = FindStateType(typeName);
        
        var records = new List<ExportedStateRecord>();
        var successCount = 0;
        
        foreach (var doc in documents)
        {
            try
            {
                var record = DeserializeDocument(doc, typeName, stateType);
                if (record != null)
                {
                    records.Add(record);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to deserialize document from {Collection}", collectionName);
            }
        }
        
        _logger.LogInformation("Exported {Success} records from {Collection}", successCount, collectionName);
        
        // HasMore: if MongoDB returned more documents than limit, there is more data
        // We fetched limit+1 to check, so if we got limit+1, there's more
        var hasMore = documents.Count > limit;
        
        // If we fetched extra, trim it (keep only limit records)
        if (hasMore && records.Count > limit)
        {
            records = records.Take(limit).ToList();
        }
        
        return new StateExportResult
        {
            CollectionName = collectionName,
            TypeName = typeName,
            Skip = skip,
            Limit = limit,
            HasMore = hasMore,
            Records = records
        };
    }

    private ExportedStateRecord? DeserializeDocument(BsonDocument doc, string typeName, Type? stateType)
    {
        var id = doc.GetValue("_id", "").AsString;
        var etag = doc.GetValue("_etag", "").AsString;
        
        if (!doc.Contains("_doc"))
        {
            return new ExportedStateRecord
            {
                Id = id,
                ETag = etag,
                State = BsonDocumentToDict(doc)
            };
        }
        
        var innerDoc = doc["_doc"];
        if (!innerDoc.IsBsonDocument) return null;
        
        var docContent = innerDoc.AsBsonDocument;
        
        // Skip EventSourcing format (historical data)
        if (IsEventSourcingFormat(docContent))
        {
            _logger.LogDebug("Skipping EventSourcing document {Id} - historical data", id);
            return null;
        }
        
        // Check document format
        var hasData = docContent.Contains("data");
        var dataSize = 0;
        if (hasData && docContent["data"].IsBsonBinaryData)
        {
            dataSize = docContent["data"].AsBsonBinaryData.Bytes.Length;
        }
        
        // Case 1: Direct BSON fields format (no 'data' field) - Orleans JSON storage
        if (!hasData && docContent.ElementCount > 0 && !docContent.Contains("__id"))
        {
            return new ExportedStateRecord
            {
                Id = id,
                ETag = etag,
                State = BsonDocumentToDict(docContent)
            };
        }
        
        // Case 2: Direct BSON fields with __id (Orleans JSON storage variant)
        if (!hasData && docContent.Contains("__id"))
        {
            var stateDict = BsonDocumentToDict(docContent);
            stateDict.Remove("__id");
            return new ExportedStateRecord
            {
                Id = id,
                ETag = etag,
                State = stateDict
            };
        }
        
        // Case 3: Binary data format - need deserialization
        if (stateType != null && hasData)
        {
            try
            {
                var deserializeMethod = _serializer.GetType()
                    .GetMethod("Deserialize", new[] { typeof(BsonValue) })!
                    .MakeGenericMethod(stateType);
                
                var state = deserializeMethod.Invoke(_serializer, new object[] { innerDoc });
                
                if (state != null)
                {
                    var stateDict = ObjectToDict(state);
                    
                    // Count non-default fields to detect failed deserialization
                    var nonNullFields = stateDict.Count(kvp => {
                        var v = kvp.Value;
                        if (v == null) return false;
                        if (v is string s && string.IsNullOrEmpty(s)) return false;
                        if (v is bool b && !b) return false;
                        if (v is int i && i == 0) return false;
                        if (v is Guid g && g == Guid.Empty) return false;
                        if (v is DateTime dt && dt == default(DateTime)) return false;
                        if (v is Dictionary<string, object?> dict && dict.Count == 0) return false;
                        return true;
                    });
                    
                    // If all defaults, try EventSourcing snapshot wrapper
                    if (nonNullFields == 0 && dataSize > 100)
                    {
                        var snapshotRecord = TryDeserializeEventSourcingSnapshot(innerDoc, stateType, id, etag, dataSize);
                        if (snapshotRecord != null)
                        {
                            return snapshotRecord;
                        }

                        _logger.LogDebug("Deserialization returned all defaults for {Type}, returning raw base64", stateType.Name);
                        return new ExportedStateRecord
                        {
                            Id = id,
                            ETag = etag,
                            State = BsonDocumentToDict(innerDoc.AsBsonDocument)
                        };
                    }
                    
                    return new ExportedStateRecord
                    {
                        Id = id,
                        ETag = etag,
                        State = stateDict
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Deserialization failed for {Type}, falling back to raw BSON", typeName);
            }
        }
        
        // Fallback: return raw BSON structure (for debugging)
        return new ExportedStateRecord
        {
            Id = id,
            ETag = etag,
            State = BsonDocumentToDict(innerDoc.AsBsonDocument)
        };
    }
    
    /// <summary>
    /// Detects EventSourcing format data which contains Log history, not current state
    /// </summary>
    private static bool IsEventSourcingFormat(BsonDocument doc)
    {
        // Check for __type containing EventSourcing markers
        if (doc.Contains("__type"))
        {
            var typeValue = doc["__type"].AsString;
            if (typeValue.Contains("Orleans.EventSourcing") || 
                typeValue.Contains("LogStateWithMetaData") ||
                typeValue.Contains("LogStorage"))
            {
                return true;
            }
        }
        
        // Check for Log field (EventSourcing log structure)
        if (doc.Contains("Log"))
        {
            return true;
        }
        
        // Check for GlobalVersion + WriteVector (EventSourcing metadata)
        if (doc.Contains("GlobalVersion") && doc.Contains("WriteVector"))
        {
            return true;
        }
        
        // Check if binary data contains EventLog/LogEvent markers (old event log format)
        if (doc.Contains("data") && doc["data"].IsBsonBinaryData)
        {
            var bytes = doc["data"].AsBsonBinaryData.Bytes;
            if (bytes.Length > 20 && bytes.Length < 2000) // Event logs are typically small
            {
                // Convert first part of binary to string to check for EventLog markers
                var sampleText = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 200));
                if (sampleText.Contains("EventLog") || sampleText.Contains("LogEvent"))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private Type? FindStateType(string typeName)
    {
        if (_stateTypeCache.TryGetValue(typeName, out var cached))
            return cached;
        
        // Explicit mappings for special cases where naming convention doesn't match
        var explicitMappings = new Dictionary<string, string[]>
        {
            // AnonymousUserGAgent -> AnonymousUserState (in Aevatar.Application.Grains.Agents.Anonymous)
            ["AnonymousUserGAgent"] = new[] { "AnonymousUserState" },
            // TwitterIdentityBindingGAgent -> TwitterIdentityBindingState (in Aevatar.Application.Grains.Twitter)
            ["TwitterIdentityBindingGAgent"] = new[] { "TwitterIdentityBindingState" },
        };
        
        // Extract base name (e.g., "UserStatisticsGAgent" -> "UserStatistics")
        var baseName = typeName.Replace("GAgent", "").Replace("Agent", "");
        
        // Build state name candidates
        var stateNamesList = new List<string>();
        
        // Add explicit mappings first if available
        if (explicitMappings.TryGetValue(typeName, out var explicitNames))
        {
            stateNamesList.AddRange(explicitNames);
        }
        
        // Add common naming patterns
        stateNamesList.AddRange(new[]
        {
            baseName + "GAgentState",      // UserStatisticsGAgentState (prefer GAgent state)
            baseName + "State",            // UserStatisticsState
            typeName + "State",            // UserStatisticsGAgentState
            baseName,                      // UserStatistics (if State is the class name)
            typeName                       // UserStatisticsGAgent (fallback)
        });
        
        var stateNames = stateNamesList.Distinct().ToArray();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .OrderBy(a =>
            {
                var name = a.GetName().Name ?? string.Empty;
                if (name.Contains("GodGPT.GAgents", StringComparison.OrdinalIgnoreCase)) return 0;
                if (name.Contains("Aevatar.Application.Grains", StringComparison.OrdinalIgnoreCase)) return 1;
                return 2;
            })
            .ToList();
        
        foreach (var stateName in stateNames)
        {
            foreach (var assembly in assemblies)
            {
                try
                {
                    // Try exact match first
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == stateName || t.FullName?.EndsWith($".{stateName}") == true);
                    
                    if (type != null && !type.IsInterface && !type.IsAbstract)
                    {
                        _stateTypeCache[typeName] = type;
                        _logger.LogInformation("Found State type: {Type} for {GrainType} (Assembly={Assembly})", 
                            type.FullName, typeName, assembly.GetName().Name);
                        return type;
                    }
                }
                catch { /* ignore reflection errors */ }
            }
        }
        
        _logger.LogWarning("State type not found for {Type}, will use raw BSON", typeName);
        return null;
    }

    private Dictionary<string, object?> ObjectToDict(object obj)
    {
        var result = new Dictionary<string, object?>();
        var type = obj.GetType();
        
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                var value = prop.GetValue(obj);
                result[prop.Name] = ConvertValue(value);
            }
            catch { /* skip */ }
        }
        
        return result;
    }

    private ExportedStateRecord? TryDeserializeEventSourcingSnapshot(BsonValue innerDoc, Type stateType, string id, string etag, int dataSize)
    {
        try
        {
            var serializerType = _serializer.GetType();
            var deserializeMethodBase = serializerType.GetMethod("Deserialize", new[] { typeof(BsonValue) });
            if (deserializeMethodBase == null)
            {
                return null;
            }

            // Attempt 1: ViewStateSnapshot<TLogView>
            var snapshotType = typeof(ViewStateSnapshot<>).MakeGenericType(stateType);
            var snapshotObj = deserializeMethodBase.MakeGenericMethod(snapshotType)
                .Invoke(_serializer, new object[] { innerDoc });
            var record = TryBuildSnapshotRecord(snapshotType, snapshotObj, id, etag, dataSize, "ViewStateSnapshot");
            if (record != null)
            {
                return record;
            }

            // Attempt 2: ViewStateSnapshotWithMetadata<TLogView>
            var metadataSnapshotType = typeof(ViewStateSnapshotWithMetadata<>).MakeGenericType(stateType);
            var metadataObj = deserializeMethodBase.MakeGenericMethod(metadataSnapshotType)
                .Invoke(_serializer, new object[] { innerDoc });
            return TryBuildSnapshotRecord(metadataSnapshotType, metadataObj, id, etag, dataSize, "ViewStateSnapshotWithMetadata");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EventSourcing snapshot deserialization failed for {Type}", stateType.Name);
            return null;
        }
    }

    private ExportedStateRecord? TryBuildSnapshotRecord(Type snapshotType, object? snapshotObj, string id, string etag, int dataSize, string wrapperName)
    {
        if (snapshotObj == null)
        {
            return null;
        }

        // ViewStateSnapshot<T> has State.Snapshot; ViewStateSnapshotWithMetadata<T> has Snapshot directly
        object? snapshotValue = null;
        object? snapshotVersion = null;
        object? writeVector = null;

        var stateProp = snapshotType.GetProperty("State");
        if (stateProp != null)
        {
            var stateValue = stateProp.GetValue(snapshotObj);
            if (stateValue == null)
            {
                return null;
            }
            var metadataType = stateValue.GetType();
            snapshotValue = metadataType.GetProperty("Snapshot")?.GetValue(stateValue);
            snapshotVersion = metadataType.GetProperty("SnapshotVersion")?.GetValue(stateValue);
            writeVector = metadataType.GetProperty("WriteVector")?.GetValue(stateValue);
        }
        else
        {
            snapshotValue = snapshotType.GetProperty("Snapshot")?.GetValue(snapshotObj);
            snapshotVersion = snapshotType.GetProperty("SnapshotVersion")?.GetValue(snapshotObj);
            writeVector = snapshotType.GetProperty("WriteVector")?.GetValue(snapshotObj);
        }

        if (snapshotValue == null)
        {
            return null;
        }

        var stateDict = ObjectToDict(snapshotValue);
        if (snapshotVersion != null)
        {
            stateDict["_snapshotVersion"] = snapshotVersion;
        }
        if (writeVector != null)
        {
            stateDict["_writeVector"] = writeVector;
        }

        return new ExportedStateRecord
        {
            Id = id,
            ETag = etag,
            State = stateDict
        };
    }

    private object? ConvertValue(object? value)
    {
        if (value == null) return null;
        
        var type = value.GetType();
        
        if (type.IsPrimitive || value is string || value is decimal || 
            value is DateTime || value is Guid || value is DateTimeOffset)
            return value;
        
        if (value is System.Collections.IDictionary dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (System.Collections.DictionaryEntry entry in dict)
                result[entry.Key?.ToString() ?? ""] = ConvertValue(entry.Value);
            return result;
        }
        
        if (value is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(ConvertValue(item));
            return list;
        }
        
        // Complex object
        return ObjectToDict(value);
    }

    private static Dictionary<string, object?> BsonDocumentToDict(BsonDocument doc)
    {
        var result = new Dictionary<string, object?>();
        foreach (var element in doc)
            result[element.Name] = BsonValueToObject(element.Value);
        return result;
    }

    private static object? BsonValueToObject(BsonValue value) => value.BsonType switch
    {
        BsonType.Document => BsonDocumentToDict(value.AsBsonDocument),
        BsonType.Array => value.AsBsonArray.Select(BsonValueToObject).ToList(),
        BsonType.String => value.AsString,
        BsonType.Int32 => value.AsInt32,
        BsonType.Int64 => value.AsInt64,
        BsonType.Double => value.AsDouble,
        BsonType.Boolean => value.AsBoolean,
        BsonType.DateTime => value.ToUniversalTime(),
        BsonType.Null => null,
        BsonType.ObjectId => value.AsObjectId.ToString(),
        BsonType.Binary => new Dictionary<string, object?> { ["_base64"] = Convert.ToBase64String(value.AsBsonBinaryData.Bytes), ["_size"] = value.AsBsonBinaryData.Bytes.Length },
        _ => value.ToString()
    };

    private static string ExtractTypeName(string collectionName)
    {
        var cleaned = collectionName;
        // Handle Stream prefix
        if (cleaned.StartsWith("Streamgodgpt"))
            cleaned = cleaned["Streamgodgpt".Length..];
        else if (cleaned.StartsWith("Stream"))
            cleaned = cleaned["Stream".Length..];
        // Handle Orleansgodgptprod prefix (e.g., OrleansgodgptprodUserPaymentState)
        else if (cleaned.StartsWith("Orleansgodgptprod"))
            cleaned = cleaned["Orleansgodgptprod".Length..];
        
        // If still contains dots, extract last part (namespace.ClassName -> ClassName)
        var parts = cleaned.Split('.');
        return parts.LastOrDefault() ?? collectionName;
    }
}
