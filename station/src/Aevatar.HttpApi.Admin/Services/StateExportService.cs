using System;
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
/// Simple state export service - paged queries, no long connections
/// </summary>
public class StateExportService : ISingletonDependency
{
    private readonly ILogger<StateExportService> _logger;
    private readonly IMongoDatabase _database;
    
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
        
        _logger.LogInformation("StateExportService initialized: {Database}", databaseName);
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Get export summary - total counts per type (for planning pagination)
    /// </summary>
    public async Task<ExportSummaryDto> GetExportSummaryAsync(List<string>? types)
    {
        var summary = new ExportSummaryDto { Types = new List<TypeSummary>() };
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
            var count = await collection.CountDocumentsAsync(_ => true);
            
            summary.Types.Add(new TypeSummary 
            { 
                TypeName = typeName, 
                CollectionName = collectionName,
                Count = (int)count 
            });
            summary.TotalCount += (int)count;
        }
        
        return summary;
    }

    /// <summary>
    /// Export single type with pagination - short request, no timeout
    /// </summary>
    public async Task<PagedExportDto> ExportTypePagedAsync(
        string collectionName, 
        int skip = 0, 
        int limit = 1000)
    {
        _logger.LogInformation("Exporting {Collection} skip={Skip} limit={Limit}", 
            collectionName, skip, limit);
        
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var typeName = ExtractTypeName(collectionName);
        
        var totalCount = await collection.CountDocumentsAsync(_ => true);
        var records = new List<ExportedRecord>();
        
        var documents = await collection.Find(_ => true)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
        
        foreach (var doc in documents)
        {
            var record = DeserializeDocument(doc, typeName);
            if (record != null) records.Add(record);
        }
        
        return new PagedExportDto
        {
            TypeName = typeName,
            CollectionName = collectionName,
            Skip = skip,
            Limit = limit,
            TotalCount = (int)totalCount,
            ReturnedCount = records.Count,
            HasMore = skip + records.Count < totalCount,
            Records = records
        };
    }

    /// <summary>
    /// Get available state types
    /// </summary>
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

#region DTOs

public class ExportSummaryDto
{
    public int TotalCount { get; set; }
    public List<TypeSummary> Types { get; set; } = new();
}

public class TypeSummary
{
    public string TypeName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PagedExportDto
{
    public string TypeName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int Skip { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int ReturnedCount { get; set; }
    public bool HasMore { get; set; }
    public List<ExportedRecord> Records { get; set; } = new();
}

#endregion
