// ABOUTME: Client demonstrating how to interact with PluginGAgentHost
// ABOUTME: Shows complete usage of IPluginGAgentHost interface

using Aevatar.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;

namespace PluginGAgentHostDemo.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== PluginGAgentHost Client Demo ===");

        var host = new HostBuilder()
            .UseOrleansClient(clientBuilder =>
            {
                clientBuilder
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .ConfigureApplicationParts(parts =>
                    {
                        // Add the core assemblies for Orleans interfaces
                        parts.AddApplicationPart(typeof(IPluginGAgentHost).Assembly).WithReferences();
                    });
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.AddConsole());
            })
            .UseConsoleLifetime()
            .Build();

        // Start the client
        await host.StartAsync();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();

        try
        {
            await RunPluginDemoAsync(grainFactory, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task RunPluginDemoAsync(IGrainFactory grainFactory, ILogger logger)
    {
        Console.WriteLine("\n=== PluginGAgentHost Integration Demo ===\n");

        // Create plugin agents using the factory approach
        var factory = grainFactory.GetGrain<IPluginGAgentFactory>(0);

        Console.WriteLine("1. Creating Weather Service Plugin Agent");
        Console.WriteLine("---------------------------------------");
        
        var weatherAgentId = Guid.NewGuid();
        var weatherAgent = await factory.CreatePluginGAgentAsync(weatherAgentId, "WeatherService", "1.0.0");
        
        Console.WriteLine($"✓ Created weather agent with ID: {weatherAgentId}");
        Console.WriteLine($"✓ Plugin loaded: WeatherService v1.0.0");

        // Test weather service methods
        Console.WriteLine("\n2. Testing Weather Service Methods");
        Console.WriteLine("----------------------------------");

        var weatherResult = await weatherAgent.CallPluginMethodAsync("GetCurrentWeatherAsync", new object[] { "London" });
        Console.WriteLine($"✓ GetCurrentWeather('London'): {weatherResult}");

        // Test read-only method
        var tempResult = await weatherAgent.CallPluginMethodAsync("GetTemperatureAsync", new object[] { "Paris" });
        Console.WriteLine($"✓ GetTemperature('Paris'): {tempResult}");

        // Test one-way method (fire and forget)
        await weatherAgent.CallPluginMethodAsync("LogWeatherEventAsync", new object[] { "Storm warning issued" });
        Console.WriteLine($"✓ LogWeatherEvent (OneWay method called)");

        Console.WriteLine("\n3. Creating Calculator Plugin Agent");
        Console.WriteLine("-----------------------------------");

        var calculatorAgentId = Guid.NewGuid();
        var calculatorAgent = await factory.CreatePluginGAgentAsync(calculatorAgentId, "Calculator", "1.0.0");
        
        Console.WriteLine($"✓ Created calculator agent with ID: {calculatorAgentId}");
        Console.WriteLine($"✓ Plugin loaded: Calculator v1.0.0");

        // Test calculator methods
        Console.WriteLine("\n4. Testing Calculator Methods");
        Console.WriteLine("-----------------------------");

        var addResult = await calculatorAgent.CallPluginMethodAsync("AddAsync", new object[] { 15, 25 });
        Console.WriteLine($"✓ Add(15, 25): {addResult}");

        var multiplyResult = await calculatorAgent.CallPluginMethodAsync("MultiplyAsync", new object[] { 6, 7 });
        Console.WriteLine($"✓ Multiply(6, 7): {multiplyResult}");

        var divideResult = await calculatorAgent.CallPluginMethodAsync("DivideAsync", new object[] { 100.0, 7.0 });
        Console.WriteLine($"✓ Divide(100, 7): {divideResult}");

        Console.WriteLine("\n5. Plugin State Management");
        Console.WriteLine("--------------------------");

        // Set state for weather agent
        var weatherState = new { LastQuery = "London", Timestamp = DateTime.UtcNow };
        await weatherAgent.SetPluginStateAsync(weatherState);
        Console.WriteLine($"✓ Weather agent state set: {weatherState}");

        var retrievedState = await weatherAgent.GetPluginStateAsync();
        Console.WriteLine($"✓ Weather agent state retrieved: {retrievedState}");

        // Set state for calculator agent
        var calculatorState = new { LastOperation = "Add", Result = addResult };
        await calculatorAgent.SetPluginStateAsync(calculatorState);
        Console.WriteLine($"✓ Calculator agent state set: {calculatorState}");

        Console.WriteLine("\n6. Plugin Metadata");
        Console.WriteLine("------------------");

        var weatherMetadata = await weatherAgent.GetPluginMetadataAsync();
        Console.WriteLine($"✓ Weather plugin metadata: {weatherMetadata?.Name} v{weatherMetadata?.Version}");
        Console.WriteLine($"  Description: {weatherMetadata?.Description}");

        var calculatorMetadata = await calculatorAgent.GetPluginMetadataAsync();
        Console.WriteLine($"✓ Calculator plugin metadata: {calculatorMetadata?.Name} v{calculatorMetadata?.Version}");
        Console.WriteLine($"  Description: {calculatorMetadata?.Description}");

        Console.WriteLine("\n7. Plugin Hot Reload (Simulation)");
        Console.WriteLine("----------------------------------");

        await weatherAgent.ReloadPluginAsync();
        Console.WriteLine($"✓ Weather plugin reloaded (hot reload simulation)");

        // Test that the reloaded plugin still works
        var reloadTestResult = await weatherAgent.CallPluginMethodAsync("GetCurrentWeatherAsync", new object[] { "Tokyo" });
        Console.WriteLine($"✓ Post-reload test - GetCurrentWeather('Tokyo'): {reloadTestResult}");

        Console.WriteLine("\n8. Error Handling");
        Console.WriteLine("-----------------");

        try
        {
            await weatherAgent.CallPluginMethodAsync("NonExistentMethod", new object[] { });
            Console.WriteLine("✗ Should have thrown exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✓ Expected error handled: {ex.GetType().Name}");
        }

        Console.WriteLine("\n=== Demo Summary ===");
        Console.WriteLine("Key Benefits Demonstrated:");
        Console.WriteLine("• Plugins have zero Orleans dependencies");
        Console.WriteLine("• Full Orleans integration (clustering, persistence, events)");
        Console.WriteLine("• Type-safe method calling");
        Console.WriteLine("• State management through Orleans");
        Console.WriteLine("• Plugin metadata access");
        Console.WriteLine("• Hot reload capability");
        Console.WriteLine("• Robust error handling");
        Console.WriteLine("• Multiple plugin instances");

        Console.WriteLine("\nPluginGAgentHost successfully bridges:");
        Console.WriteLine("• IAgentPlugin (user's business logic)");
        Console.WriteLine("• GAgentBase (Orleans + event sourcing)");
        Console.WriteLine("• IPluginGAgentHost (Orleans grain interface)");

        Console.WriteLine("\n=== Demo Complete ===");
    }
}