# IWeatherGrain Complete Usage Example

This document demonstrates the complete usage of `IWeatherGrain` from interface definition to client consumption.

## 1. Orleans Grain Interface Definition

```csharp
// File: IWeatherGrain.cs
using Orleans;

namespace Aevatar.Examples.Grains
{
    /// <summary>
    /// Orleans grain interface that defines the contract for weather services
    /// This interface is what Orleans clients will use to communicate with the grain
    /// </summary>
    public interface IWeatherGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// Gets current weather - marked as ReadOnly for concurrent access optimization
        /// </summary>
        [ReadOnly]
        Task<string> GetWeatherAsync(string city);
        
        /// <summary>
        /// Gets temperature only - also ReadOnly
        /// </summary>
        [ReadOnly] 
        Task<decimal> GetTemperatureAsync(string city);
        
        /// <summary>
        /// Starts monitoring weather changes - can run concurrently with other methods
        /// </summary>
        [AlwaysInterleave]
        Task StartMonitoringAsync(string city, int intervalMinutes);
        
        /// <summary>
        /// Logs weather event - fire and forget, no need to wait for response
        /// </summary>
        [OneWay]
        Task LogWeatherEventAsync(string eventMessage);
        
        /// <summary>
        /// Updates weather data - modifies state, so no special attributes
        /// </summary>
        Task UpdateWeatherDataAsync(string city, string weatherData);
    }
}
```

## 2. Plugin Implementation (No Orleans Dependencies)

```csharp
// File: WeatherPlugin.cs
using Aevatar.Core.Abstractions.Plugin;

namespace Aevatar.Examples.Plugins
{
    /// <summary>
    /// Plugin implementation - completely independent of Orleans
    /// </summary>
    [AgentPlugin("WeatherService", "1.0.0", "Provides weather information services")]
    public class WeatherPlugin : AgentPluginBase
    {
        private readonly Dictionary<string, WeatherData> _weatherCache = new();
        private readonly Dictionary<string, CancellationTokenSource> _monitors = new();
        
        [AgentMethod("GetWeather", IsReadOnly = true)]
        public async Task<string> GetWeatherAsync(string city)
        {
            // Pure business logic - no Orleans dependencies
            if (_weatherCache.TryGetValue(city, out var data))
            {
                return $"{city}: {data.Condition}, {data.Temperature}°C";
            }
            
            // Simulate API call
            await Task.Delay(100);
            return $"{city}: Sunny, 25°C";
        }
        
        [AgentMethod("GetTemperature", IsReadOnly = true)]
        public async Task<decimal> GetTemperatureAsync(string city)
        {
            if (_weatherCache.TryGetValue(city, out var data))
            {
                return data.Temperature;
            }
            
            return 25.0m;
        }
        
        [AgentMethod("StartMonitoring", AlwaysInterleave = true)]
        public async Task StartMonitoringAsync(string city, int intervalMinutes)
        {
            // Cancel existing monitor if any
            if (_monitors.TryGetValue(city, out var cts))
            {
                cts.Cancel();
            }
            
            // Start new monitoring
            var newCts = new CancellationTokenSource();
            _monitors[city] = newCts;
            
            _ = Task.Run(async () =>
            {
                while (!newCts.Token.IsCancellationRequested)
                {
                    await CheckWeatherChangesAsync(city);
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), newCts.Token);
                }
            });
        }
        
        [AgentMethod("LogWeatherEvent", OneWay = true)]
        public async Task LogWeatherEventAsync(string eventMessage)
        {
            // Fire and forget logging
            Logger?.LogInformation($"Weather Event: {eventMessage}");
            
            // Could write to file, database, etc.
            await Task.CompletedTask;
        }
        
        [AgentMethod("UpdateWeatherData")]
        public async Task UpdateWeatherDataAsync(string city, string weatherData)
        {
            // Parse and update cache
            var parts = weatherData.Split(',');
            _weatherCache[city] = new WeatherData
            {
                Condition = parts[0],
                Temperature = decimal.Parse(parts[1])
            };
            
            // Notify subscribers about the change
            await PublishEventAsync("WeatherUpdated", new { City = city, Data = weatherData });
        }
        
        private async Task CheckWeatherChangesAsync(string city)
        {
            // Monitoring logic
            Logger?.LogDebug($"Checking weather for {city}");
        }
        
        private class WeatherData
        {
            public string Condition { get; set; } = "";
            public decimal Temperature { get; set; }
        }
    }
}
```

## 3. Proxy Generation and Registration

```csharp
// File: WeatherGrainBootstrap.cs
using Orleans.Hosting;
using Aevatar.Core;

namespace Aevatar.Examples.Bootstrap
{
    public class WeatherGrainBootstrap
    {
        public static async Task ConfigureWeatherGrain(ISiloBuilder siloBuilder)
        {
            // 1. Load the plugin
            var pluginLoader = new AgentPluginLoader();
            var weatherPlugin = await pluginLoader.LoadPluginAsync<WeatherPlugin>();
            
            // 2. Initialize the plugin
            var context = new AgentContext
            {
                AgentId = "weather-service",
                Logger = new ConsoleAgentLogger()
            };
            await weatherPlugin.InitializeAsync(context);
            
            // 3. Generate Orleans grain proxy
            var methodRouter = new OrleansMethodRouter();
            var proxyGenerator = new OrleansGrainProxyGenerator(methodRouter);
            
            // This creates a dynamic type that:
            // - Implements IWeatherGrain
            // - Routes all method calls to the plugin
            // - Preserves all Orleans attributes
            var grainProxy = proxyGenerator.GenerateGrainProxy<IWeatherGrain>(weatherPlugin);
            
            // 4. Register the generated proxy with Orleans
            siloBuilder.ConfigureServices(services =>
            {
                // The proxy is registered as a singleton grain implementation
                services.AddSingleton<IWeatherGrain>(grainProxy);
            });
        }
    }
}
```

