using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Default implementation of agent plugin loader
/// </summary>
public class AgentPluginLoader : IAgentPluginLoader, IDisposable
{
    private readonly ILogger<AgentPluginLoader> _logger;
    private readonly PluginLoadOptions _options;
    private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblies = new();
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _loadContexts = new();
    private readonly ConcurrentDictionary<string, AgentPluginMetadata> _availablePlugins = new();

    public AgentPluginLoader(ILogger<AgentPluginLoader> logger, IOptions<PluginLoadOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new PluginLoadOptions();
    }

    public async Task<IAgentPlugin> LoadPluginAsync(string pluginName, string? version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading plugin: {PluginName}, Version: {Version}", pluginName, version ?? "latest");

            // Try to find plugin in available plugins registry
            var pluginKey = $"{pluginName}:{version ?? "latest"}";
            if (!_availablePlugins.TryGetValue(pluginKey, out var metadata))
            {
                throw new PluginLoadException(pluginName, version, $"Plugin '{pluginName}' version '{version}' not found");
            }

            // Load from assembly path if available
            // In a real implementation, this would come from a plugin store/registry
            var assemblyPath = GetPluginAssemblyPath(pluginName, version);
            return await LoadPluginFromAssemblyAsync(assemblyPath, null, cancellationToken);
        }
        catch (Exception ex) when (!(ex is PluginLoadException))
        {
            throw new PluginLoadException(pluginName, version, $"Failed to load plugin: {ex.Message}", ex);
        }
    }

    public async Task<IAgentPlugin> LoadPluginFromAssemblyAsync(string assemblyPath, string? typeName = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}");
        }

        try
        {
            var assemblyBytes = await File.ReadAllBytesAsync(assemblyPath, cancellationToken);
            var plugin = await LoadPluginFromBytesAsync(assemblyBytes, typeName, cancellationToken);
            
            // Update _availablePlugins registry for consistency
            await UpdateAvailablePluginsFromLoadedPlugin(plugin, assemblyPath, cancellationToken);
            
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from assembly: {AssemblyPath}", assemblyPath);
            throw new PluginLoadException("Unknown", null, $"Failed to load from assembly: {ex.Message}", ex);
        }
    }

    public async Task<IAgentPlugin> LoadPluginFromBytesAsync(byte[] assemblyBytes, string? typeName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            AssemblyLoadContext? loadContext = null;
            Assembly assembly;

            if (_options.IsolateInSeparateContext)
            {
                // Create isolated load context
                var contextName = $"PluginContext_{Guid.NewGuid():N}";
                loadContext = new AssemblyLoadContext(contextName, isCollectible: true);
                _loadContexts[contextName] = loadContext;

                using var stream = new MemoryStream(assemblyBytes);
                assembly = loadContext.LoadFromStream(stream);
            }
            else
            {
                // Load in default context
                assembly = Assembly.Load(assemblyBytes);
            }

            // Find plugin type
            var pluginType = FindPluginType(assembly, typeName);
            if (pluginType == null)
            {
                throw new InvalidOperationException($"No plugin type found in assembly. Expected type implementing {nameof(IAgentPlugin)}");
            }

            // Create plugin instance
            var plugin = Activator.CreateInstance(pluginType) as IAgentPlugin;
            if (plugin == null)
            {
                throw new InvalidOperationException($"Failed to create instance of plugin type: {pluginType.FullName}");
            }

            // Update _availablePlugins registry for consistency
            await UpdateAvailablePluginsFromAssembly(assembly, cancellationToken);

            _logger.LogInformation("Successfully loaded plugin: {PluginType}", pluginType.FullName);
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from bytes");
            throw new PluginLoadException("Unknown", null, $"Failed to load from bytes: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AgentPluginMetadata>> GetAvailablePluginsAsync(CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would scan plugin directories or query a plugin registry
        await ScanForPluginsAsync(cancellationToken);
        return _availablePlugins.Values;
    }

    public async Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Find and unload contexts associated with this plugin
            var contextsToUnload = _loadContexts.Where(kvp => kvp.Key.Contains(pluginName)).ToList();
            
            foreach (var (contextName, context) in contextsToUnload)
            {
                try
                {
                    context.Unload();
                    _loadContexts.TryRemove(contextName, out _);
                    _logger.LogInformation("Unloaded plugin context: {ContextName}", contextName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to unload plugin context: {ContextName}", contextName);
                }
            }
        }, cancellationToken);
    }

    private Type? FindPluginType(Assembly assembly, string? typeName)
    {
        try
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                return assembly.GetType(typeName);
            }

            // Find type with AgentPluginAttribute or implementing IAgentPlugin
            var types = assembly.GetTypes();
            
            // First, look for types with AgentPluginAttribute
            var pluginType = types.FirstOrDefault(t => t.GetCustomAttribute<AgentPluginAttribute>() != null);
            if (pluginType != null)
            {
                return pluginType;
            }

            // Fallback to types implementing IAgentPlugin
            return types.FirstOrDefault(t => typeof(IAgentPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning assembly for plugin types");
            return null;
        }
    }

    private string GetPluginAssemblyPath(string pluginName, string? version)
    {
        // Mock implementation - in reality this would query a plugin store
        var pluginDir = Path.Combine(Environment.CurrentDirectory, "plugins", pluginName);
        var versionDir = version != null ? Path.Combine(pluginDir, version) : pluginDir;
        return Path.Combine(versionDir, $"{pluginName}.dll");
    }

    private async Task ScanForPluginsAsync(CancellationToken cancellationToken)
    {
        // Mock implementation - scan plugins directory
        var pluginsDir = Path.Combine(Environment.CurrentDirectory, "plugins");
        if (!Directory.Exists(pluginsDir))
        {
            return;
        }

        await Task.Run(() =>
        {
            var pluginDirs = Directory.GetDirectories(pluginsDir);
            foreach (var pluginDir in pluginDirs)
            {
                var pluginName = Path.GetFileName(pluginDir);
                var dllPath = Path.Combine(pluginDir, $"{pluginName}.dll");
                
                if (File.Exists(dllPath))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        var pluginType = FindPluginType(assembly, null);
                        
                        if (pluginType != null)
                        {
                            var attr = pluginType.GetCustomAttribute<AgentPluginAttribute>();
                            var metadata = new AgentPluginMetadata(
                                attr?.Name ?? pluginName,
                                attr?.Version ?? "1.0.0",
                                attr?.Description ?? "Agent Plugin"
                            );
                            
                            var key = $"{metadata.Name}:{metadata.Version}";
                            _availablePlugins[key] = metadata;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to scan plugin: {PluginPath}", dllPath);
                    }
                }
            }
        }, cancellationToken);
    }

    private async Task UpdateAvailablePluginsFromLoadedPlugin(IAgentPlugin plugin, string assemblyPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                UpdateAvailablePluginsFromAssemblySync(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update _availablePlugins from loaded plugin at: {AssemblyPath}", assemblyPath);
            }
        }, cancellationToken);
    }

    private async Task UpdateAvailablePluginsFromAssembly(Assembly assembly, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            UpdateAvailablePluginsFromAssemblySync(assembly);
        }, cancellationToken);
    }

    private void UpdateAvailablePluginsFromAssemblySync(Assembly assembly)
    {
        try
        {
            var pluginType = FindPluginType(assembly, null);
            if (pluginType != null)
            {
                var attr = pluginType.GetCustomAttribute<AgentPluginAttribute>();
                var assemblyName = assembly.GetName();
                
                var metadata = new AgentPluginMetadata(
                    attr?.Name ?? assemblyName.Name ?? "Unknown",
                    attr?.Version ?? assemblyName.Version?.ToString() ?? "1.0.0",
                    attr?.Description ?? "Dynamically loaded plugin"
                );
                
                var key = $"{metadata.Name}:{metadata.Version}";
                _availablePlugins[key] = metadata;
                
                _logger.LogDebug("Updated _availablePlugins registry with plugin: {PluginName}:{Version}", 
                    metadata.Name, metadata.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract plugin metadata from assembly");
        }
    }

    public void Dispose()
    {
        foreach (var context in _loadContexts.Values)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unloading plugin context during disposal");
            }
        }
        
        _loadContexts.Clear();
        _loadedAssemblies.Clear();
    }
}

