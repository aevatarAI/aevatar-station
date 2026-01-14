using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Admin.Services;

/// <summary>
/// Simple state export service - streams data directly without storage
/// </summary>
public class StateExportService : IStateExportService, ISingletonDependency
{
    private readonly ILogger<StateExportService> _logger;
    private readonly IMongoDatabase _database;
    
    // Business state types to export
    private static readonly HashSet<string> BusinessStateTypes = new()
    {
        "UserStatistics", "GodChat", "UserQuota", "UserBilling",
        "Invitation", "InviteCode", "Lumen", "UserInfo",
        "GoogleAuth", "Awakening", "DailyPush", "AnonymousUser",
        "UserFeedback", "FreeTrialCode"
    };

    public StateExportService(
        ILogger<StateExportService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        
        var connectionString = configuration["OrleansEventSourcing:Mongodb:Connection"] 
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["OrleansEventSourcing:Mongodb:Database"] 
            ?? "GodgptDb";
        
        _logger.LogInformation("StateExportService initialized with database: {Database}", databaseName);
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Stream export data directly to response - no storage needed
    /// </summary>
    public async Task StreamExportAsync(Stream outputStream, List<string>? types)
    {
        _logger.LogInformation("Starting streaming export for types: {Types}", 
            types != null ? string.Join(", ", types) : "all");
        
        await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions 
        { 
            Indented = false  // Compact for streaming
        });
        
        writer.WriteStartObject();
        writer.WriteString("exportedAt", DateTime.UtcNow.ToString("O"));
        writer.WritePropertyName("records");
        writer.WriteStartArray();
        
        var totalCount = 0;
        var typesToExport = types?.Count > 0 ? types : BusinessStateTypes.ToList();
        
        // Get all Stream* collections
        var collectionNames = await _database.ListCollectionNamesAsync();
        var streamCollections = (await collectionNames.ToListAsync())
            .Where(c => c.StartsWith("Stream"))
            .ToList();
        
        foreach (var collectionName in streamCollections)
        {
            var matchesType = typesToExport.Any(t => 
                collectionName.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (!matchesType) continue;
            
            var typeName = ExtractTypeName(collectionName);
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            
            using var cursor = await collection.Find(_ => true).ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var doc in cursor.Current)
                {
                    var record = DeserializeDocument(doc, typeName);
                    if (record == null) continue;
                    
                    // Write record directly to stream
                    writer.WriteStartObject();
                    writer.WriteString("id", record.Id);
                    writer.WriteString("type", record.Type);
                    writer.WritePropertyName("state");
                    WriteObjectAsJson(writer, record.State);
                    writer.WriteEndObject();
                    
                    totalCount++;
                    
                    // Flush periodically to keep connection alive
                    if (totalCount % 100 == 0)
                    {
                        await writer.FlushAsync();
                        await outputStream.FlushAsync();
                    }
                }
            }
        }
        
        writer.WriteEndArray();
        writer.WriteNumber("totalCount", totalCount);
        writer.WriteEndObject();
        
