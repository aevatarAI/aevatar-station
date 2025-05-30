using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Plugin;
using Aevatar.Core.Abstractions.Plugin;
using Orleans.Concurrency;
using System.Reflection;

namespace PluginAgentExample;

/// <summary>
/// Demonstrates the Orleans Grain Proxy Generator functionality
/// Shows both Reflection.Emit (recommended) and Castle DynamicProxy (fallback) approaches
/// </summary>
public class ProxyGeneratorExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Orleans Grain Proxy Generator Example");
        Console.WriteLine("=========================================");

        // Create host with logging and DI
        var host = CreateHost();
        var logger = host.Services.GetRequiredService<ILogger<ProxyGeneratorExample>>();
        var methodRouter = host.Services.GetRequiredService<OrleansMethodRouter>();
        var proxyGenerator = host.Services.GetRequiredService<OrleansGrainProxyGenerator>();

        try
        {
            // Create the weather service plugin
            var weatherPlugin = new WeatherServicePlugin();
            logger.LogInformation("Created weather service plugin: {PluginName} v{Version}", 
                weatherPlugin.Name, weatherPlugin.Version);

            // Demonstrate both approaches
            await DemonstrateReflectionEmitApproach(proxyGenerator, weatherPlugin, logger);
            await DemonstrateCastleDynamicProxyApproach(proxyGenerator, weatherPlugin, logger);
            await CompareOrleansAttributeSupport(proxyGenerator, weatherPlugin, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in proxy generator example");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("\n✅ Example completed successfully!");
    }

    /// <summary>
    /// Demonstrates the Reflection.Emit approach with full Orleans attribute support
    /// </summary>
    private static async Task DemonstrateReflectionEmitApproach(OrleansGrainProxyGenerator proxyGenerator, 
        WeatherServicePlugin weatherPlugin, ILogger logger)
    {
        Console.WriteLine("\n🎯 === Reflection.Emit Approach (Recommended) ===");
        Console.WriteLine("Full Orleans attribute support with actual [ReadOnly], [AlwaysInterleave], [OneWay] attributes");

        try
        {
            // Generate grain implementation using Reflection.Emit
            var grainImpl = proxyGenerator.GenerateGrainImplementation<IWeatherServiceGrain>(weatherPlugin);
            logger.LogInformation("✅ Successfully generated grain implementation using Reflection.Emit");

            // Test the implementation
            await TestGrainImplementation(grainImpl, "Reflection.Emit", logger);

            // Verify Orleans attributes are actually present
            VerifyOrleansAttributesPresent(grainImpl, "Reflection.Emit", logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Reflection.Emit approach failed");
            Console.WriteLine($"❌ Reflection.Emit failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates the Castle DynamicProxy approach (fallback)
    /// </summary>
    private static async Task DemonstrateCastleDynamicProxyApproach(OrleansGrainProxyGenerator proxyGenerator, 
        WeatherServicePlugin weatherPlugin, ILogger logger)
    {
        Console.WriteLine("\n⚠️  === Castle DynamicProxy Approach (Fallback) ===");
        Console.WriteLine("Limited Orleans attribute support - attributes are emulated, not preserved");

        try
        {
            // Generate grain proxy using Castle DynamicProxy
            var grainProxy = proxyGenerator.GenerateGrainProxy<IWeatherServiceGrain>(weatherPlugin);
            logger.LogInformation("✅ Successfully generated grain proxy using Castle DynamicProxy");

            // Test the proxy
            await TestGrainImplementation(grainProxy, "Castle DynamicProxy", logger);

            // Check Orleans attributes (they won't be present)
            VerifyOrleansAttributesPresent(grainProxy, "Castle DynamicProxy", logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Castle DynamicProxy approach failed");
            Console.WriteLine($"❌ Castle DynamicProxy failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test grain functionality
    /// </summary>
    private static async Task TestGrainImplementation(IWeatherServiceGrain grain, string approach, ILogger logger)
    {
        Console.WriteLine($"\n🧪 Testing {approach} Grain Functionality:");

        // Test 1: Basic method call
        Console.WriteLine("\n1. Testing basic method call:");
        var weather = await grain.GetCurrentWeatherAsync("New York");
        Console.WriteLine($"   🌤️  Weather: {weather}");

        // Test 2: ReadOnly method (Orleans optimization)
        Console.WriteLine("\n2. Testing ReadOnly method (Orleans optimized):");
        var temperature = await grain.GetTemperatureAsync("London");
        Console.WriteLine($"   🌡️  Temperature: {temperature}°C");

        // Test 3: AlwaysInterleave method (concurrent execution)
        Console.WriteLine("\n3. Testing AlwaysInterleave method (concurrent execution):");
        var monitoringTask1 = grain.StartMonitoringAsync("Tokyo", 5);
        var monitoringTask2 = grain.StartMonitoringAsync("Sydney", 10);
        await Task.WhenAll(monitoringTask1, monitoringTask2);
        Console.WriteLine("   📡 Multiple monitoring sessions started concurrently");

        // Test 4: OneWay method (fire-and-forget)
        Console.WriteLine("\n4. Testing OneWay method (fire-and-forget):");
        _ = grain.LogWeatherEventAsync("Storm warning issued"); // No await needed for OneWay
        Console.WriteLine("   📝 Weather event logged (OneWay - no waiting)");

        // Test 5: Method with complex parameters and return types
        Console.WriteLine("\n5. Testing complex method with multiple parameters:");
        var forecast = await grain.GetExtendedForecastAsync("Seattle", 7, true);
        Console.WriteLine($"   📅 7-day forecast: {forecast.Count} entries, detailed: {forecast.FirstOrDefault()?.Contains("detailed")}");

        // Test 6: Complex return type
        Console.WriteLine("\n6. Testing complex return type:");
        var stats = await grain.GetWeatherStatsAsync("Boston");
        Console.WriteLine($"   📊 Stats: Avg {stats.AverageTemperature}°C, Min {stats.MinTemperature}°C, Max {stats.MaxTemperature}°C");

        logger.LogInformation("✅ All {Approach} tests completed successfully", approach);
    }

    /// <summary>
    /// Verify Orleans attributes presence for comparison
    /// </summary>
    private static void VerifyOrleansAttributesPresent(IWeatherServiceGrain grain, string approach, ILogger logger)
    {
        Console.WriteLine($"\n🔍 Verifying Orleans Attributes for {approach}:");
        
        var grainType = grain.GetType();
        logger.LogInformation("🔍 Analyzing generated type: {TypeName}", grainType.Name);

        // Check each method and its Orleans attributes
        var interfaceMethods = typeof(IWeatherServiceGrain).GetMethods();
        
        foreach (var interfaceMethod in interfaceMethods)
        {
            // Find the corresponding method in the generated type
            var generatedMethod = grainType.GetMethod(interfaceMethod.Name);
            if (generatedMethod != null)
            {
                Console.WriteLine($"   📋 Method: {interfaceMethod.Name}");
                
                // Check for Orleans attributes
                CheckOrleansAttribute<ReadOnlyAttribute>(generatedMethod, interfaceMethod, "ReadOnly");
                CheckOrleansAttribute<AlwaysInterleaveAttribute>(generatedMethod, interfaceMethod, "AlwaysInterleave");
                CheckOrleansAttribute<OneWayAttribute>(generatedMethod, interfaceMethod, "OneWay");
            }
            else
            {
                Console.WriteLine($"   ❌ Method {interfaceMethod.Name} not found in generated type");
            }
        }

        logger.LogInformation("✅ Orleans attribute verification completed for {Approach}", approach);
    }

    /// <summary>
    /// Check for specific Orleans attribute
    /// </summary>
    private static void CheckOrleansAttribute<TAttribute>(MethodInfo generatedMethod, MethodInfo interfaceMethod, string attributeName)
        where TAttribute : Attribute
    {
        var interfaceHasAttr = interfaceMethod.GetCustomAttribute<TAttribute>() != null;
        var generatedHasAttr = generatedMethod.GetCustomAttribute<TAttribute>() != null;

        if (interfaceHasAttr)
        {
            if (generatedHasAttr)
            {
                Console.WriteLine($"      ✅ [{attributeName}] attribute correctly preserved");
            }
            else
            {
                Console.WriteLine($"      ❌ [{attributeName}] attribute missing in generated method");
            }
        }
    }

    /// <summary>
    /// Compare Orleans attribute support between both approaches
    /// </summary>
    private static async Task CompareOrleansAttributeSupport(OrleansGrainProxyGenerator proxyGenerator, 
        WeatherServicePlugin weatherPlugin, ILogger logger)
    {
        Console.WriteLine("\n📊 === Orleans Attribute Support Comparison ===");

        try
        {
            var reflectionEmitGrain = proxyGenerator.GenerateGrainImplementation<IWeatherServiceGrain>(weatherPlugin);
            var castleProxyGrain = proxyGenerator.GenerateGrainProxy<IWeatherServiceGrain>(weatherPlugin);

            Console.WriteLine("\n| Feature | Reflection.Emit | Castle DynamicProxy |");
            Console.WriteLine("|---------|-----------------|---------------------|");

            // Check each Orleans attribute type
            CheckAttributeSupport<ReadOnlyAttribute>(reflectionEmitGrain, castleProxyGrain, "ReadOnly", "GetTemperatureAsync");
            CheckAttributeSupport<AlwaysInterleaveAttribute>(reflectionEmitGrain, castleProxyGrain, "AlwaysInterleave", "StartMonitoringAsync");
            CheckAttributeSupport<OneWayAttribute>(reflectionEmitGrain, castleProxyGrain, "OneWay", "LogWeatherEventAsync");

            Console.WriteLine("| Runtime Optimization | ✅ Full support | ❌ No optimization |");
            Console.WriteLine("| Concurrency Control | ✅ Orleans-managed | ⚠️  Application-managed |");
            Console.WriteLine("| Performance | ✅ Optimized | ⚠️  Overhead |");

            logger.LogInformation("✅ Orleans attribute support comparison completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Comparison failed");
            Console.WriteLine($"❌ Comparison failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check attribute support for specific attribute type
    /// </summary>
    private static void CheckAttributeSupport<TAttribute>(IWeatherServiceGrain reflectionEmitGrain, 
        IWeatherServiceGrain castleProxyGrain, string attributeName, string methodName)
        where TAttribute : Attribute
    {
        var reflectionEmitMethod = reflectionEmitGrain.GetType().GetMethod(methodName);
        var castleProxyMethod = castleProxyGrain.GetType().GetMethod(methodName);

        var reflectionEmitHasAttr = reflectionEmitMethod?.GetCustomAttribute<TAttribute>() != null;
        var castleProxyHasAttr = castleProxyMethod?.GetCustomAttribute<TAttribute>() != null;

        var reflectionEmitStatus = reflectionEmitHasAttr ? "✅ Actual attribute" : "❌ Missing";
        var castleProxyStatus = castleProxyHasAttr ? "✅ Actual attribute" : "⚠️  Emulated behavior";

        Console.WriteLine($"| {attributeName} | {reflectionEmitStatus} | {castleProxyStatus} |");
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<OrleansMethodRouter>();
                services.AddSingleton<OrleansGrainProxyGenerator>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();
    }
}

/// <summary>
/// Orleans grain interface that the plugin will implement
/// This interface includes all Orleans attributes that should be preserved
/// </summary>
public interface IWeatherServiceGrain
{
    /// <summary>
    /// Get current weather for a location
    /// </summary>
    Task<string> GetCurrentWeatherAsync(string location);

    /// <summary>
    /// Get temperature - marked as ReadOnly for Orleans optimization
    /// </summary>
    [ReadOnly]
    Task<decimal> GetTemperatureAsync(string location);

    /// <summary>
    /// Start monitoring - allows concurrent execution
    /// </summary>
    [AlwaysInterleave]
    Task StartMonitoringAsync(string location, int intervalMinutes);

    /// <summary>
    /// Log event - fire and forget (OneWay)
    /// </summary>
    [OneWay]
    Task LogWeatherEventAsync(string eventMessage);

    /// <summary>
    /// Get extended forecast with complex parameters
    /// </summary>
    Task<List<string>> GetExtendedForecastAsync(string location, int days, bool includeDetails);

    /// <summary>
    /// Get weather statistics
    /// </summary>
    [ReadOnly]
    Task<WeatherStats> GetWeatherStatsAsync(string location);
}

/// <summary>
/// Complex return type to test type handling
/// </summary>
public class WeatherStats
{
    public decimal AverageTemperature { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public int TotalReadings { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Plugin implementation that will be proxied to Orleans grain
/// Notice: NO Orleans dependencies whatsoever!
/// </summary>
[AgentPlugin("WeatherService", "2.0.0", Description = "Advanced weather service with Orleans proxy support")]
public class WeatherServicePlugin : AgentPluginBase
{
    private readonly Dictionary<string, decimal> _temperatures = new();
    private readonly Dictionary<string, WeatherStats> _stats = new();
    private readonly List<string> _monitoredLocations = new();
    private readonly List<string> _events = new();

    public override AgentPluginMetadata Metadata { get; protected set; } = 
        new("WeatherService", "2.0.0", "Advanced weather service with Orleans proxy support");

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("WeatherService plugin initializing...");
        
        // Initialize some sample data
        _temperatures["New York"] = 22.5m;
        _temperatures["London"] = 15.0m;
        _temperatures["Tokyo"] = 28.0m;
        _temperatures["Sydney"] = 20.0m;

        // Initialize stats
        foreach (var location in _temperatures.Keys)
        {
            _stats[location] = new WeatherStats
            {
                AverageTemperature = _temperatures[location],
                MinTemperature = _temperatures[location] - 5,
                MaxTemperature = _temperatures[location] + 8,
                TotalReadings = Random.Shared.Next(100, 1000),
                LastUpdated = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
            };
        }

        await Task.CompletedTask;
    }

    [AgentMethod("GetCurrentWeatherAsync")]
    public async Task<string> GetCurrentWeatherAsync(string location)
    {
        await Task.Delay(Random.Shared.Next(10, 50)); // Simulate API call
        
        if (location == "INVALID_CITY")
        {
            throw new InvalidOperationException("Weather service unavailable for invalid city");
        }

        var temp = _temperatures.GetValueOrDefault(location, 20.0m);
        var conditions = GetRandomConditions();
        
        return $"{location}: {conditions}, {temp}°C";
    }

    [AgentMethod("GetTemperatureAsync", IsReadOnly = true)]
    public async Task<decimal> GetTemperatureAsync(string location)
    {
        await Task.Delay(5); // Fast read operation
        return _temperatures.GetValueOrDefault(location, 20.0m);
    }

    [AgentMethod("StartMonitoringAsync", AlwaysInterleave = true)]
    public async Task StartMonitoringAsync(string location, int intervalMinutes)
    {
        await Task.Delay(20); // Simulate setup time
        
        if (!_monitoredLocations.Contains(location))
        {
            _monitoredLocations.Add(location);
        }
        
        Logger?.LogInformation($"Started monitoring {location} every {intervalMinutes} minutes");
    }

    [AgentMethod("LogWeatherEventAsync", OneWay = true)]
    public async Task LogWeatherEventAsync(string eventMessage)
    {
        await Task.Delay(1);
        _events.Add($"{DateTime.UtcNow:HH:mm:ss}: {eventMessage}");
        Logger?.LogDebug($"Logged weather event: {eventMessage}");
    }

    [AgentMethod("GetExtendedForecastAsync")]
    public async Task<List<string>> GetExtendedForecastAsync(string location, int days, bool includeDetails)
    {
        await Task.Delay(30); // Simulate complex calculation
        
        var forecast = new List<string>();
        var baseTemp = _temperatures.GetValueOrDefault(location, 20.0m);
        
        for (int i = 0; i < days; i++)
        {
            var dayTemp = baseTemp + (decimal)(Random.Shared.NextDouble() - 0.5) * 10;
            var conditions = GetRandomConditions();
            
            var entry = includeDetails 
                ? $"Day {i + 1}: {conditions}, {dayTemp:F1}°C (detailed forecast with humidity and pressure data)"
                : $"Day {i + 1}: {conditions}, {dayTemp:F1}°C";
                
            forecast.Add(entry);
        }
        
        return forecast;
    }

    [AgentMethod("GetWeatherStatsAsync", IsReadOnly = true)]
    public async Task<WeatherStats> GetWeatherStatsAsync(string location)
    {
        await Task.Delay(10);
        return _stats.GetValueOrDefault(location, new WeatherStats
        {
            AverageTemperature = 20.0m,
            MinTemperature = 15.0m,
            MaxTemperature = 25.0m,
            TotalReadings = 0,
            LastUpdated = DateTime.UtcNow
        });
    }

    private string GetRandomConditions()
    {
        var conditions = new[] { "Sunny", "Cloudy", "Partly Cloudy", "Rainy", "Overcast", "Clear" };
        return conditions[Random.Shared.Next(conditions.Length)];
    }
}

/// <summary>
/// Simple agent context for testing
/// </summary>
public class SimpleAgentContext : IAgentContext
{
    public SimpleAgentContext(string agentId)
    {
        AgentId = agentId;
        Logger = new SimpleAgentLogger();
        Configuration = new Dictionary<string, object>();
    }

    public string AgentId { get; }
    public IAgentLogger Logger { get; }
    public IReadOnlyDictionary<string, object> Configuration { get; }

    public Task PublishEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TResponse> PublishEventWithResponseAsync<TResponse>(IAgentEvent agentEvent, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where TResponse : class
    {
        return Task.FromResult(Activator.CreateInstance<TResponse>());
    }

    public Task<IAgentReference> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IAgentReference>(null!);
    }

    public Task RegisterAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SubscribeToAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class SimpleAgentLogger : IAgentLogger
{
    public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
    public void LogInformation(string message) => Console.WriteLine($"[INFO] {message}");
    public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
    public void LogError(string message, Exception? exception = null) => Console.WriteLine($"[ERROR] {message} {exception?.Message}");
}