/// <summary>
/// Default plugin registry implementation
/// </summary>
public class AgentPluginRegistry : IAgentPluginRegistry
{
    private readonly ConcurrentDictionary<string, IAgentPlugin> _plugins = new();
    private readonly ILogger<AgentPluginRegistry> _logger;

    public AgentPluginRegistry(ILogger<AgentPluginRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RegisterPlugin(string agentId, IAgentPlugin plugin)
    {
        _plugins[agentId] = plugin;
        _logger.LogDebug("Registered plugin for agent: {AgentId}", agentId);
    }

    public IAgentPlugin? GetPlugin(string agentId)
    {
        return _plugins.TryGetValue(agentId, out var plugin) ? plugin : null;
    }

    public bool UnregisterPlugin(string agentId)
    {
        var removed = _plugins.TryRemove(agentId, out var plugin);
        if (removed)
        {
            _logger.LogDebug("Unregistered plugin for agent: {AgentId}", agentId);
            _ = Task.Run(async () =>
            {
                try
                {
                    await plugin!.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing plugin for agent: {AgentId}", agentId);
                }
            });
        }
        return removed;
    }

    public IEnumerable<(string AgentId, IAgentPlugin Plugin)> GetAllPlugins()
    {
        return _plugins.Select(kvp => (kvp.Key, kvp.Value));
    }
}