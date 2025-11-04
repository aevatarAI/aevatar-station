using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using Aevatar.Core.Interception.Configurations;

namespace Aevatar.Core.Interception.Context;

/// <summary>
/// Manages trace context across both HTTP and Orleans grain call boundaries.
/// Provides unified API for trace context management that works with both
/// AsyncLocal (HTTP contexts) and Orleans RequestContext (grain-to-grain calls).
/// </summary>
public static class TraceContext
{
    private static readonly AsyncLocal<string?> _activeTraceId = new();
    private static TraceConfig _config = new();
    
    // Thread-safe lock for concurrent access to _config
    private static readonly ReaderWriterLockSlim _configLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    
    /// <summary>
    /// Static constructor to ensure proper initialization
    /// </summary>
    static TraceContext()
    {
        // Register cleanup on application shutdown
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Dispose();
    }
    
    /// <summary>
    /// Disposes the ReaderWriterLockSlim to free resources
    /// </summary>
    public static void Dispose()
    {
        try
        {
            _configLock?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, ignore
        }
    }
    
    // Orleans RequestContext keys
    private const string TraceIdKey = "AevatarTraceId";
    private const string TraceConfigKey = "AevatarTraceConfig";
    
    /// <summary>
    /// Gets or sets the active trace ID.
    /// Automatically handles both AsyncLocal and Orleans RequestContext.
    /// </summary>
    public static string? ActiveTraceId
    {
        get => GetTraceId();
        set => SetTraceId(value);
    }
    
    /// <summary>
    /// Gets whether tracing is currently enabled for the active context.
    /// </summary>
    public static bool IsTracingEnabled
    {
        get
        {
            // return true;
            var config = GetTraceConfig();
            var traceId = ActiveTraceId;
            
            if (config == null)
                return false;
            
            return config.ShouldTrace(traceId);
        }
    }
    
    /// <summary>
    /// Updates the trace configuration by modifying the existing _config instance.
    /// Also updates RequestContext config if Orleans context is available.
    /// </summary>
    /// <param name="updateAction">Action to update the existing config</param>
    public static void UpdateTraceConfig(Action<TraceConfig> updateAction)
    {
        if (updateAction == null)
            return;

        _configLock.EnterWriteLock();
        try
        {
            // Always update existing _config reference, never create new instance
            updateAction(_config);
        }
        finally
        {
            _configLock.ExitWriteLock();
        }
        
        // Update RequestContext config if Orleans context is available
        // No lock needed - reference read is atomic and _config reference is never reassigned
        if (IsOrleansContextAvailable())
        {
            RequestContext.Set(TraceConfigKey, _config);
        }
    }
    
    /// <summary>
    /// Gets the current trace configuration.
    /// Reads from Orleans RequestContext if available, otherwise returns static _config.
    /// </summary>
    /// <returns>The current trace configuration, never null.</returns>
    public static TraceConfig GetTraceConfig()
    {
        // First try to read from Orleans RequestContext if available
        if (IsOrleansContextAvailable())
        {
            var orleansConfig = RequestContext.Get(TraceConfigKey) as TraceConfig;
            if (orleansConfig != null)
                return orleansConfig;
        }
        
        // Fallback to static _config
        return _config;
    }
    
    /// <summary>
    /// Propagates the current AsyncLocal context to Orleans RequestContext.
    /// Called by outgoing grain call filters.
    /// </summary>
    public static void PropagateToOrleansContext()
    {
        if (!IsOrleansContextAvailable())
            return;
            
        var traceId = _activeTraceId.Value;
        
        if (!string.IsNullOrEmpty(traceId))
        {
            RequestContext.Set(TraceIdKey, traceId);
        }
        
        // Store _config in RequestContext for grain-to-grain propagation
        // No lock needed - reference read is atomic and _config reference is never reassigned
        RequestContext.Set(TraceConfigKey, _config);
    }
    
    /// <summary>
    /// Reads context from Orleans RequestContext and sets AsyncLocal context.
    /// Called by incoming grain call filters.
    /// </summary>
    public static void ReadFromOrleansContext()
    {
        if (!IsOrleansContextAvailable())
            return;
            
        var traceId = RequestContext.Get(TraceIdKey) as string;
        
        if (!string.IsNullOrEmpty(traceId))
        {
            _activeTraceId.Value = traceId;
        }
        
        // Read _config from Orleans RequestContext and update static _config
        var orleansConfig = RequestContext.Get(TraceConfigKey) as TraceConfig;
        if (orleansConfig != null)
        {
            _configLock.EnterWriteLock();
            try
            {
                _config = orleansConfig;
            }
            finally
            {
                _configLock.ExitWriteLock();
            }
        }
    }
    
