using System.Collections.Generic;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Context;

namespace Aevatar.Core.Interception.Services;

/// <summary>
/// Implementation of ITraceManager that provides runtime control over tracing configuration.
/// </summary>
public class TraceManager : ITraceManager
{
    /// <summary>
    /// Enables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to enable tracing for.</param>
    /// <returns>True if tracing was enabled, false if the trace ID is invalid.</returns>
    public bool EnableTracing(string traceId)
    {
        return TraceContext.EnableTracing(traceId);
    }

    /// <summary>
    /// Disables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to disable tracing for.</param>
    /// <returns>True if tracing was disabled, false if the trace ID wasn't found.</returns>
    public bool DisableTracing(string traceId)
    {
        return TraceContext.DisableTracing(traceId);
    }

    /// <summary>
    /// Adds a trace ID to tracking without making it the active trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to add to tracking.</param>
    /// <returns>True if the trace ID was added, false if it already existed or is invalid.</returns>
    public bool AddTrackedId(string traceId)
    {
        return TraceContext.AddTrackedId(traceId);
    }

    /// <summary>
    /// Removes a trace ID from tracking.
    /// </summary>
    /// <param name="traceId">The trace ID to remove from tracking.</param>
    /// <returns>True if the trace ID was removed, false if it didn't exist.</returns>
    public bool RemoveTrackedId(string traceId)
    {
        return TraceContext.RemoveTrackedId(traceId);
    }

    /// <summary>
    /// Gets all currently tracked trace IDs.
    /// </summary>
    /// <returns>A copy of all tracked IDs, or empty set if none.</returns>
    public HashSet<string> GetTrackedIds()
    {
        return TraceContext.GetTrackedIds();
    }

    /// <summary>
    /// Gets the current trace configuration.
    /// </summary>
    /// <returns>The current trace configuration, or null if not set.</returns>
    public TraceConfig? GetCurrentConfiguration()
    {
        return TraceContext.GetTraceConfig();
    }

    /// <summary>
    /// Sets the trace configuration.
    /// </summary>
    /// <param name="config">The trace configuration to set.</param>
    public void SetTraceConfig(TraceConfig config)
    {
        if (config == null)
            return;
            
        // Use the new UpdateTraceConfig method that updates existing instance
        TraceContext.UpdateTraceConfig(existingConfig =>
        {
            existingConfig.Enabled = config.Enabled;
            existingConfig.TrackedIds.Clear();
            foreach (var traceId in config.TrackedIds)
            {
                existingConfig.TrackedIds.Add(traceId);
            }
        });
    }

    /// <summary>
    /// Gets whether tracing is currently enabled for the active context.
    /// </summary>
    /// <returns>True if tracing is enabled, false otherwise.</returns>
    public bool IsTracingEnabled()
    {
        return TraceContext.IsTracingEnabled;
    }

    /// <summary>
    /// Clears the current trace context and resets to defaults.
    /// </summary>
    public void Clear()
    {
        // Clear the trace context
        TraceContext.Clear();
    }
}
