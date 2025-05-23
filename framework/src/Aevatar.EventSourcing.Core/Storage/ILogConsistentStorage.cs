using Orleans.Runtime;

namespace Aevatar.EventSourcing.Core.Storage;

public interface ILogConsistentStorage
{
    /// <summary>
    /// Read event logs from database.
    /// </summary>
    /// <param name="grainTypeName"></param>
    /// <param name="grainId"></param>
    /// <param name="fromVersion"></param>
    /// <param name="maxCount"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion,
        int maxCount);

    /// <summary>
    /// Get stored event logs count.
    /// </summary>
    /// <param name="grainTypeName"></param>
    /// <param name="grainId"></param>
    /// <returns></returns>
    Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId);

    /// <summary>
    /// Save event logs to database.
    /// </summary>
    /// <param name="grainTypeName"></param>
    /// <param name="grainId"></param>
    /// <param name="entries"></param>
    /// <param name="expectedVersion"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IList<TLogEntry> entries,
        int expectedVersion);
}