    /// <summary>
    /// Clears the current trace context.
    /// Resets _config to default values instead of setting to null.
    /// </summary>
    public static void Clear()
    {
        _activeTraceId.Value = null;
        
        _configLock.EnterWriteLock();
        try
        {
            // Reset _config to defaults instead of setting to null
            _config.Enabled = false;
            _config.TrackedIds.Clear();
        }
        finally
        {
            _configLock.ExitWriteLock();
        }
        
        if (IsOrleansContextAvailable())
        {
            RequestContext.Remove(TraceIdKey);
            RequestContext.Remove(TraceConfigKey);
        }
    }

    /// <summary>
    /// Enables tracing for a specific trace ID by setting it as active and adding it to TrackedIds.
    /// Updates the existing _config instance.
    /// </summary>
    /// <param name="traceId">The trace ID to enable tracing for.</param>
    /// <returns>True if tracing was enabled, false if the trace ID is invalid.</returns>
    public static bool EnableTracing(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;

        // Set the active trace ID
        ActiveTraceId = traceId;

        // Update existing _config instance
        UpdateTraceConfig(config =>
        {
            config.Enabled = true;
            config.AddTrackedId(traceId);
        });

        return true;
    }

    /// <summary>
    /// Disables tracing for a specific trace ID by removing it from TrackedIds.
    /// If it's the current active trace ID, clears the active trace ID as well.
    /// Updates the existing _config instance.
    /// </summary>
    /// <param name="traceId">The trace ID to disable tracing for.</param>
    /// <returns>True if tracing was disabled, false if the trace ID wasn't found.</returns>
    public static bool DisableTracing(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;

        var removed = false;
        
        UpdateTraceConfig(config =>
        {
            removed = config.RemoveTrackedId(traceId);
        });
        
        // If we removed the currently active trace ID, clear it
        if (removed && ActiveTraceId == traceId)
        {
            ActiveTraceId = null;
        }

        return removed;
    }

    /// <summary>
    /// Adds a trace ID to the current TrackedIds without making it the active trace ID.
    /// Updates the existing _config instance.
    /// </summary>
    /// <param name="traceId">The trace ID to add to tracking.</param>
    /// <returns>True if the trace ID was added, false if it already existed or is invalid.</returns>
    public static bool AddTrackedId(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;

        var added = false;
        
        UpdateTraceConfig(config =>
        {
            added = config.AddTrackedId(traceId);
        });

        return added;
    }

    /// <summary>
    /// Removes a trace ID from the current TrackedIds.
    /// Updates the existing _config instance.
    /// </summary>
    /// <param name="traceId">The trace ID to remove from tracking.</param>
    /// <returns>True if the trace ID was removed, false if it didn't exist.</returns>
    public static bool RemoveTrackedId(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return false;

        var removed = false;
        
        UpdateTraceConfig(config =>
        {
            removed = config.RemoveTrackedId(traceId);
        });

        return removed;
    }

    /// <summary>
    /// Gets all currently tracked trace IDs.
    /// Always reads from the existing _config reference.
    /// </summary>
    /// <returns>A copy of all tracked IDs, or empty set if none.</returns>
    public static HashSet<string> GetTrackedIds()
    {
        // Always read from existing _config reference
        return _config.GetTrackedIds();
    }
    
    private static string? GetTraceId()
    {
        // First try AsyncLocal (for HTTP contexts)
        var traceId = _activeTraceId.Value;
        if (!string.IsNullOrEmpty(traceId))
            return traceId;
        
        // Then try Orleans RequestContext (for grain contexts)
        if (IsOrleansContextAvailable())
        {
            return RequestContext.Get(TraceIdKey) as string;
        }
        
        return null;
    }
    
    private static void SetTraceId(string? value)
    {
        _activeTraceId.Value = value;
        
        // Also propagate to Orleans context if available
        if (IsOrleansContextAvailable())
        {
            if (!string.IsNullOrEmpty(value))
            {
                RequestContext.Set(TraceIdKey, value);
            }
            else
            {
                RequestContext.Remove(TraceIdKey);
            }
        }
    }
    
    private static bool IsOrleansContextAvailable()
    {
        try
        {
            // This will work if we're in Orleans context
            RequestContext.Get("__test__");
            return true;
        }
        catch
        {
            return false;
        }
    }
} 