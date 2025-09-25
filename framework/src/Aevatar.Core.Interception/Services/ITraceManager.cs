using System.Collections.Generic;
using Aevatar.Core.Interception.Configurations;

namespace Aevatar.Core.Interception.Services;

/// <summary>
/// Provides runtime control over tracing configuration for the interception system.
/// </summary>
public interface ITraceManager
{
    /// <summary>
    /// Enables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to enable tracing for.</param>
    /// <returns>True if tracing was enabled, false if the trace ID is invalid.</returns>
    bool EnableTracing(string traceId);

    /// <summary>
    /// Disables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to disable tracing for.</param>
    /// <returns>True if tracing was disabled, false if the trace ID wasn't found.</returns>
    bool DisableTracing(string traceId);

    /// <summary>
    /// Adds a trace ID to tracking without making it the active trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to add to tracking.</param>
    /// <returns>True if the trace ID was added, false if it already existed or is invalid.</returns>
    bool AddTrackedId(string traceId);

    /// <summary>
    /// Removes a trace ID from tracking.
    /// </summary>
    /// <param name="traceId">The trace ID to remove from tracking.</param>
    /// <returns>True if the trace ID was removed, false if it didn't exist.</returns>
    bool RemoveTrackedId(string traceId);

    /// <summary>
    /// Gets all currently tracked trace IDs.
    /// </summary>
    /// <returns>A copy of all tracked IDs, or empty set if none.</returns>
    HashSet<string> GetTrackedIds();

    /// <summary>
    /// Gets the current trace configuration.
    /// </summary>
    /// <returns>The current trace configuration, or null if not set.</returns>
    TraceConfig? GetCurrentConfiguration();

    /// <summary>
    /// Sets the trace configuration.
    /// </summary>
    /// <param name="config">The trace configuration to set.</param>
    void SetTraceConfig(TraceConfig config);

    /// <summary>
    /// Gets whether tracing is currently enabled for the active context.
    /// </summary>
    /// <returns>True if tracing is enabled, false otherwise.</returns>
    bool IsTracingEnabled();

    /// <summary>
    /// Clears the current trace context.
    /// </summary>
    void Clear();
}
