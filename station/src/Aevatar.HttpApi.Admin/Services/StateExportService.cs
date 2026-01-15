using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Serialization;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Admin.Services;

/// <summary>
/// Simple state export service - paged queries, no long connections
/// Deserializes Orleans binary state to readable JSON using Orleans Serializer
/// </summary>
public class StateExportService : ISingletonDependency
{
    private readonly ILogger<StateExportService> _logger;
    private readonly IMongoDatabase _database;
    private readonly Serializer _orleansSerializer;
    
    private static readonly HashSet<string> BusinessStateTypes = new()
    {
        "UserStatistics", "GodChat", "UserQuota", "UserBilling",
        "Invitation", "InviteCode", "Lumen", "UserInfo",
        "GoogleAuth", "Awakening", "DailyPush", "AnonymousUser",
        "UserFeedback", "FreeTrialCode"
    };

    public StateExportService(
        ILogger<StateExportService> logger,
        IConfiguration configuration,
        Serializer orleansSerializer)
    {
        _logger = logger;
        _orleansSerializer = orleansSerializer;
        
        // Read from StateExport config (for admin export API)
        var connectionString = configuration["StateExport:ConnectionString"] 
            ?? configuration["Orleans:MongoDBClient"]
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["StateExport:Database"] 
            ?? configuration["Orleans:DataBase"]
            ?? "AevatarDb";
        
        _logger.LogInformation("StateExportService initialized: {Database} from {Connection}", 
            databaseName, connectionString.Split('@').LastOrDefault());
        
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
            // Try to deserialize using Orleans serializer with dynamic type
            var bytes = dataValue.AsBsonBinaryData.Bytes;
            var parsedState = ParseOrleansBinaryState(bytes, typeName);
            
            return new ExportedRecord
            {
                Id = id,
                Type = typeName,
                State = parsedState
            };
        }
        
