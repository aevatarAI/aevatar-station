using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Aevatar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PluginAgentExample;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Configure plugin load options
        services.Configure<PluginLoadOptions>(options =>
        {
            options.EnableHotReload = true;
            options.IsolateInSeparateContext = true;
            options.LoadTimeout = TimeSpan.FromSeconds(30);
        });
    })
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .UseAevatar(includingAbpServices: true)
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddConsole());
    })
    .UseConsoleLifetime();

var host = builder.Build();

Console.WriteLine("Starting WeatherAgent Plugin Silo...");
Console.WriteLine("Press Ctrl+C to shut down the silo");

// Load and register the WeatherAgentPlugin using DI services
try
{
    Console.WriteLine("Loading WeatherAgentPlugin...");

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var pluginLoader = host.Services.GetRequiredService<IAgentPluginLoader>();
    var pluginRegistry = host.Services.GetRequiredService<IAgentPluginRegistry>();

    logger.LogInformation("Loading WeatherAgentPlugin...");

    // Load plugin from current assembly
    var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
    var plugin = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);

    logger.LogInformation("Plugin loaded: {PluginName} v{Version}", plugin.Metadata.Name, plugin.Metadata.Version);
    logger.LogInformation("Plugin description: {Description}", plugin.Metadata.Description);

    // Register plugin in registry for Orleans grains to access
    var agentId = "weather-agent-silo-001";
    pluginRegistry.RegisterPlugin(agentId, plugin);

    // Note: AgentContext will be created by the DI container when needed by Orleans grains
    // The plugin will be initialized through the proper Orleans grain lifecycle
    
    logger.LogInformation("WeatherAgentPlugin successfully loaded and registered with ID: {AgentId}", agentId);
    
    Console.WriteLine($"✅ WeatherAgentPlugin loaded and registered as: {agentId}");
    Console.WriteLine($"   Plugin: {plugin.Metadata.Name} v{plugin.Metadata.Version}");
    Console.WriteLine($"   Description: {plugin.Metadata.Description}");
    Console.WriteLine();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to load WeatherAgentPlugin");
    Console.WriteLine($"❌ Failed to load plugin: {ex.Message}");
}

Console.WriteLine("Starting WeatherAgentPlugin Silo...");

await host.RunAsync(); 