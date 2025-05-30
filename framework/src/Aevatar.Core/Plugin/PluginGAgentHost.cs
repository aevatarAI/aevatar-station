using System.Collections.Concurrent;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Host GAgent that loads and manages plugin-based agents
/// This class bridges the plugin system to GAgentBase
/// </summary>
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class PluginGAgentHost : GAgentBase<PluginAgentState, PluginStateLogEvent>
{
    private readonly IAgentPluginLoader _pluginLoader;
    private readonly IAgentPluginRegistry _pluginRegistry;
    private readonly ConcurrentDictionary<string, MethodInfo> _exposedMethods = new();
    private IAgentPlugin? _plugin;
    private IAgentContext? _pluginContext;

    public PluginGAgentHost(
        IAgentPluginLoader pluginLoader,
        IAgentPluginRegistry pluginRegistry)
    {
        _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        
        try
        {
            // Load plugin based on grain ID or configuration
            await LoadPluginAsync(cancellationToken);
            
            // Initialize plugin with context
            if (_plugin != null)
            {
                _pluginContext = CreatePluginContext();
                await _plugin.InitializeAsync(_pluginContext, cancellationToken);
                
                // Register plugin in registry
                _pluginRegistry.RegisterPlugin(this.GetGrainId().ToString(), _plugin);
                
                // Cache exposed methods for routing
                CacheExposedMethods();
                
                Logger.LogInformation("Plugin loaded and initialized for agent: {AgentId}", this.GetGrainId());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load plugin for agent: {AgentId}", this.GetGrainId());
            throw;
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        try
        {
            if (_plugin != null)
            {
                await _plugin.DisposeAsync();
                _pluginRegistry.UnregisterPlugin(this.GetGrainId().ToString());
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error disposing plugin for agent: {AgentId}", this.GetGrainId());
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(_plugin?.Metadata.Description ?? "Plugin-based GAgent");
    }

    /// <summary>
    /// Route method calls to the plugin
    /// </summary>
    public async Task<object?> CallPluginMethodAsync(string methodName, object?[] parameters)
    {
        if (_plugin == null)
        {
            throw new InvalidOperationException("Plugin not loaded");
        }

        try
        {
            return await _plugin.ExecuteMethodAsync(methodName, parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling plugin method: {MethodName}", methodName);
            throw;
        }
    }

    /// <summary>
    /// Handle Orleans events and route them to plugin
    /// </summary>
    [AllEventHandler(allowSelfHandling: true)]
    protected override async Task ForwardEventAsync(EventWrapperBase eventWrapper)
    {
        // First, call base implementation to handle Orleans forwarding
        await base.ForwardEventAsync(eventWrapper);
        
        // Then route to plugin if it's a plugin event
        if (eventWrapper is EventWrapper<PluginEventWrapper> pluginEventWrapper && _plugin != null)
        {
            var pluginEvent = new AgentEvent
            {
                EventType = pluginEventWrapper.Event.PluginEventType,
                Data = pluginEventWrapper.Event.PluginEventData,
                Timestamp = pluginEventWrapper.Event.Timestamp,
                CorrelationId = pluginEventWrapper.Event.CorrelationId?.ToString(),
                SourceAgentId = pluginEventWrapper.Event.SourceAgentId
            };

            try
            {
                await _plugin.HandleEventAsync(pluginEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling event in plugin: {EventType}", pluginEvent.EventType);
                throw;
            }
        }
    }

    /// <summary>
    /// Get plugin state
    /// </summary>
    public async Task<object?> GetPluginStateAsync()
    {
        if (_plugin == null)
        {
            return null;
        }

        return await _plugin.GetStateAsync();
    }

    /// <summary>
    /// Set plugin state
    /// </summary>
    public async Task SetPluginStateAsync(object? state)
    {
        if (_plugin == null)
        {
            throw new InvalidOperationException("Plugin not loaded");
        }

        await _plugin.SetStateAsync(state);
        
        // Optionally persist to Orleans state
        State.PluginState = state;
        // This would trigger state persistence through GAgentBase
    }

    /// <summary>
    /// Get plugin metadata
    /// </summary>
    public AgentPluginMetadata? GetPluginMetadata()
    {
        return _plugin?.Metadata;
    }

    /// <summary>
    /// Reload the plugin (for hot reload scenarios)
    /// </summary>
    public async Task ReloadPluginAsync(CancellationToken cancellationToken = default)
    {
        if (_plugin != null)
        {
            await _plugin.DisposeAsync();
            _pluginRegistry.UnregisterPlugin(this.GetGrainId().ToString());
        }

        await LoadPluginAsync(cancellationToken);
        
        if (_plugin != null && _pluginContext != null)
        {
            await _plugin.InitializeAsync(_pluginContext, cancellationToken);
            _pluginRegistry.RegisterPlugin(this.GetGrainId().ToString(), _plugin);
            CacheExposedMethods();
        }
    }

    // Private helper methods
    private async Task LoadPluginAsync(CancellationToken cancellationToken)
    {
        // Determine which plugin to load based on grain configuration
        var pluginName = State.PluginName ?? DeterminePluginNameFromGrainId();
        var pluginVersion = State.PluginVersion;

        if (string.IsNullOrEmpty(pluginName))
        {
            throw new InvalidOperationException("No plugin name specified for agent");
        }

        _plugin = await _pluginLoader.LoadPluginAsync(pluginName, pluginVersion, cancellationToken);
    }

    private string DeterminePluginNameFromGrainId()
    {
        // Extract plugin name from grain ID or type
        var grainId = this.GetGrainId().ToString();
        
        // Simple extraction - in practice this would be more sophisticated
        if (grainId.Contains("_"))
        {
            return grainId.Split('_')[0];
        }
        
        return grainId;
    }

    private IAgentContext CreatePluginContext()
    {
        var configuration = new Dictionary<string, object>();
        
        // Add any configuration from Orleans state
        if (State.Configuration != null)
        {
            foreach (var kvp in State.Configuration)
            {
                configuration[kvp.Key] = kvp.Value;
            }
        }

        return new AgentContext(this, Logger, GrainFactory, ServiceProvider, configuration);
    }

    private void CacheExposedMethods()
    {
        if (_plugin == null) return;

        var pluginType = _plugin.GetType();
        var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var agentMethodAttr = method.GetCustomAttribute<AgentMethodAttribute>();
            if (agentMethodAttr != null)
            {
                var methodName = agentMethodAttr.MethodName ?? method.Name;
                _exposedMethods[methodName] = method;
                
                Logger.LogDebug("Cached exposed method: {MethodName} -> {ActualMethod}", methodName, method.Name);
            }
        }
    }
}

/// <summary>
/// State for plugin-based agents
/// </summary>
public class PluginAgentState : StateBase
{
    public string? PluginName { get; set; }
    public string? PluginVersion { get; set; }
    public object? PluginState { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    public DateTime LastLoadTime { get; set; }
}

/// <summary>
/// State log event for plugin agents
/// </summary>
public class PluginStateLogEvent : StateLogEventBase<PluginStateLogEvent>
{
    public string? PluginName { get; set; }
    public string? PluginVersion { get; set; }
    public object? PluginStateChange { get; set; }
    public string? ChangeType { get; set; } // "Load", "Reload", "StateUpdate", etc.
}

/// <summary>
/// Factory for creating plugin-based GAgents
/// </summary>
public interface IPluginGAgentFactory
{
    /// <summary>
    /// Create a plugin-based GAgent
    /// </summary>
    Task<PluginGAgentHost> CreatePluginGAgentAsync(string agentId, string pluginName, string? pluginVersion = null);
    
    /// <summary>
    /// Create a plugin-based GAgent with custom configuration
    /// </summary>
    Task<PluginGAgentHost> CreatePluginGAgentAsync(string agentId, string pluginName, string? pluginVersion, Dictionary<string, object>? configuration);
}

/// <summary>
/// Default implementation of plugin GAgent factory
/// </summary>
public class PluginGAgentFactory : IPluginGAgentFactory
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<PluginGAgentFactory> _logger;

    public PluginGAgentFactory(IGrainFactory grainFactory, ILogger<PluginGAgentFactory> logger)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PluginGAgentHost> CreatePluginGAgentAsync(string agentId, string pluginName, string? pluginVersion = null)
    {
        return await CreatePluginGAgentAsync(agentId, pluginName, pluginVersion, null);
    }

    public async Task<PluginGAgentHost> CreatePluginGAgentAsync(string agentId, string pluginName, string? pluginVersion, Dictionary<string, object>? configuration)
    {
        var grainId = GrainId.Create("PluginGAgent", agentId);
        var grain = _grainFactory.GetGrain<PluginGAgentHost>(grainId);
        
        // Initialize the grain's state with plugin information
        var state = await grain.GetStateAsync();
        state.PluginName = pluginName;
        state.PluginVersion = pluginVersion;
        state.Configuration = configuration;
        state.LastLoadTime = DateTime.UtcNow;
        
        _logger.LogInformation("Created plugin GAgent: {AgentId} with plugin: {PluginName}:{PluginVersion}", 
            agentId, pluginName, pluginVersion);
        
        return grain;
    }
}