        return null;
    }

    /// <summary>
    /// Parse Orleans binary state using Orleans Serializer with dynamic type loading
    /// </summary>
    private object ParseOrleansBinaryState(byte[] bytes, string? stateTypeName = null)
    {
        // Try to find and load the State type dynamically
        if (!string.IsNullOrEmpty(stateTypeName))
        {
            var stateType = FindStateType(stateTypeName);
            if (stateType != null)
            {
                try
                {
                    // Use reflection to call Deserialize<T> with the actual type
                    var deserializeMethod = typeof(Serializer)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "Deserialize" && 
                                            m.IsGenericMethod && 
                                            m.GetParameters().Length == 1 &&
                                            m.GetParameters()[0].ParameterType == typeof(byte[]));
                    
                    if (deserializeMethod != null)
                    {
                        var genericMethod = deserializeMethod.MakeGenericMethod(stateType);
                        var deserialized = genericMethod.Invoke(_orleansSerializer, new object[] { bytes });
                        
                        if (deserialized != null)
                        {
                            _logger.LogInformation("Successfully deserialized State type: {Type}", stateType.Name);
                            return ObjectToDictionary(deserialized);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dynamic type deserialization failed for {Type}", stateTypeName);
                }
            }
        }
        
        // Fallback: extract strings from binary
        return ExtractStringsFromBinaryFallback(bytes);
    }
    
    /// <summary>
    /// Find State type by name from loaded assemblies
    /// </summary>
    private Type? FindStateType(string typeName)
    {
        // Try common State naming patterns
        var stateNames = new[]
        {
            typeName.Replace("GAgent", "State"),  // GodChatGAgent -> GodChatState
            typeName + "State",
            typeName
        };
        
        foreach (var stateName in stateNames)
        {
            // Search in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == stateName || 
                                            t.FullName?.EndsWith("." + stateName) == true);
                    if (type != null)
                    {
                        _logger.LogDebug("Found State type: {Type} in assembly {Assembly}", 
                            type.FullName, assembly.GetName().Name);
                        return type;
                    }
                }
                catch
                {
                    // Ignore assembly loading errors
                }
            }
        }
        
        _logger.LogDebug("State type not found: {TypeName}", typeName);
        return null;
    }
    
    /// <summary>
    /// Convert deserialized object to dictionary using reflection
    /// </summary>
    private object ObjectToDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, object?>();
        
        var type = obj.GetType();
        
        // Handle primitive types and strings
        if (type.IsPrimitive || obj is string || obj is decimal || obj is DateTime || obj is Guid)
            return obj;
        
        // Handle arrays and lists
        if (obj is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(item != null ? ObjectToDictionary(item) : null);
            return list;
        }
        
        // Handle dictionary types
        if (obj is System.Collections.IDictionary dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (System.Collections.DictionaryEntry entry in dict)
                result[entry.Key?.ToString() ?? "null"] = entry.Value != null ? ObjectToDictionary(entry.Value) : null;
            return result;
        }
        
        // Handle complex objects - use reflection
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var objectDict = new Dictionary<string, object?>();
        
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    objectDict[prop.Name] = ObjectToDictionary(value);
                }
            }
            catch
            {
                // Skip properties that throw exceptions
            }
        }
        
        // If no properties were serialized, return type name as string
        if (objectDict.Count == 0)
            return new { _type = type.Name, _value = obj.ToString() };
        
        return objectDict;
    }
    
    /// <summary>
    /// Fallback: Extract readable strings and parse config from binary data
    /// </summary>
    private object ExtractStringsFromBinaryFallback(byte[] bytes)
    {
        var result = new Dictionary<string, object>();
        var strings = ExtractAllStrings(bytes);
        
        // Parse configuration values
        var config = new Dictionary<string, object>();
        
        foreach (var str in strings)
        {
            // Model names
            if (str.StartsWith("gpt-") || str.StartsWith("claude-") || str.StartsWith("deepseek"))
            {
                config["model"] = str.Split(new[] { 'A', '\0' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? str;
            }
            // Endpoints
            else if (str.Contains("azure.com") || str.Contains("openai.com") || str.Contains("api."))
            {
                var endpoint = ExtractUrl(str);
                if (!string.IsNullOrEmpty(endpoint))
                    config["endpoint"] = endpoint;
            }
            // API Keys (long alphanumeric strings)
            else if (str.Length >= 40 && str.Length <= 200 && IsApiKeyLike(str))
            {
                config["apiKey"] = MaskApiKey(str);
                config["apiKeyRaw"] = str; // Full key for migration
            }
            // Provider
            else if (str == "OpenAI" || str == "AzureOpenAI" || str == "DeepSeek")
            {
                config["provider"] = str;
            }
            // Region
            else if (str == "DEFAULT" || str == "CONSOLE" || str.Contains("REGION"))
            {
                config["region"] = str.Trim();
            }
            // Chat Manager Guid (UUID pattern)
            else if (IsGuidString(str))
            {
                if (!config.ContainsKey("chatManagerId"))
                    config["chatManagerId"] = str;
            }
        }
        
        if (config.Count > 0)
            result["config"] = config;
        
        // Also include raw strings for debugging
        var otherStrings = strings
            .Where(s => !s.Contains("Aevatar.") && !s.Contains("Orleans.") && 
                        !s.Contains("System.") && !s.Contains("[[") && !s.Contains("Version=") &&
                        !s.Contains("GAgent") && !s.Contains("Event") && s.Length < 100)
            .Distinct()
            .Take(20)
            .ToList();
        
        if (otherStrings.Any())
            result["otherData"] = otherStrings;
        
        result["_byteLength"] = bytes.Length;
        result["_rawBase64"] = Convert.ToBase64String(bytes);
        
        return result;
    }
    
    private List<string> ExtractAllStrings(byte[] bytes)
    {
        var strings = new List<string>();
        var currentString = new StringBuilder();
        
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b >= 32 && b < 127)
            {
                currentString.Append((char)b);
            }
            else
            {
                if (currentString.Length >= 3)
                    strings.Add(currentString.ToString());
                currentString.Clear();
            }
        }
        
        if (currentString.Length >= 3)
            strings.Add(currentString.ToString());
        
        return strings;
    }
    
    private string ExtractUrl(string str)
    {
        var httpIndex = str.IndexOf("http");
        if (httpIndex >= 0)
        {
            var end = str.IndexOfAny(new[] { ' ', '\0', 'A', '@' }, httpIndex);
            return end > httpIndex ? str[httpIndex..end] : str[httpIndex..];
        }
        return str;
    }
    
    private bool IsApiKeyLike(string str)
    {
        // API keys are usually alphanumeric with some special chars
        return str.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '=');
    }
    
    private bool IsGuidString(string str)
    {
        return Guid.TryParse(str, out _);
    }
    
    private string MaskApiKey(string key)
    {
        if (key.Length <= 8) return "***";
        return key[..4] + "****" + key[^4..];
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
