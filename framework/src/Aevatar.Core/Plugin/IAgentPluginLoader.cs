using Aevatar.Core.Abstractions.Plugin;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Interface for loading agent plugins
/// </summary>
public interface IAgentPluginLoader
{
    /// <summary>
    /// Load a plugin by name and version
    /// </summary>
    Task<IAgentPlugin> LoadPluginAsync(string pluginName, string? version = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Load a plugin from assembly path
    /// </summary>
    Task<IAgentPlugin> LoadPluginFromAssemblyAsync(string assemblyPath, string? typeName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Load a plugin from byte array (for dynamic loading)
    /// </summary>
    Task<IAgentPlugin> LoadPluginFromBytesAsync(byte[] assemblyBytes, string? typeName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available plugins
    /// </summary>
    Task<IEnumerable<AgentPluginMetadata>> GetAvailablePluginsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unload a plugin (if supported)
    /// </summary>
    Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Plugin loading options
/// </summary>
public class PluginLoadOptions
{
    public bool EnableHotReload { get; set; } = false;
    public bool IsolateInSeparateContext { get; set; } = true;
    public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public string[]? AllowedAssemblies { get; set; }
    public string[]? ForbiddenAssemblies { get; set; }
}

/// <summary>
/// Plugin registry for managing loaded plugins
/// </summary>
public interface IAgentPluginRegistry
{
    /// <summary>
    /// Register a plugin instance
    /// </summary>
    void RegisterPlugin(string agentId, IAgentPlugin plugin);
    
    /// <summary>
    /// Get a registered plugin
    /// </summary>
    IAgentPlugin? GetPlugin(string agentId);
    
    /// <summary>
    /// Unregister a plugin
    /// </summary>
    bool UnregisterPlugin(string agentId);
    
    /// <summary>
    /// Get all registered plugins
    /// </summary>
    IEnumerable<(string AgentId, IAgentPlugin Plugin)> GetAllPlugins();
}

/// <summary>
/// Exception thrown when plugin loading fails
/// </summary>
public class PluginLoadException : Exception
{
    public string PluginName { get; }
    public string? Version { get; }

    public PluginLoadException(string pluginName, string? version, string message) 
        : base(message)
    {
        PluginName = pluginName;
        Version = version;
    }

    public PluginLoadException(string pluginName, string? version, string message, Exception innerException) 
        : base(message, innerException)
    {
        PluginName = pluginName;
        Version = version;
    }
}