        await writer.FlushAsync();
        _logger.LogInformation("Streaming export completed. Total: {Count} records", totalCount);
    }

    /// <summary>
    /// Simple sync export - returns all data directly (for small datasets)
    /// </summary>
    public async Task<ExportResultDto> ExportAllAsync(List<string>? types)
    {
        _logger.LogInformation("Starting sync export for types: {Types}", 
            types != null ? string.Join(", ", types) : "all");
        
        var records = new List<ExportedRecord>();
        var typesToExport = types?.Count > 0 ? types : BusinessStateTypes.ToList();
        
        var collectionNames = await _database.ListCollectionNamesAsync();
        var streamCollections = (await collectionNames.ToListAsync())
            .Where(c => c.StartsWith("Stream"))
            .ToList();
        
        foreach (var collectionName in streamCollections)
        {
            var matchesType = typesToExport.Any(t => 
                collectionName.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (!matchesType) continue;
            
            var typeName = ExtractTypeName(collectionName);
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            
            using var cursor = await collection.Find(_ => true).ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var doc in cursor.Current)
                {
                    var record = DeserializeDocument(doc, typeName);
                    if (record != null) records.Add(record);
                }
            }
        }
        
        _logger.LogInformation("Sync export completed. Total: {Count} records", records.Count);
        
        return new ExportResultDto
        {
            ExportedAt = DateTime.UtcNow,
            TotalCount = records.Count,
            Records = records
        };
    }

    public async Task<List<string>> GetAvailableTypesAsync()
    {
        var types = new List<string>();
        
        try
        {
            var collectionNames = await _database.ListCollectionNamesAsync();
            var streamCollections = await collectionNames.ToListAsync();
            
            foreach (var coll in streamCollections.Where(c => c.StartsWith("Stream")))
            {
                var typeName = ExtractTypeName(coll);
                if (!string.IsNullOrEmpty(typeName) && !types.Contains(typeName))
                    types.Add(typeName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available types");
            return BusinessStateTypes.ToList();
        }
        
        return types.OrderBy(t => t).ToList();
    }

    #region Legacy interface support (not used in new streaming API)
    
    public Task<string> StartExportAsync(List<string>? types) 
        => Task.FromResult("use-streaming-api");
    
    public ExportTask? GetTaskStatus(string taskId) => null;
    
    public List<ExportedRecord>? GetAndRemoveTaskData(string taskId) => null;
    
    #endregion

    #region Helpers
    
    private ExportedRecord? DeserializeDocument(BsonDocument doc, string typeName)
    {
        var id = doc.GetValue("_id", BsonValue.Create("")).AsString;
        
        if (!doc.Contains("_doc"))
        {
            return new ExportedRecord
            {
                Id = id,
                Type = typeName,
                State = BsonDocumentToObject(doc)
            };
        }
        
        var docField = doc["_doc"];
        if (!docField.IsBsonDocument) return null;
        
        var innerDoc = docField.AsBsonDocument;
        if (!innerDoc.Contains("data")) return null;
        
        var dataValue = innerDoc["data"];
        
        if (dataValue.IsBsonDocument)
        {
            return new ExportedRecord
            {
                Id = id,
                Type = typeName,
                State = BsonDocumentToObject(dataValue.AsBsonDocument)
            };
        }
        
        if (dataValue.IsBsonBinaryData)
        {
            return new ExportedRecord
            {
                Id = id,
                Type = typeName,
                State = new { _rawBase64 = Convert.ToBase64String(dataValue.AsBsonBinaryData.Bytes) }
            };
        }
        
        return null;
    }

    private static object BsonDocumentToObject(BsonDocument doc)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var element in doc)
            dict[element.Name] = BsonValueToObject(element.Value);
        return dict;
    }

    private static object? BsonValueToObject(BsonValue value) => value.BsonType switch
    {
        BsonType.Document => BsonDocumentToObject(value.AsBsonDocument),
        BsonType.Array => value.AsBsonArray.Select(BsonValueToObject).ToList(),
        BsonType.String => value.AsString,
        BsonType.Int32 => value.AsInt32,
        BsonType.Int64 => value.AsInt64,
        BsonType.Double => value.AsDouble,
        BsonType.Boolean => value.AsBoolean,
        BsonType.DateTime => value.ToUniversalTime(),
        BsonType.Null => null,
        BsonType.ObjectId => value.AsObjectId.ToString(),
        BsonType.Binary => Convert.ToBase64String(value.AsBsonBinaryData.Bytes),
        _ => value.ToString()
    };

    private static void WriteObjectAsJson(Utf8JsonWriter writer, object? obj)
    {
        switch (obj)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToString("O"));
                break;
            case Dictionary<string, object?> dict:
                writer.WriteStartObject();
                foreach (var kv in dict)
                {
                    writer.WritePropertyName(kv.Key);
                    WriteObjectAsJson(writer, kv.Value);
                }
                writer.WriteEndObject();
                break;
            case IEnumerable<object?> list:
                writer.WriteStartArray();
                foreach (var item in list)
                    WriteObjectAsJson(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(obj.ToString());
                break;
        }
    }

    private static string ExtractTypeName(string collectionName)
    {
        var cleaned = collectionName;
        if (cleaned.StartsWith("Streamgodgpt"))
            cleaned = cleaned["Streamgodgpt".Length..];
        else if (cleaned.StartsWith("Stream"))
            cleaned = cleaned["Stream".Length..];
        
        var parts = cleaned.Split('.');
        return parts.LastOrDefault() ?? collectionName;
    }
    
    #endregion
}

/// <summary>
/// Direct export result (for sync API)
/// </summary>
public class ExportResultDto
{
    public DateTime ExportedAt { get; set; }
    public int TotalCount { get; set; }
    public List<ExportedRecord> Records { get; set; } = new();
}
