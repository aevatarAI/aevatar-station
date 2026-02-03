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
    private readonly Orleans.Serialization.Serializer _orleansSerializer;
    private readonly IMongoDatabase _database;
    private readonly Dictionary<string, Type> _stateTypeCache = new();

    public StateExportGrain(
        ILogger<StateExportGrain> logger,
        IGrainStateSerializer serializer,
        Orleans.Serialization.Serializer orleansSerializer,
        IConfiguration configuration)
    {
        _logger = logger;
        _serializer = serializer;
        _orleansSerializer = orleansSerializer;
        
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
    
    /// <summary>
    /// Cleans MongoDB connection string by removing incompatible parameters
    /// </summary>
    private static string CleanConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;
        
        try
        {
            var uri = new Uri(connectionString);
            var host = uri.Host;
            
            // Check if connection string contains multiple hosts (comma-separated)
            // Extract hosts from the connection string
            var hostPart = connectionString.Split('@').LastOrDefault()?.Split('/').FirstOrDefault();
            var hasMultipleHosts = !string.IsNullOrEmpty(hostPart) && hostPart.Contains(',');
            
            // If multiple hosts are present and directConnection=true exists, remove it
            if (hasMultipleHosts && connectionString.Contains("directConnection=true", StringComparison.OrdinalIgnoreCase))
            {
                // Remove directConnection=true parameter
                connectionString = System.Text.RegularExpressions.Regex.Replace(
                    connectionString,
                    @"[&?]directConnection=true",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Clean up double ampersands or question marks
                connectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"[&]{2,}", "&");
                connectionString = connectionString.Replace("?&", "?").TrimEnd('&', '?');
            }
            
            return connectionString;
        }
        catch
        {
            // If parsing fails, try simple string replacement as fallback
            if (connectionString.Contains(',') && connectionString.Contains("directConnection=true", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = System.Text.RegularExpressions.Regex.Replace(
                    connectionString,
                    @"[&?]directConnection=true",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                connectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"[&]{2,}", "&");
                connectionString = connectionString.Replace("?&", "?").TrimEnd('&', '?');
            }
            return connectionString;
        }
    }

    public async Task<List<StateCollectionInfo>> GetCollectionsAsync()
    {
        var result = new List<StateCollectionInfo>();
        
        var collectionNames = await _database.ListCollectionNamesAsync();
        var names = await collectionNames.ToListAsync();
        
        // Support "Stream" and "Orleansgodgptprod" prefixed collections
        foreach (var name in names.Where(n => n.StartsWith("Stream") || n.StartsWith("Orleansgodgptprod")))
        {
            var collection = _database.GetCollection<BsonDocument>(name);
            var count = await collection.CountDocumentsAsync(_ => true);
            
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
        
        // Use estimated count for better performance (doesn't require full scan)
        var totalCount = await collection.EstimatedDocumentCountAsync();
        _logger.LogInformation("Collection {Collection} has approximately {Count} documents", collectionName, totalCount);
        
        // Fetch documents with projection to only get _id and _doc fields (more efficient)
        var documents = await collection.Find(_ => true)
            .Project(Builders<BsonDocument>.Projection.Include("_id").Include("_doc").Include("_etag"))
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
        
        _logger.LogInformation("Fetched {Count} documents from MongoDB", documents.Count);
        
        // Resolve state type and determine storage format
        var (stateType, isEventSourcingSnapshot) = ResolveStateContext(collectionName, typeName);
        
        var records = new List<ExportedStateRecord>();
        var successCount = 0;
        var errorCount = 0;
        
        // Process documents in batches to avoid memory issues
        foreach (var doc in documents)
        {
            try
            {
                var record = DeserializeDocument(doc, typeName, stateType, isEventSourcingSnapshot);
                if (record != null)
                {
                    records.Add(record);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                var docId = doc.GetValue("_id", "unknown").ToString();
                _logger.LogWarning(ex, "Failed to deserialize document {Id} from {Collection}", docId, collectionName);
                
                // Add error record for debugging
                records.Add(new ExportedStateRecord
                {
                    Id = docId,
                    ETag = doc.GetValue("_etag", "").AsString,
                    State = new Dictionary<string, object?>
                    {
                        ["_error"] = ex.Message,
                        ["_raw"] = "Deserialization failed"
                    }
                });
            }
        }
        
        _logger.LogInformation("Exported {Success}/{Total} records from {Collection} (errors: {Errors})", 
            successCount, records.Count, collectionName, errorCount);
        
        return new StateExportResult
        {
            CollectionName = collectionName,
            TypeName = typeName,
            Skip = skip,
            Limit = limit,
            TotalCount = totalCount,
            HasMore = skip + records.Count < totalCount,
            Records = records
        };
    }

    public async Task<StateExportResult> ExportByIdAsync(string collectionName, string id)
    {
        _logger.LogInformation("Exporting single record: {Collection} id={Id}", collectionName, id);
        
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var typeName = ExtractTypeName(collectionName);
        
        // Build filter for ID - try multiple formats
        var filters = new List<FilterDefinition<BsonDocument>>
        {
            Builders<BsonDocument>.Filter.Eq("_id", id)
        };
        
        // Try GUID format variations
        if (Guid.TryParse(id, out var guid))
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("_id", guid.ToString("D")));  // With dashes
            filters.Add(Builders<BsonDocument>.Filter.Eq("_id", guid.ToString("N")));  // Without dashes
            
            // Also try full GrainId format: "TypeName/Guid"
            filters.Add(Builders<BsonDocument>.Filter.Regex("_id", $".*/{guid:D}$"));
            filters.Add(Builders<BsonDocument>.Filter.Regex("_id", $".*/{guid:N}$"));
        }
        
        var filter = Builders<BsonDocument>.Filter.Or(filters);
        
        var document = await collection.Find(filter)
            .Project(Builders<BsonDocument>.Projection.Include("_id").Include("_doc").Include("_etag"))
            .FirstOrDefaultAsync();
        
        if (document == null)
        {
            _logger.LogWarning("Record not found: {Collection} id={Id}", collectionName, id);
            return new StateExportResult
            {
                CollectionName = collectionName,
                TypeName = typeName,
                Skip = 0,
                Limit = 1,
                TotalCount = 0,
                HasMore = false,
                Records = new List<ExportedStateRecord>()
            };
        }
        
        // Resolve state type and determine storage format
        var (stateType, isEventSourcingSnapshot) = ResolveStateContext(collectionName, typeName);
        
        var records = new List<ExportedStateRecord>();
        try
        {
            var record = DeserializeDocument(document, typeName, stateType, isEventSourcingSnapshot);
            if (record != null)
                records.Add(record);
        }
        catch (Exception ex)
        {
            var docId = document.GetValue("_id", "unknown").ToString();
            _logger.LogWarning(ex, "Failed to deserialize document {Id} from {Collection}", docId, collectionName);
            records.Add(new ExportedStateRecord
            {
                Id = docId,
                ETag = document.GetValue("_etag", "").AsString,
                State = new Dictionary<string, object?>
                {
                    ["_error"] = ex.Message,
                    ["_raw"] = "Deserialization failed"
                }
            });
        }
        
        return new StateExportResult
        {
            CollectionName = collectionName,
            TypeName = typeName,
            Skip = 0,
            Limit = 1,
            TotalCount = records.Count,
            HasMore = false,
            Records = records
        };
    }

    private ExportedStateRecord? DeserializeDocument(BsonDocument doc, string typeName, Type? stateType, bool isEventSourcingSnapshot = false)
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
        
        // Use IGrainStateSerializer to deserialize if we have the State type
        if (stateType != null)
        {
            try
            {
                // Log raw BSON structure for debugging
                var hasData = docContent.Contains("data");
                var bsonKeys = string.Join(", ", docContent.Elements.Select(e => e.Name));
                var dataSize = 0;
                if (hasData && docContent["data"].IsBsonBinaryData)
                {
                    dataSize = docContent["data"].AsBsonBinaryData.Bytes.Length;
                }
                
                _logger.LogInformation("🔍 Deserializing {Type}: HasData={HasData}, DataSize={DataSize} bytes, BSON keys: {Keys}, IsEventSourcing={IsES}", 
                    stateType.Name, hasData, dataSize, bsonKeys, isEventSourcingSnapshot);
                
                object? state = null;
                
                if (isEventSourcingSnapshot)
                {
                    // For EventSourcing snapshots, deserialize as ViewStateSnapshot<T>
                    state = TryDeserializeEventSourcingSnapshot(innerDoc, stateType);
                }
                else
                {
                    // For Orleans grain state, deserialize directly as State type
                    var deserializeMethod = _serializer.GetType()
                        .GetMethod("Deserialize", new[] { typeof(BsonValue) })!
                        .MakeGenericMethod(stateType);
                    
                    state = deserializeMethod.Invoke(_serializer, new object[] { innerDoc });
                }
                
                if (state != null)
                {
                    var stateDict = ObjectToDict(state);
                    
                    // Find fields with actual values (not default/null)
                    var nonDefaultFields = stateDict
                        .Where(kvp => {
                            var v = kvp.Value;
                            if (v == null) return false;
                            if (v is string s && string.IsNullOrEmpty(s)) return false;
                            if (v is bool b && !b) return false;
                            if (v is int i && i == 0) return false;
                            if (v is Guid g && g == Guid.Empty) return false;
                            if (v is DateTime dt && dt == default(DateTime)) return false;
                            if (v is Dictionary<string, object?> dict && dict.Count == 0) return false;
                            return true;
                        })
                        .Select(kvp => $"{kvp.Key}={kvp.Value}")
                        .Take(10)
                        .ToList();
                    
                    var nonNullFields = nonDefaultFields.Count;
                    
                    _logger.LogInformation("✅ Deserialized {Type}: {NonNullFields}/{TotalFields} non-default fields. Sample: {Sample}", 
                        stateType.Name, nonNullFields, stateDict.Count, 
                        string.Join(", ", nonDefaultFields));
                    
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
                _logger.LogWarning(ex, "IGrainStateSerializer deserialization failed for {Type}, falling back to raw BSON", typeName);
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
    /// Deserialize EventSourcing snapshot format: ViewStateSnapshot&lt;T&gt; → extract Snapshot
    /// </summary>
    private object? TryDeserializeEventSourcingSnapshot(BsonValue innerDoc, Type stateType)
    {
        _logger.LogInformation("🔧 TryDeserializeEventSourcingSnapshot: stateType={StateType}", stateType.FullName);
        
        // For EventSourcing (Stream collections), prioritize ViewStateSnapshotWithMetadata format
        // Strategy 1: Try ViewStateSnapshotWithMetadata<T> directly (EventSourcing internal format)
        try
        {
            var metadataType = typeof(ViewStateSnapshotWithMetadata<>).MakeGenericType(stateType);
            _logger.LogInformation("🔧 Trying ViewStateSnapshotWithMetadata<{Type}>", stateType.Name);
            
            var deserializeMethod = _serializer.GetType()
                .GetMethod("Deserialize", new[] { typeof(BsonValue) })!
                .MakeGenericMethod(metadataType);
            
            var metadataObj = deserializeMethod.Invoke(_serializer, new object[] { innerDoc });
            
            if (metadataObj != null)
            {
                var snapshotProp = metadataType.GetProperty("Snapshot");
                var snapshot = snapshotProp?.GetValue(metadataObj);
                var versionProp = metadataType.GetProperty("SnapshotVersion");
                var version = versionProp?.GetValue(metadataObj);
                var writeVectorProp = metadataType.GetProperty("WriteVector");
                var writeVector = writeVectorProp?.GetValue(metadataObj);
                
                _logger.LogInformation("🔧 ViewStateSnapshotWithMetadata: Snapshot={HasSnapshot}, Version={Version}, WriteVector={WriteVector}", 
                    snapshot != null, version, writeVector);
                
                if (snapshot != null)
                {
                    _logger.LogInformation("✅ ViewStateSnapshotWithMetadata deserialized successfully for {Type}", stateType.Name);
                    return snapshot;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("🔧 ViewStateSnapshotWithMetadata failed for {Type}: {Error}", stateType.Name, ex.Message);
        }
        
        // Strategy 3: Try ViewStateSnapshot<T> wrapper (full EventSourcing format)
        try
        {
            var viewStateSnapshotType = typeof(ViewStateSnapshot<>).MakeGenericType(stateType);
            _logger.LogInformation("🔧 Trying ViewStateSnapshot<{Type}>", stateType.Name);
            
            var deserializeMethod = _serializer.GetType()
                .GetMethod("Deserialize", new[] { typeof(BsonValue) })!
                .MakeGenericMethod(viewStateSnapshotType);
            
            var viewStateSnapshot = deserializeMethod.Invoke(_serializer, new object[] { innerDoc });
            
            if (viewStateSnapshot != null)
            {
                var stateProp = viewStateSnapshotType.GetProperty("State");
                var metadataObj = stateProp?.GetValue(viewStateSnapshot);
                
                _logger.LogInformation("🔧 ViewStateSnapshot.State: {HasState}", metadataObj != null);
                
                if (metadataObj != null)
                {
                    var snapshotProp = metadataObj.GetType().GetProperty("Snapshot");
                    var snapshot = snapshotProp?.GetValue(metadataObj);
                    
                    if (snapshot != null)
                    {
                        _logger.LogInformation("✅ ViewStateSnapshot deserialized successfully for {Type}", stateType.Name);
                        return snapshot;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ ViewStateSnapshot deserialization also failed for {Type}: {Error}", stateType.Name, ex.Message);
        }
        
        // Strategy 4: Last resort - try direct deserialization as State type
        try
        {
            _logger.LogInformation("🔧 Trying direct deserialization for {Type}", stateType.Name);
            var deserializeMethod = _serializer.GetType()
                .GetMethod("Deserialize", new[] { typeof(BsonValue) })!
                .MakeGenericMethod(stateType);
            
            var directResult = deserializeMethod.Invoke(_serializer, new object[] { innerDoc });
            
            if (directResult != null)
            {
                var stateDict = ObjectToDict(directResult);
                var nonDefaultCount = stateDict.Count(kvp => {
                    var v = kvp.Value;
                    if (v == null) return false;
                    if (v is string s && string.IsNullOrEmpty(s)) return false;
                    if (v is int i && i == 0) return false;
                    if (v is Dictionary<string, object?> dict && dict.Count == 0) return false;
                    if (v is List<object?> list && list.Count == 0) return false;
                    return true;
                });
                
                if (nonDefaultCount > 0)
                {
                    _logger.LogInformation("✅ Direct deserialization succeeded for {Type}: {NonDefault} non-default fields", 
                        stateType.Name, nonDefaultCount);
                    return directResult;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Direct deserialization also failed for {Type}: {Error}", stateType.Name, ex.Message);
        }
        
        return null;
    }

    /// <summary>
    /// Resolves the state type and determines storage format for a collection.
    /// </summary>
    private (Type? stateType, bool isEventSourcingSnapshot) ResolveStateContext(string collectionName, string typeName)
    {
        var stateType = FindStateType(typeName);
        var isEventSourcingSnapshot = collectionName.StartsWith("Stream", StringComparison.Ordinal);
        
        _logger.LogInformation("ResolveStateContext: collection={Collection}, typeName={TypeName}, stateType={StateType}, isEventSourcing={IsES}",
            collectionName, typeName, stateType?.FullName ?? "NULL", isEventSourcingSnapshot);
        
        return (stateType, isEventSourcingSnapshot);
    }

    private Type? FindStateType(string typeName)
    {
        if (_stateTypeCache.TryGetValue(typeName, out var cached))
            return cached;
        
        // Extract base name (e.g., "UserStatisticsGAgent" -> "UserStatistics")
        var baseName = typeName.Replace("GAgent", "").Replace("Agent", "");
        
        // Try common naming patterns for State types
        var stateNames = new[]
        {
            baseName + "State",           // UserStatisticsState
            baseName + "GAgentState",      // UserStatisticsGAgentState
            typeName + "State",            // UserStatisticsGAgentState
            baseName,                      // UserStatistics (if State is the class name)
            typeName                       // UserStatisticsGAgent (fallback)
        };
        
        foreach (var stateName in stateNames)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Try exact match first
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == stateName || t.FullName?.EndsWith($".{stateName}") == true);
                    
                    if (type != null && !type.IsInterface && !type.IsAbstract)
                    {
                        _stateTypeCache[typeName] = type;
                        _logger.LogInformation("Found State type: {Type} for {GrainType}", type.FullName, typeName);
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
        BsonType.Binary => new Dictionary<string, string> { ["_base64"] = Convert.ToBase64String(value.AsBsonBinaryData.Bytes) },
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
