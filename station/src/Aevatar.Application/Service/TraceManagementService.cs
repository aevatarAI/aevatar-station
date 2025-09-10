using System.Collections.Generic;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Services;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

/// <summary>
/// Service implementation for managing trace configuration at runtime.
/// This service wraps the framework's ITraceManager to provide business logic layer.
/// </summary>
public class TraceManagementService : ApplicationService, ITraceManagementService
{
    private readonly ITraceManager _traceManager;
    private readonly ILogger<TraceManagementService> _logger;

    public TraceManagementService(
        ITraceManager traceManager,
        ILogger<TraceManagementService> logger)
    {
        _traceManager = traceManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public TraceConfig? GetCurrentConfiguration()
    {
        try
        {
            var config = _traceManager.GetCurrentConfiguration();
            _logger.LogDebug("Retrieved current trace configuration: {Config}", config);
            return config;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve current trace configuration");
            return null;
        }
    }

    /// <inheritdoc/>
    public HashSet<string> GetTrackedIds()
    {
        try
        {
            var trackedIds = _traceManager.GetTrackedIds();
            _logger.LogDebug("Retrieved tracked trace IDs: {Count} IDs", trackedIds.Count);
            return trackedIds;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tracked trace IDs");
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Gets whether tracing is currently enabled.
    /// </summary>
    /// <returns>True if tracing is enabled, false otherwise.</returns>
    public bool IsTracingEnabled()
    {
        try
        {
            var isEnabled = _traceManager.IsTracingEnabled();
            _logger.LogDebug("Tracing enabled status: {IsEnabled}", isEnabled);
            return isEnabled;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to check if tracing is enabled");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool EnableTracing(string traceId, bool enabled = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                _logger.LogWarning("Attempted to enable tracing with null or empty trace ID");
                return false;
            }

            var success = enabled ? _traceManager.EnableTracing(traceId) : _traceManager.DisableTracing(traceId);
            if (success)
            {
                _logger.LogInformation("Successfully {Action} tracing for trace ID: {TraceId}", 
                    enabled ? "enabled" : "disabled", traceId);
            }
            else
            {
                _logger.LogWarning("Failed to {Action} tracing for trace ID: {TraceId}", 
                    enabled ? "enable" : "disable", traceId);
            }
            return success;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while {Action} tracing for trace ID: {TraceId}", 
                enabled ? "enabling" : "disabling", traceId);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool DisableTracing(string traceId)
    {
        return EnableTracing(traceId, false);
    }

    /// <inheritdoc/>
    public bool AddTrackedId(string traceId, bool enabled = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                _logger.LogWarning("Attempted to add tracked ID with null or empty trace ID");
                return false;
            }

            var success = _traceManager.AddTrackedId(traceId);
            if (success && enabled)
            {
                // Also enable global tracing when adding a tracked ID with enabled=true
                _traceManager.EnableTracing(traceId);
            }
            if (success)
            {
                _logger.LogInformation("Successfully added trace ID to tracking: {TraceId} (enabled: {Enabled})", 
                    traceId, enabled);
            }
            else
            {
                _logger.LogWarning("Failed to add trace ID to tracking: {TraceId}", traceId);
            }
            return success;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while adding trace ID to tracking: {TraceId}", traceId);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool RemoveTrackedId(string traceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                _logger.LogWarning("Attempted to remove tracked ID with null or empty trace ID");
                return false;
            }

            var success = _traceManager.RemoveTrackedId(traceId);
            if (success)
            {
                _logger.LogInformation("Successfully removed trace ID from tracking: {TraceId}", traceId);
            }
            else
            {
                _logger.LogWarning("Failed to remove trace ID from tracking: {TraceId}", traceId);
            }
            return success;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while removing trace ID from tracking: {TraceId}", traceId);
            return false;
        }
    }

    /// <summary>
    /// Clears the current trace context.
    /// </summary>
    public void Clear()
    {
        try
        {
            _traceManager.Clear();
            _logger.LogInformation("Successfully cleared trace context");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while clearing trace context");
        }
    }
}
