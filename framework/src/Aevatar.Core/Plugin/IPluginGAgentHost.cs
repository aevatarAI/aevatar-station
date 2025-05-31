using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Orleans.Concurrency;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Orleans grain interface for plugin-based GAgent host
/// This interface exposes the plugin management functionality through Orleans RPC
/// </summary>
public interface IPluginGAgentHost : IStateGAgent<PluginAgentState>
{
    /// <summary>
    /// Route method calls to the plugin
    /// </summary>
    Task<object?> CallPluginMethodAsync(string methodName, object?[] parameters);

    /// <summary>
    /// Get plugin state
    /// </summary>
    [ReadOnly]
    Task<object?> GetPluginStateAsync();

    /// <summary>
    /// Set plugin state
    /// </summary>
    Task SetPluginStateAsync(object? state);

    /// <summary>
    /// Get plugin metadata
    /// </summary>
    [ReadOnly]
    Task<AgentPluginMetadata?> GetPluginMetadataAsync();

    /// <summary>
    /// Reload the plugin (for hot reload scenarios)
    /// </summary>
    Task ReloadPluginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize plugin configuration (called by factory to set up initial state)
    /// </summary>
    Task InitializePluginConfigurationAsync(string pluginName, string? pluginVersion, Dictionary<string, object>? configuration);
} 