## 4. Client Usage (Exactly Like Any Orleans Grain)

```csharp
// File: WeatherClient.cs
using Orleans;

namespace Aevatar.Examples.Client
{
    public class WeatherClient
    {
        private readonly IClusterClient _clusterClient;
        
        public WeatherClient(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }
        
        public async Task UseWeatherGrainAsync()
        {
            // Get grain reference - Orleans handles the routing
            var weatherGrain = _clusterClient.GetGrain<IWeatherGrain>(0);
            
            // 1. ReadOnly operations can run concurrently
            var weatherTask = weatherGrain.GetWeatherAsync("London");
            var tempTask = weatherGrain.GetTemperatureAsync("London");
            
            // Both execute in parallel due to [ReadOnly] attribute
            await Task.WhenAll(weatherTask, tempTask);
            
            Console.WriteLine($"Weather: {weatherTask.Result}");
            Console.WriteLine($"Temperature: {tempTask.Result}°C");
            
            // 2. Start monitoring (runs concurrently with other operations)
            await weatherGrain.StartMonitoringAsync("London", 5);
            
            // 3. Fire and forget logging (returns immediately)
            weatherGrain.LogWeatherEventAsync("Started monitoring London");
            
            // 4. Update weather data (exclusive access - no concurrency)
            await weatherGrain.UpdateWeatherDataAsync("London", "Rainy,18");
            
            // 5. Multiple clients can call ReadOnly methods simultaneously
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(weatherGrain.GetWeatherAsync("London"));
            }
            
            // All 100 calls execute concurrently on the grain
            await Task.WhenAll(tasks);
        }
    }
}
```

## 5. How the Proxy Works Behind the Scenes

```csharp
// This is what the generated proxy looks like internally (simplified)
public class GeneratedWeatherGrainProxy : Grain, IWeatherGrain
{
    private readonly IAgentPlugin _plugin;
    private readonly IMethodRouter _router;
    
    // Constructor injected by proxy generator
    public GeneratedWeatherGrainProxy(IAgentPlugin plugin, IMethodRouter router)
    {
        _plugin = plugin;
        _router = router;
    }
    
    // Generated method with Orleans attribute preserved
    [ReadOnly]
    public async Task<string> GetWeatherAsync(string city)
    {
        // Castle DynamicProxy intercepts the call
        // Routes to plugin via method router
        var result = await _router.RouteMethodAsync(_plugin, "GetWeather", new object[] { city });
        return (string)result;
    }
    
    [ReadOnly]
    public async Task<decimal> GetTemperatureAsync(string city)
    {
        var result = await _router.RouteMethodAsync(_plugin, "GetTemperature", new object[] { city });
        return (decimal)result;
    }
    
    [AlwaysInterleave]
    public async Task StartMonitoringAsync(string city, int intervalMinutes)
    {
        await _router.RouteMethodAsync(_plugin, "StartMonitoring", new object[] { city, intervalMinutes });
    }
    
    [OneWay]
    public async Task LogWeatherEventAsync(string eventMessage)
    {
        // OneWay methods don't wait for completion
        _ = _router.RouteMethodAsync(_plugin, "LogWeatherEvent", new object[] { eventMessage });
    }
    
    public async Task UpdateWeatherDataAsync(string city, string weatherData)
    {
        await _router.RouteMethodAsync(_plugin, "UpdateWeatherData", new object[] { city, weatherData });
    }
}
```

## Key Points About IWeatherGrain Usage

1. **Interface Contract**: `IWeatherGrain` defines the contract that Orleans clients use. It's a standard Orleans grain interface with attributes.

2. **Plugin Implementation**: The plugin implements the business logic without knowing about Orleans. Methods are mapped via `[AgentMethod]` attributes.

3. **Proxy Bridge**: The generated proxy implements `IWeatherGrain` and forwards all calls to the plugin, preserving Orleans semantics.

4. **Client Transparency**: Clients use `IWeatherGrain` exactly like any other Orleans grain - they don't know a plugin is behind it.

5. **Orleans Features**: All Orleans features work automatically:
   - **Concurrency**: `[ReadOnly]` methods run concurrently
   - **Interleaving**: `[AlwaysInterleave]` allows concurrent execution with other methods
   - **Fire-and-forget**: `[OneWay]` methods return immediately
   - **Single-threaded**: Methods without attributes run exclusively
   - **Location transparency**: Orleans handles grain placement and routing
   - **Persistence**: State can be persisted via Orleans providers
   - **Streaming**: Can subscribe to Orleans streams

This architecture provides the best of both worlds: developers write simple plugins, while the system provides full Orleans functionality transparently.