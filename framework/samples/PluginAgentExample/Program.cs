using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PluginAgentExample;

/// <summary>
/// Example program demonstrating how to use the plugin-based agent system
/// This shows how developers can create agents without depending on Orleans or GAgentBase
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🌤️  Weather Agent Plugin Example");
        Console.WriteLine("=================================");
        Console.WriteLine("This example demonstrates a plugin-based agent with zero Orleans dependencies.");
        Console.WriteLine();

        // Create host builder with DI
        var hostBuilder = CreateHostBuilder(args);
        using var host = hostBuilder.Build();

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var pluginLoader = host.Services.GetRequiredService<IAgentPluginLoader>();
        var pluginRegistry = host.Services.GetRequiredService<IAgentPluginRegistry>();

        try
        {
            // Example 1: Load plugin directly
            await LoadPluginDirectlyExample(logger, pluginLoader);
            
            Console.WriteLine();
            
            // Example 2: Simulate Orleans grain host scenario
            await SimulateOrleansHostExample(logger, loggerFactory, pluginLoader, pluginRegistry);
            
            Console.WriteLine();
            
            // Example 3: Plugin hot reload demonstration
            await HotReloadExample(logger, pluginLoader, pluginRegistry);
            
            Console.WriteLine();
            
            // Example 4: Inter-agent communication demonstration
            await InterAgentCommunicationExample(logger, pluginLoader, pluginRegistry);
            
            Console.WriteLine();
            
            // Example 5: Orleans Grain Proxy Generator demonstration
            await ProxyGeneratorExample.RunExampleAsync(loggerFactory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running plugin examples");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register plugin system services
                services.Configure<PluginLoadOptions>(options =>
                {
                    options.EnableHotReload = true;
                    options.IsolateInSeparateContext = true;
                    options.LoadTimeout = TimeSpan.FromSeconds(30);
                });
                
                services.AddSingleton<IAgentPluginLoader, AgentPluginLoader>();
                services.AddSingleton<IAgentPluginRegistry, AgentPluginRegistry>();
                services.AddSingleton<OrleansMethodRouter>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }

    private static async Task LoadPluginDirectlyExample(ILogger logger, IAgentPluginLoader pluginLoader)
    {
        Console.WriteLine("📦 Example 1: Loading Plugin Directly");
        Console.WriteLine("-------------------------------------");

        try
        {
            // Load plugin from current assembly bytes (simulating plugin loading)
            var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
            var plugin = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);

            Console.WriteLine($"✅ Loaded plugin: {plugin.Metadata.Name} v{plugin.Metadata.Version}");
            Console.WriteLine($"   Description: {plugin.Metadata.Description}");

            // Create mock context
            var context = CreateMockAgentContext("weather-agent-001", logger);
            
            // Initialize plugin
            await plugin.InitializeAsync(context);
            Console.WriteLine("✅ Plugin initialized");

            // Test some methods
            var weather = await plugin.ExecuteMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Current weather: {weather}");

            var forecast = await plugin.ExecuteMethodAsync("GetForecast", new object[] { 3 });
            Console.WriteLine($"📅 3-day forecast retrieved: {forecast != null}");

            // Test event handling
            var testEvent = new AgentEvent
            {
                EventType = "WeatherUpdateRequest",
                Data = "Manual update request",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString(),
                SourceAgentId = "test-requester"
            };

            await plugin.HandleEventAsync(testEvent);
            Console.WriteLine("📨 Event handled successfully");

            // Clean up
            await plugin.DisposeAsync();
            Console.WriteLine("🧹 Plugin disposed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in direct plugin loading example");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static async Task SimulateOrleansHostExample(ILogger logger, ILoggerFactory loggerFactory, IAgentPluginLoader pluginLoader, IAgentPluginRegistry registry)
    {
        Console.WriteLine("🎭 Example 2: Simulating Orleans Host Scenario");
        Console.WriteLine("----------------------------------------------");

        try
        {
            // Simulate what PluginGAgentHost would do
            var agentId = "weather-agent-orleans-001";

            // Load plugin (simulating Orleans grain activation)
            var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
            var plugin = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);

            // Register in registry
            registry.RegisterPlugin(agentId, plugin);
            Console.WriteLine($"✅ Plugin registered in registry: {agentId}");

            // Initialize with context
            var context = CreateMockAgentContext(agentId, logger);
            await plugin.InitializeAsync(context);
            Console.WriteLine("✅ Plugin initialized with Orleans context");

            // Simulate Orleans method calls with different attributes
            var methodRouter = new OrleansMethodRouter(loggerFactory.CreateLogger<OrleansMethodRouter>());
            methodRouter.RegisterPlugin(plugin);

            // Test ReadOnly method
            Console.WriteLine($"🔍 Testing ReadOnly method: IsReadOnly = {methodRouter.IsReadOnly("GetCurrentWeather")}");
            var currentWeather = await methodRouter.RouteMethodCallAsync(plugin, "GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Weather from router: {currentWeather != null}");

            // Test method with parameters
            var alerts = await methodRouter.RouteMethodCallAsync(plugin, "GetAlerts", new object[] { true });
            Console.WriteLine($"🚨 Active alerts: {alerts != null}");

            // Test state management
            var state = await plugin.GetStateAsync();
            Console.WriteLine($"💾 Plugin state retrieved: {state != null}");

            // Simulate Orleans grain deactivation
            registry.UnregisterPlugin(agentId);
            Console.WriteLine("🧹 Plugin unregistered from registry");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Orleans host simulation example");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static async Task HotReloadExample(ILogger logger, IAgentPluginLoader pluginLoader, IAgentPluginRegistry registry)
    {
        Console.WriteLine("🔄 Example 3: Hot Reload Demonstration");
        Console.WriteLine("-------------------------------------");

        try
        {
            var agentId = "weather-agent-hotreload-001";

            // Initial load
            var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
            var plugin1 = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);
            
            registry.RegisterPlugin(agentId, plugin1);
            var context = CreateMockAgentContext(agentId, logger);
            await plugin1.InitializeAsync(context);
            
            Console.WriteLine("✅ Initial plugin loaded and registered");

            // Get initial weather
            var weather1 = await plugin1.ExecuteMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Initial weather: {weather1 != null}");

            // Simulate hot reload (unload and reload)
            registry.UnregisterPlugin(agentId);
            await plugin1.DisposeAsync();
            Console.WriteLine("🔄 Plugin unloaded");

            // Load new version (in reality, this would be a different assembly)
            var plugin2 = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);
            registry.RegisterPlugin(agentId, plugin2);
            await plugin2.InitializeAsync(context);
            
            Console.WriteLine("✅ Plugin reloaded");

            // Get weather from reloaded plugin
            var weather2 = await plugin2.ExecuteMethodAsync("GetCurrentWeather", Array.Empty<object>());
            Console.WriteLine($"🌤️  Weather after reload: {weather2 != null}");

            // Clean up
            registry.UnregisterPlugin(agentId);
            await plugin2.DisposeAsync();
            Console.WriteLine("🧹 Hot reload example completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in hot reload example");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static async Task InterAgentCommunicationExample(ILogger logger, IAgentPluginLoader pluginLoader, IAgentPluginRegistry registry)
    {
        Console.WriteLine("🤝 Example 4: Inter-Agent Communication");
        Console.WriteLine("-------------------------------------");

        try
        {
            // Create two weather agents for different cities
            var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
            
            // Agent 1: New York Weather Agent
            var nyAgent = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);
            var nyAgentId = "weather-agent-ny";
            registry.RegisterPlugin(nyAgentId, nyAgent);
            
            var nyContext = CreateMockAgentContext(nyAgentId, logger, new Dictionary<string, object>
            {
                ["Location"] = "New York"
            });
            await nyAgent.InitializeAsync(nyContext);
            MockAgentContext.RegisterPluginInMockRegistry(nyAgentId, nyAgent);
            Console.WriteLine("✅ New York weather agent initialized");

            // Agent 2: London Weather Agent  
            var londonAgent = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);
            var londonAgentId = "weather-agent-london";
            registry.RegisterPlugin(londonAgentId, londonAgent);
            
            var londonContext = CreateMockAgentContext(londonAgentId, logger, new Dictionary<string, object>
            {
                ["Location"] = "London"
            });
            await londonAgent.InitializeAsync(londonContext);
            MockAgentContext.RegisterPluginInMockRegistry(londonAgentId, londonAgent);
            Console.WriteLine("✅ London weather agent initialized");

            // Demonstrate method calls between agents
            Console.WriteLine("\n📞 Testing inter-agent method calls:");
            
            try
            {
                var londonWeather = await nyAgent.ExecuteMethodAsync("RequestWeatherFromAgent", new object[] { londonAgentId });
                Console.WriteLine($"🌤️  NY agent got London weather: {londonWeather != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Method call failed (expected in mock): {ex.Message}");
            }

            // Demonstrate event sending between agents
            Console.WriteLine("\n📨 Testing inter-agent event sending:");
            
            try
            {
                var alertSent = await nyAgent.ExecuteMethodAsync("SendAlertToAgent", 
                    new object[] { londonAgentId, "Storm Warning", "High winds approaching from the west" });
                Console.WriteLine($"🚨 Alert sent from NY to London: {alertSent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Event sending failed (expected in mock): {ex.Message}");
            }

            // Demonstrate batch synchronization
            Console.WriteLine("\n🔄 Testing batch agent synchronization:");
            
            try
            {
                var syncResults = await nyAgent.ExecuteMethodAsync("SyncDataWithAgents", 
                    new object[] { new string[] { londonAgentId, "weather-agent-tokyo" } });
                Console.WriteLine($"📊 Sync completed: {syncResults != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Batch sync failed (expected in mock): {ex.Message}");
            }

            // Test direct event handling
            Console.WriteLine("\n📬 Testing direct event handling:");
            
            var testSyncEvent = new AgentEvent
            {
                EventType = "WeatherDataSync",
                Data = new
                {
                    SourceLocation = "Paris",
                    Temperature = 18.5m,
                    Conditions = "Cloudy",
                    SyncedAt = DateTime.UtcNow
                },
                Timestamp = DateTime.UtcNow,
                SourceAgentId = "weather-agent-paris"
            };

            await londonAgent.HandleEventAsync(testSyncEvent);
            Console.WriteLine("📨 Sync event sent to London agent");

            // Check alerts created by event handling
            var alerts = await londonAgent.ExecuteMethodAsync("GetAlerts", new object[] { true });
            Console.WriteLine($"🚨 London agent alerts: {alerts != null}");

            // Clean up
            registry.UnregisterPlugin(nyAgentId);
            registry.UnregisterPlugin(londonAgentId);
            MockAgentContext.UnregisterPluginFromMockRegistry(nyAgentId);
            MockAgentContext.UnregisterPluginFromMockRegistry(londonAgentId);
            await nyAgent.DisposeAsync();
            await londonAgent.DisposeAsync();
            Console.WriteLine("🧹 Inter-agent communication example completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in inter-agent communication example");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static IAgentContext CreateMockAgentContext(string agentId, ILogger logger, Dictionary<string, object>? customConfig = null)
    {
        return new MockAgentContext(agentId, logger, customConfig);
    }
}

/// <summary>
/// Mock implementation of IAgentContext for testing
/// </summary>
public class MockAgentContext : IAgentContext
{
    private readonly ILogger _logger;
    private static readonly Dictionary<string, IAgentPlugin> _globalRegistry = new();

    public MockAgentContext(string agentId, ILogger logger, Dictionary<string, object>? customConfig = null)
    {
        AgentId = agentId;
        _logger = logger;
        Logger = new MockAgentLogger(logger);
        Configuration = customConfig ?? new Dictionary<string, object>
        {
            ["Location"] = "San Francisco",
            ["UpdateInterval"] = 5,
            ["EnableAlerts"] = true
        };
    }

    public string AgentId { get; }
    public IAgentLogger Logger { get; }
    public IReadOnlyDictionary<string, object> Configuration { get; }

    public async Task PublishEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📤 Publishing event: {EventType} from {AgentId}", 
            agentEvent.EventType, AgentId);
        await Task.Delay(10, cancellationToken); // Simulate network delay
    }

    public async Task<TResponse> PublishEventWithResponseAsync<TResponse>(
        IAgentEvent agentEvent, 
        TimeSpan? timeout = null, 
        CancellationToken cancellationToken = default) where TResponse : class
    {
        _logger.LogInformation("📤➡️ Publishing event with response: {EventType} from {AgentId}", 
            agentEvent.EventType, AgentId);
        await Task.Delay(50, cancellationToken); // Simulate network delay
        
        // Return mock response
        return Activator.CreateInstance<TResponse>();
    }

    public async Task<IAgentReference> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return new MockAgentReference(agentId, _globalRegistry, _logger);
    }

    // Method to register plugins in the global registry for mock inter-agent communication
    public static void RegisterPluginInMockRegistry(string agentId, IAgentPlugin plugin)
    {
        _globalRegistry[agentId] = plugin;
    }

    public static void UnregisterPluginFromMockRegistry(string agentId)
    {
        _globalRegistry.Remove(agentId);
    }

    public async Task RegisterAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 Registering agents: {AgentIds}", string.Join(", ", agentIds));
        await Task.Delay(20, cancellationToken);
    }

    public async Task SubscribeToAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔔 Subscribing to agents: {AgentIds}", string.Join(", ", agentIds));
        await Task.Delay(20, cancellationToken);
    }
}

