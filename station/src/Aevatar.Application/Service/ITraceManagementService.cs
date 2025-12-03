using System.Collections.Generic;
using Aevatar.Core.Interception.Configurations;

namespace Aevatar.Service;

/// <summary>
/// Service interface for managing trace configuration at runtime.
/// </summary>
public interface ITraceManagementService
{
    /// <summary>
    /// Gets the current trace configuration.
    /// </summary>
    /// <returns>The current trace configuration.</returns>
    TraceConfig? GetCurrentConfiguration();

    /// <summary>
    /// Gets all currently tracked trace IDs.
    /// </summary>
    /// <returns>List of tracked trace IDs.</returns>
    HashSet<string> GetTrackedIds();

    /// <summary>
    /// Gets whether tracing is currently enabled.
    /// </summary>
    /// <returns>True if tracing is enabled, false otherwise.</returns>
    bool IsTracingEnabled();

    /// <summary>
    /// Enables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to enable.</param>
    /// <param name="enabled">Whether to enable or disable tracing.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool EnableTracing(string traceId, bool enabled = true);

    /// <summary>
    /// Disables tracing for a specific trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to disable.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool DisableTracing(string traceId);

    /// <summary>
    /// Adds a trace ID to tracking without making it the active trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to add.</param>
    /// <param name="enabled">Whether tracing is enabled for this ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool AddTrackedId(string traceId, bool enabled = true);

    /// <summary>
    /// Removes a trace ID from tracking.
    /// </summary>
    /// <param name="traceId">The trace ID to remove.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool RemoveTrackedId(string traceId);

    /// <summary>
    /// Clears the current trace context.
    /// </summary>
    void Clear();
}
