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
    Task<StateExportResult> ExportAsync(string collectionName, int skip, int limit);
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
    [Id(4)] public long TotalCount { get; set; }
    [Id(5)] public bool HasMore { get; set; }
    [Id(6)] public List<ExportedStateRecord> Records { get; set; } = new();
}

[GenerateSerializer]
public class ExportedStateRecord
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string ETag { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object?> State { get; set; } = new();
}
