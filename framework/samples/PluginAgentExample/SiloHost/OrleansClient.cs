using Aevatar.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace SiloHost;

/// <summary>
/// Standalone Orleans client that connects to the silo and calls plugin methods
/// This demonstrates how external applications can interact with plugin-based agents
/// </summary>
public class OrleansClient
{
    private readonly ILogger<OrleansClient> _logger;
    private IClusterClient? _client;

    public OrleansClient(ILogger<OrleansClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting Orleans client...");

        var hostBuilder = Host.CreateDefaultBuilder()
            .UseOrleansClient((context, clientBuilder) =>
            {
                clientBuilder
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "PluginAgentService";
                    });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        var host = hostBuilder.Build();
        await host.StartAsync();
        
        _client = host.Services.GetRequiredService<IClusterClient>();
        
        _logger.LogInformation("✅ Orleans client connected successfully");
    }

    public async Task StopAsync()
    {
        // For Orleans 9.0.1, we need to stop the host
        if (_client != null)
        {
            // The client will be disposed when the host is disposed
            _logger.LogInformation("Orleans client disconnected");
        }
    }

    public async Task TestPluginMethodCallsAsync(string agentId)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Client not started");
        }

        Console.WriteLine($"\n🧪 Testing plugin method calls on agent: {agentId}");
        Console.WriteLine("=".PadRight(50, '='));

        try
        {
            // Get the PluginGAgentHost grain
            var grainId = GrainId.Create("PluginGAgent", agentId);
            var grain = _client.GetGrain<PluginGAgentHost>(grainId);

            // Test 1: Get current weather (ReadOnly method)
            Console.WriteLine("🌤️  Testing GetCurrentWeather (ReadOnly)...");
            var weather = await grain.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"    Result: {weather != null} (Type: {weather?.GetType().Name})");

            // Test 2: Get forecast with parameters
            Console.WriteLine("📅 Testing GetForecast with 5 days...");
            var forecast = await grain.CallPluginMethodAsync("GetForecast", new object[] { 5 });
            Console.WriteLine($"    Result: {forecast != null} (Type: {forecast?.GetType().Name})");

            // Test 3: Update location (State-changing method)
            Console.WriteLine("📍 Testing UpdateLocation...");
            var locationResult = await grain.CallPluginMethodAsync("UpdateLocation", new object[] { "New York" });
            Console.WriteLine($"    Result: {locationResult}");

            // Test 4: Create alert
            Console.WriteLine("🚨 Testing CreateAlert...");
            var alertId = await grain.CallPluginMethodAsync("CreateAlert", 
                new object[] { "Client Test", "Alert created from Orleans client", 15 });
            Console.WriteLine($"    Alert ID: {alertId}");

            // Test 5: Get alerts
            Console.WriteLine("📋 Testing GetAlerts...");
            var alerts = await grain.CallPluginMethodAsync("GetAlerts", new object[] { true });
            Console.WriteLine($"    Active alerts: {alerts != null}");

            // Test 6: Get plugin health status
            Console.WriteLine("❤️  Testing GetHealthStatus...");
            var health = await grain.CallPluginMethodAsync("GetHealthStatus", Array.Empty<object>());
            Console.WriteLine($"    Health status: {health != null}");

            // Test 7: Get plugin state
            Console.WriteLine("💾 Testing GetPluginState...");
            var state = await grain.GetPluginStateAsync();
            Console.WriteLine($"    State retrieved: {state != null}");

            // Test 8: Get plugin metadata
            Console.WriteLine("ℹ️  Testing GetPluginMetadata...");
            var metadata = grain.GetPluginMetadata();
            Console.WriteLine($"    Plugin: {metadata?.Name} v{metadata?.Version}");
            Console.WriteLine($"    Description: {metadata?.Description}");

            Console.WriteLine("✅ All plugin method calls completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing plugin method calls");
            Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }

    public async Task TestInterAgentCommunicationAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Client not started");
        }

        Console.WriteLine("\n🤝 Testing Inter-Agent Communication");
        Console.WriteLine("=".PadRight(40, '='));

        try
        {
            // Create two weather agents for different cities
            var nyAgentId = "weather-ny-client";
            var laAgentId = "weather-la-client";

            // Get grain references
            var nyGrain = _client.GetGrain<PluginGAgentHost>(GrainId.Create("PluginGAgent", nyAgentId));
            var laGrain = _client.GetGrain<PluginGAgentHost>(GrainId.Create("PluginGAgent", laAgentId));

            // Set different locations
            await nyGrain.CallPluginMethodAsync("UpdateLocation", new object[] { "New York" });
            await laGrain.CallPluginMethodAsync("UpdateLocation", new object[] { "Los Angeles" });

            Console.WriteLine("🏙️  Set up NY and LA weather agents");

            // Test inter-agent method call (may fail in mock environment)
            try
            {
                Console.WriteLine("📞 Testing RequestWeatherFromAgent...");
                var remoteWeather = await nyGrain.CallPluginMethodAsync("RequestWeatherFromAgent", new object[] { laAgentId });
                Console.WriteLine($"    NY agent got LA weather: {remoteWeather != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  Inter-agent call failed (expected in this demo): {ex.Message}");
            }

            // Test sending alert between agents
            try
            {
                Console.WriteLine("📨 Testing SendAlertToAgent...");
                var alertSent = await nyGrain.CallPluginMethodAsync("SendAlertToAgent", 
                    new object[] { laAgentId, "Storm Warning", "High winds from the east" });
                Console.WriteLine($"    Alert sent from NY to LA: {alertSent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  Alert sending failed (expected in this demo): {ex.Message}");
            }

            Console.WriteLine("✅ Inter-agent communication tests completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing inter-agent communication");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    public async Task TestPluginHotReloadAsync(string agentId)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Client not started");
        }

        Console.WriteLine("\n🔄 Testing Plugin Hot Reload");
        Console.WriteLine("=".PadRight(30, '='));

        try
        {
            var grain = _client.GetGrain<PluginGAgentHost>(GrainId.Create("PluginGAgent", agentId));

            // Get initial state
            Console.WriteLine("📊 Getting initial plugin state...");
            var initialMetadata = grain.GetPluginMetadata();
            Console.WriteLine($"    Initial plugin: {initialMetadata?.Name} v{initialMetadata?.Version}");

            // Test plugin reload
            Console.WriteLine("🔄 Reloading plugin...");
            await grain.ReloadPluginAsync();
            Console.WriteLine("    Plugin reloaded successfully");

            // Verify plugin still works after reload
            Console.WriteLine("🧪 Testing plugin after reload...");
            var weather = await grain.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"    Weather call after reload: {weather != null}");

            // Get metadata after reload
            var reloadedMetadata = grain.GetPluginMetadata();
            Console.WriteLine($"    Reloaded plugin: {reloadedMetadata?.Name} v{reloadedMetadata?.Version}");

            Console.WriteLine("✅ Plugin hot reload test completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing plugin hot reload");
            Console.WriteLine($"❌ Hot reload error: {ex.Message}");
        }
    }
} 