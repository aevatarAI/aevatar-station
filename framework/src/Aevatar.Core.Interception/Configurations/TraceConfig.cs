using Orleans;
using System.Collections.Generic;

namespace Aevatar.Core.Interception.Configurations;

/// <summary>
/// Configuration for trace filtering and sampling.
/// </summary>
[GenerateSerializer]
public class TraceConfig
{
    /// <summary>
    /// Gets or sets whether tracing is enabled.
    /// </summary>
    [Id(0)]
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the list of tracked trace IDs.
    /// </summary>
    [Id(1)]
    public HashSet<string> TrackedIds { get; set; } = new();
    
    /// <summary>
    /// Determines if tracing should be enabled for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to check.</param>
    /// <returns>True if tracing should be enabled for this trace ID.</returns>
    public bool ShouldTrace(string traceId)
    {
        if (!Enabled)
            return false;
            
        // If no specific trace IDs are tracked, trace everything
        if (TrackedIds.Count == 0)
            return false;
            
        // Check if this specific trace ID is being tracked
        return TrackedIds.Contains(traceId);
    }
    
    /// <summary>
    /// Adds a trace ID to the tracked IDs set.
    /// </summary>
    /// <param name="traceId">The trace ID to add.</param>
    /// <returns>True if the trace ID was added, false if it already existed.</returns>
    public bool AddTrackedId(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;
            
        return TrackedIds.Add(traceId);
    }
    
    /// <summary>
    /// Removes a trace ID from the tracked IDs set.
    /// </summary>
    /// <param name="traceId">The trace ID to remove.</param>
    /// <returns>True if the trace ID was removed, false if it didn't exist.</returns>
    public bool RemoveTrackedId(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;
            
        return TrackedIds.Remove(traceId);
    }
    
    /// <summary>
    /// Gets a copy of all currently tracked trace IDs.
    /// </summary>
    /// <returns>A copy of the tracked IDs set.</returns>
    public HashSet<string> GetTrackedIds()
    {
        return new HashSet<string>(TrackedIds);
    }
}
