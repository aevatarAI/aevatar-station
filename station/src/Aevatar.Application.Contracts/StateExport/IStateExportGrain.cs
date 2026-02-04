using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace Aevatar.StateExport;

/// <summary>
/// Grain interface for exporting State data with proper Orleans deserialization
/// </summary>
public interface IStateExportGrain : IGrainWithStringKey
{
    /// <summary>
    /// Get available State collections
    /// </summary>
    Task<List<StateCollectionInfo>> GetCollectionsAsync();
    
    /// <summary>
    /// Export State data from a collection with pagination (direct MongoDB read + HybridGrainStateSerializer)
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="skip">Skip count (for offset-based pagination, used when cursor is null)</param>
    /// <param name="limit">Page size</param>
    /// <param name="cursor">Optional cursor for cursor-based pagination (use _id from previous response, faster for large offsets)</param>
    Task<StateExportResult> ExportAsync(string collectionName, int skip, int limit, string? cursor = null);
    
    /// <summary>
    /// Export a single record by ID
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="id">Record ID (MongoDB _id field value)</param>
    Task<StateExportResult> ExportByIdAsync(string collectionName, string id);
}

[GenerateSerializer]
public class StateCollectionInfo
{
    [Id(0)] public string CollectionName { get; set; } = string.Empty;
    [Id(1)] public string TypeName { get; set; } = string.Empty;
    [Id(2)] public long Count { get; set; }
}

[GenerateSerializer]
public class StateExportResult
{
    [Id(0)] public string CollectionName { get; set; } = string.Empty;
    [Id(1)] public string TypeName { get; set; } = string.Empty;
    [Id(2)] public int Skip { get; set; }
    [Id(3)] public int Limit { get; set; }
    [Id(4)] public bool HasMore { get; set; }
    [Id(5)] public List<ExportedStateRecord> Records { get; set; } = new();
    /// <summary>
    /// Cursor for next page (based on last record _id). Use this for cursor-based pagination instead of skip.
    /// </summary>
    [Id(6)] public string? NextCursor { get; set; }
}

[GenerateSerializer]
public class ExportedStateRecord
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string ETag { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object?> State { get; set; } = new();
    [Id(3)] public string? DeserializationNote { get; set; }
}
