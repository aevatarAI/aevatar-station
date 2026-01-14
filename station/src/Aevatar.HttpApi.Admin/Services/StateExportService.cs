using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Admin.Services;

/// <summary>
/// Service for exporting GAgent state data from MongoDB
/// </summary>
public class StateExportService : IStateExportService, ISingletonDependency
{
    private readonly ILogger<StateExportService> _logger;
    private readonly IMongoDatabase _database;
    
    // In-memory storage for export tasks (consider Redis for production)
    private static readonly ConcurrentDictionary<string, ExportTask> Tasks = new();
    
    // Business state types to export (excludes Orleans internal types)
    private static readonly HashSet<string> BusinessStateTypes = new()
    {
        "UserStatistics",
        "GodChat",
        "UserQuota",
        "UserBilling",
        "Invitation",
        "InviteCode",
        "Lumen",
        "UserInfo",
        "GoogleAuth",
        "Awakening",
        "DailyPush",
        "AnonymousUser",
        "UserFeedback",
        "FreeTrialCode"
    };

    public StateExportService(
        ILogger<StateExportService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        
        // Read MongoDB connection from configuration
        var connectionString = configuration["OrleansEventSourcing:Mongodb:Connection"];
        var databaseName = configuration["OrleansEventSourcing:Mongodb:Database"];
        
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
        {
            _logger.LogWarning("MongoDB configuration not found, using default values");
            connectionString = "mongodb://localhost:27017";
            databaseName = "GodgptDb";
        }
        
        _logger.LogInformation("StateExportService initializing with database: {Database}", databaseName);
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async Task<string> StartExportAsync(List<string>? types)
    {
        var taskId = Guid.NewGuid().ToString("N")[..8];
        var task = new ExportTask
        {
            TaskId = taskId,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            Data = new List<ExportedRecord>()
        };
        
        Tasks[taskId] = task;
        
        _logger.LogInformation("Starting export task {TaskId} for types: {Types}", 
            taskId, types != null ? string.Join(", ", types) : "all");
        
        // Execute export in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteExportAsync(task, types);
                task.Status = "completed";
                task.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Export task {TaskId} completed. Total: {Count} records", 
                    taskId, task.TotalCount);
            }
            catch (Exception ex)
            {
                task.Status = "failed";
                task.Error = ex.Message;
                task.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Export task {TaskId} failed", taskId);
            }
        });
        
        return taskId;
    }

    public ExportTask? GetTaskStatus(string taskId)
    {
        return Tasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public List<ExportedRecord>? GetAndRemoveTaskData(string taskId)
    {
        if (Tasks.TryRemove(taskId, out var task) && task.Status == "completed")
        {
            return task.Data;
        }
        return null;
    }

    public async Task<List<string>> GetAvailableTypesAsync()
    {
        var types = new List<string>();
        
        try
        {
            var collectionNames = await _database.ListCollectionNamesAsync();
            var streamCollections = await collectionNames.ToListAsync();
            
            foreach (var collection in streamCollections)
            {
                if (collection.StartsWith("Stream"))
                {
                    var typeName = ExtractTypeName(collection);
                    if (!string.IsNullOrEmpty(typeName) && !types.Contains(typeName))
                    {
                        types.Add(typeName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available types from database");
            // Return default business types as fallback
            return BusinessStateTypes.ToList();
        }
        
        return types.OrderBy(t => t).ToList();
    }

    private async Task ExecuteExportAsync(ExportTask task, List<string>? requestedTypes)
    {
        // Get all Stream* collections
        var collectionNames = await _database.ListCollectionNamesAsync();
        var streamCollections = await collectionNames.ToListAsync();
        streamCollections = streamCollections
            .Where(c => c.StartsWith("Stream"))
            .ToList();
        
        _logger.LogInformation("Found {Count} stream collections to scan", streamCollections.Count);
        
        // Filter types to export
        var typesToExport = requestedTypes?.Count > 0 
            ? requestedTypes 
            : BusinessStateTypes.ToList();
        
        foreach (var collectionName in streamCollections)
        {
            // Check if this collection matches requested types
            var matchesType = typesToExport.Any(t => 
                collectionName.Contains(t, StringComparison.OrdinalIgnoreCase));
            
            if (!matchesType) continue;
            
            await ExportCollectionAsync(task, collectionName);
        }
        
        task.TotalCount = task.Data!.Count;
    }

    private async Task ExportCollectionAsync(ExportTask task, string collectionName)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var typeName = ExtractTypeName(collectionName);
        
        _logger.LogInformation("Exporting collection: {CollectionName}", collectionName);
        
        try
        {
            using var cursor = await collection.Find(_ => true).ToCursorAsync();
            
            while (await cursor.MoveNextAsync())
            {
                foreach (var doc in cursor.Current)
                {
                    try
                    {
                        var record = DeserializeDocument(doc, typeName);
                        if (record != null)
                        {
                            task.Data!.Add(record);
                            task.ProcessedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize document {Id}", 
                            doc.GetValue("_id", BsonValue.Create("unknown")));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting collection {CollectionName}", collectionName);
        }
    }

    private ExportedRecord? DeserializeDocument(BsonDocument doc, string typeName)
    {
        var id = doc.GetValue("_id", BsonValue.Create("")).AsString;
        
        // Try to get state data from document
        if (!doc.Contains("_doc"))
        {
            // Direct state format
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
        
        try
        {
            // Return as BSON document
            if (dataValue.IsBsonDocument)
            {
                return new ExportedRecord
                {
                    Id = id,
                    Type = typeName,
                    State = BsonDocumentToObject(dataValue.AsBsonDocument)
                };
            }
            
            // Return Base64 encoded raw bytes
            if (dataValue.IsBsonBinaryData)
            {
                return new ExportedRecord
                {
                    Id = id,
                    Type = typeName,
                    State = new
                    {
                        _rawBase64 = Convert.ToBase64String(dataValue.AsBsonBinaryData.Bytes),
                        _note = "Raw binary data, deserialization not available"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Deserialize failed for {Id}, returning raw data", id);
        }
        
        return null;
    }

    private static object BsonDocumentToObject(BsonDocument doc)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var element in doc)
        {
            dict[element.Name] = BsonValueToObject(element.Value);
        }
        return dict;
    }

    private static object? BsonValueToObject(BsonValue value)
    {
        return value.BsonType switch
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
    }

    private static string ExtractTypeName(string collectionName)
    {
        // StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent
        // → UserStatisticsGAgent
        var cleaned = collectionName;
        
        // Remove common prefixes
        if (cleaned.StartsWith("Streamgodgpt"))
            cleaned = cleaned.Substring("Streamgodgpt".Length);
        else if (cleaned.StartsWith("Stream"))
            cleaned = cleaned.Substring("Stream".Length);
        
        // Get the last part after dots
        var parts = cleaned.Split('.');
        return parts.LastOrDefault() ?? collectionName;
    }
}
