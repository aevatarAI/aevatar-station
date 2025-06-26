// ABOUTME: Orleans silo configured for PluginGAgentHost approach
// ABOUTME: Shows complete setup for plugin-based agents in Orleans

using Aevatar.Core.Plugin;
using Aevatar.EventSourcing.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Storage;
using ProxyGeneratorDemo.Plugins;
using System.Reflection;

namespace PluginGAgentHostDemo.Silo;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== PluginGAgentHost Silo Starting ===");

        var host = new HostBuilder()
            .UseOrleansGrainFactory()
            .UseOrleans((context, siloBuilder) =>
            {
                siloBuilder
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseInMemoryReminderService()
                    .AddMemoryGrainStorageAsDefault()
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddMemoryStreams("StreamProvider")
                    .ConfigureApplicationParts(parts =>
                    {
                        // Add the core assemblies
                        parts.AddApplicationPart(typeof(PluginGAgentHost).Assembly).WithReferences();
                        parts.AddApplicationPart(typeof(Program).Assembly).WithReferences();
                        
                        // Add plugin assemblies
                        parts.AddApplicationPart(typeof(WeatherServicePlugin).Assembly).WithReferences();
                    })
                    .ConfigureServices(services =>
                    {
                        // Register plugin infrastructure
                        services.AddSingleton<IAgentPluginLoader, DefaultAgentPluginLoader>();
                        services.AddSingleton<IAgentPluginRegistry, DefaultAgentPluginRegistry>();
                        services.AddSingleton<IPluginGAgentFactory, PluginGAgentFactory>();
                        
                        // Configure event sourcing for plugins
                        services.AddInMemoryBasedLogConsistencyProviderAsDefault();
                    });
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.AddConsole());
            })
            .UseConsoleLifetime()
            .Build();

        Console.WriteLine("Silo configured with PluginGAgentHost support");
        Console.WriteLine("Available plugins:");
        Console.WriteLine("- WeatherServicePlugin");
        Console.WriteLine("- CalculatorPlugin");
        Console.WriteLine();
        Console.WriteLine("Starting silo...");

        await host.RunAsync();
    }
}

/// <summary>
/// Default implementation of plugin loader for demo
/// </summary>
public class DefaultAgentPluginLoader : IAgentPluginLoader
{
    private readonly ILogger<DefaultAgentPluginLoader> _logger;

    public DefaultAgentPluginLoader(ILogger<DefaultAgentPluginLoader> logger)
    {
        _logger = logger;
    }

    public async Task<IAgentPlugin> LoadPluginAsync(string pluginName, string? pluginVersion = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading plugin: {PluginName} v{PluginVersion}", pluginName, pluginVersion);

        // For demo, create plugins based on name
        return pluginName.ToLowerInvariant() switch
        {
            "weatherservice" or "weather" => new WeatherServicePlugin(),
            "calculator" => new CalculatorPlugin(),
            _ => throw new ArgumentException($"Unknown plugin: {pluginName}")
        };
    }

    public Task<IEnumerable<AgentPluginMetadata>> GetAvailablePluginsAsync(CancellationToken cancellationToken = default)
    {
        var plugins = new[]
        {
            new AgentPluginMetadata("WeatherService", "1.0.0") { Description = "Weather forecasting service" },
            new AgentPluginMetadata("Calculator", "1.0.0") { Description = "Mathematical calculator" }
        };

        return Task.FromResult<IEnumerable<AgentPluginMetadata>>(plugins);
    }

    public Task<IAgentPlugin> LoadPluginFromAssemblyAsync(string assemblyPath, string? typeName = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("LoadPluginFromAssemblyAsync not implemented in demo");
    }

    public Task<IAgentPlugin> LoadPluginFromBytesAsync(byte[] assemblyBytes, string? typeName = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("LoadPluginFromBytesAsync not implemented in demo");
    }

    public Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unloading plugin: {PluginName}", pluginName);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Default implementation of plugin registry for demo
/// </summary>
public class DefaultAgentPluginRegistry : IAgentPluginRegistry
{
    private readonly Dictionary<string, IAgentPlugin> _registeredPlugins = new();
    private readonly ILogger<DefaultAgentPluginRegistry> _logger;

    public DefaultAgentPluginRegistry(ILogger<DefaultAgentPluginRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterPlugin(string agentId, IAgentPlugin plugin)
    {
        _registeredPlugins[agentId] = plugin;
        _logger.LogInformation("Registered plugin {PluginName} for agent {AgentId}", 
            plugin.Metadata.Name, agentId);
    }

    public IAgentPlugin? GetPlugin(string agentId)
    {
        _registeredPlugins.TryGetValue(agentId, out var plugin);
        return plugin;
    }

    public bool UnregisterPlugin(string agentId)
    {
        if (_registeredPlugins.Remove(agentId))
        {
            _logger.LogInformation("Unregistered plugin for agent {AgentId}", agentId);
            return true;
        }
        return false;
    }

    public IEnumerable<(string AgentId, IAgentPlugin Plugin)> GetAllPlugins()
    {
        return _registeredPlugins.Select(kvp => (kvp.Key, kvp.Value));
    }
}