public class MockAgentLogger : IAgentLogger
{
    private readonly ILogger _logger;

    public MockAgentLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogDebug(string message) => _logger.LogDebug(message);
    public void LogInformation(string message) => _logger.LogInformation(message);
    public void LogWarning(string message) => _logger.LogWarning(message);
    public void LogError(string message, Exception? exception = null) => _logger.LogError(exception, message);
}

public class MockAgentReference : IAgentReference
{
    private readonly Dictionary<string, IAgentPlugin> _registry;
    private readonly ILogger _logger;

    public MockAgentReference(string agentId, Dictionary<string, IAgentPlugin> registry, ILogger logger)
    {
        AgentId = agentId;
        _registry = registry;
        _logger = logger;
    }

    public string AgentId { get; }

    public async Task<TResult> CallMethodAsync<TResult>(string methodName, params object?[] parameters)
    {
        await Task.Delay(20); // Simulate network call
        
        if (_registry.TryGetValue(AgentId, out var plugin))
        {
            try
            {
                _logger.LogDebug("🔗 Calling {MethodName} on agent {AgentId}", methodName, AgentId);
                var result = await plugin.ExecuteMethodAsync(methodName, parameters);
                
                if (result is TResult typedResult)
                {
                    return typedResult;
                }
                
                // Try to convert the result
                if (result != null && typeof(TResult) == typeof(string))
                {
                    return (TResult)(object)result.ToString()!;
                }
                
                return (TResult)Convert.ChangeType(result, typeof(TResult))!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call method {MethodName} on agent {AgentId}", methodName, AgentId);
                throw;
            }
        }
        
        _logger.LogWarning("Agent {AgentId} not found in registry", AgentId);
        return Activator.CreateInstance<TResult>();
    }

    public async Task SendEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network call
        
        if (_registry.TryGetValue(AgentId, out var plugin))
        {
            try
            {
                _logger.LogDebug("📨 Sending {EventType} to agent {AgentId}", agentEvent.EventType, AgentId);
                await plugin.HandleEventAsync(agentEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send event {EventType} to agent {AgentId}", agentEvent.EventType, AgentId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found in registry for event {EventType}", AgentId, agentEvent.EventType);
        }
    }
}