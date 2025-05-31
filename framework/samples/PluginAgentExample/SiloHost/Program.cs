using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using PluginAgentExample;
using System.Net;

namespace SiloHost;

/// <summary>
/// Orleans Silo host that demonstrates plugin-based agent loading
/// This shows how plugins can be loaded and exposed as Orleans grains
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 Plugin Agent Orleans Silo Host");
        Console.WriteLine("=================================");

        try
        {
            var host = CreateHostBuilder(args).Build();

            // Start the silo
            await host.StartAsync();
            
            Console.WriteLine("✅ Orleans Silo started successfully");
            Console.WriteLine("🔌 Plugin system is ready to load agents");
            Console.WriteLine();

            // Demonstrate plugin loading
            await DemonstratePluginLoadingAsync(host);

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();

            await host.StopAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting silo: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseOrleans((context, siloBuilder) =>
            {
                siloBuilder
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "PluginAgentService";
                    })
                    .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                    .ConfigureLogging(logging => logging.AddConsole());
            })
            .ConfigureServices((context, services) =>
            {
                // Register plugin system services
                services.Configure<PluginLoadOptions>(options =>
                {
                    options.EnableHotReload = true;
                    options.IsolateInSeparateContext = false; // Keep in same context for simplicity
                    options.LoadTimeout = TimeSpan.FromSeconds(30);
                });
                
                services.AddSingleton<IAgentPluginLoader, AgentPluginLoader>();
                services.AddSingleton<IAgentPluginRegistry, AgentPluginRegistry>();
                services.AddSingleton<OrleansMethodRouter>();
                services.AddSingleton<OrleansGrainProxyGenerator>();
                services.AddSingleton<PluginGAgentFactory>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }

    private static async Task DemonstratePluginLoadingAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var clusterClient = host.Services.GetRequiredService<IClusterClient>();
        var pluginLoader = host.Services.GetRequiredService<IAgentPluginLoader>();
        var pluginRegistry = host.Services.GetRequiredService<IAgentPluginRegistry>();
        var pluginFactory = host.Services.GetRequiredService<PluginGAgentFactory>();

        try
        {
            Console.WriteLine("📦 Loading WeatherAgentPlugin...");
            
            // Load the plugin from current assembly (since it's compiled with this project)
            var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
            var plugin = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);
            
            Console.WriteLine($"✅ Loaded plugin: {plugin.Metadata.Name} v{plugin.Metadata.Version}");
            Console.WriteLine($"   Description: {plugin.Metadata.Description}");

            // Create a plugin-based GAgent using the factory
            var agentId = "weather-orleans-001";
            var pluginGAgent = await pluginFactory.CreatePluginGAgentAsync(
                agentId, 
                plugin.Metadata.Name, 
                plugin.Metadata.Version,
                new Dictionary<string, object>
                {
                    ["Location"] = "San Francisco",
                    ["UpdateInterval"] = 5,
                    ["EnableAlerts"] = true
                });

            Console.WriteLine($"✅ Created PluginGAgent: {agentId}");

            // Test calling methods through Orleans grain
            Console.WriteLine("\n🔧 Testing grain method calls...");
            
            // Call GetCurrentWeather through Orleans
            var weather = await pluginGAgent.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Current weather: {weather != null}");
            
            // Call GetForecast with parameters
            var forecast = await pluginGAgent.CallPluginMethodAsync("GetForecast", new object[] { 3 });
            Console.WriteLine($"📅 3-day forecast: {forecast != null}");
            
            // Call UpdateLocation
            var locationUpdated = await pluginGAgent.CallPluginMethodAsync("UpdateLocation", new object[] { "Los Angeles" });
            Console.WriteLine($"📍 Location updated: {locationUpdated}");
            
            // Get plugin state
            var state = await pluginGAgent.GetPluginStateAsync();
            Console.WriteLine($"💾 Plugin state retrieved: {state != null}");
            
            // Get plugin metadata
            var metadata = pluginGAgent.GetPluginMetadata();
            Console.WriteLine($"ℹ️  Plugin metadata: {metadata?.Name} v{metadata?.Version}");

            // Test Orleans client calls (this shows how external clients would interact)
            Console.WriteLine("\n📞 Testing external Orleans client calls...");
            await TestOrleansClientCallsAsync(clusterClient, agentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error demonstrating plugin loading");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static async Task TestOrleansClientCallsAsync(IClusterClient clusterClient, string agentId)
    {
        try
        {
            // Get the PluginGAgentHost grain by ID
            var grainId = GrainId.Create("PluginGAgent", agentId);
            var grain = clusterClient.GetGrain<PluginGAgentHost>(grainId);
            
            Console.WriteLine("🔗 Got grain reference from Orleans client");
            
            // Call methods through Orleans grain interface
            var weather = await grain.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Orleans client weather call: {weather != null}");
            
            var alerts = await grain.CallPluginMethodAsync("GetAlerts", new object[] { true });
            Console.WriteLine($"🚨 Orleans client alerts call: {alerts != null}");
            
            // Test creating an alert
            var alertId = await grain.CallPluginMethodAsync("CreateAlert", 
                new object[] { "Test Alert", "This is a test alert from Orleans client", 10 });
            Console.WriteLine($"📨 Created alert via Orleans client: {alertId}");
            
            Console.WriteLine("✅ All Orleans client calls completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in Orleans client calls: {ex.Message}");
            throw;
        }
    }
} 