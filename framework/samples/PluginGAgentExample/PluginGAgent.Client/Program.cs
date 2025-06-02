using Aevatar.Core.Abstractions;
using Aevatar.Core.Plugin;
using Aevatar.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginAgentExample;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .UseAevatar(includingAbpServices: true);
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var pluginFactory = host.Services.GetRequiredService<IPluginGAgentFactory>();

logger.LogInformation("!!!Coding in progress!!!");
logger.LogInformation("WeatherAgent Plugin Client starting...");

try
{
    // Create a WeatherAgent plugin instance
    var weatherAgentId = Guid.NewGuid();
    var pluginName = "WeatherAgent";
    var pluginVersion = "1.0.0";
    
    // Configuration for the weather agent
    var weatherAgentConfig = new Dictionary<string, object>
    {
        ["Location"] = "New York",
        ["UpdateInterval"] = "5 minutes",
        ["AlertThreshold"] = 35.0m // Temperature threshold for alerts
    };

    logger.LogInformation("Creating WeatherAgent plugin instance: {AgentId}", weatherAgentId);
    
    var weatherAgent = await pluginFactory.CreatePluginGAgentAsync(
        weatherAgentId, 
        pluginName, 
        pluginVersion, 
        weatherAgentConfig);

    // Get plugin metadata
    var metadata = await weatherAgent.GetPluginMetadataAsync();
    if (metadata != null)
    {
        logger.LogInformation("Plugin loaded: {Name} v{Version} - {Description}", 
            metadata.Name, metadata.Version, metadata.Description);
        
        if (metadata.Properties?.TryGetValue("SupportedLocations", out var locations) == true)
        {
            logger.LogInformation("Supported locations: {Locations}", 
                string.Join(", ", (string[])locations));
        }
    }

    // Demonstrate weather plugin functionality
    await DemonstrateWeatherFunctionality(weatherAgent, logger);
    
    // Demonstrate inter-agent communication
    await DemonstrateInterAgentCommunication(pluginFactory, weatherAgent, logger);

    logger.LogInformation("WeatherAgent Plugin demonstration completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error running WeatherAgent Plugin client");
}

// Helper methods
static async Task DemonstrateWeatherFunctionality(IPluginGAgentHost weatherAgent, ILogger logger)
{
    logger.LogInformation("=== Weather Functionality Demo ===");
    
    try
    {
        // Get current weather
        logger.LogInformation("Getting current weather...");
        var currentWeather = await weatherAgent.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
        if (currentWeather is WeatherInfo weather)
        {
            logger.LogInformation("Current weather in {Location}: {Temperature}°C, {Conditions}, Humidity: {Humidity}%", 
                weather.Location, weather.Temperature, weather.Conditions, weather.Humidity);
            
            if (weather.Alerts.Any())
            {
                logger.LogInformation("Active alerts: {AlertCount}", weather.Alerts.Count);
                foreach (var alert in weather.Alerts)
                {
                    logger.LogInformation("Alert: {Type} - {Message}", alert.Type, alert.Message);
                }
            }
        }

        // Get weather forecast
        logger.LogInformation("Getting 3-day forecast...");
        var forecast = await weatherAgent.CallPluginMethodAsync("GetForecast", new object[] { 3 });
        if (forecast is List<WeatherForecast> forecastList)
        {
            foreach (var day in forecastList)
            {
                logger.LogInformation("Forecast for {Date:yyyy-MM-dd}: {High}°C/{Low}°C, {Conditions}, {Rain}% chance of rain",
                    day.Date, day.HighTemperature, day.LowTemperature, day.Conditions, day.ChanceOfRain);
            }
        }

        // Update location
        logger.LogInformation("Updating location to London...");
        var updateResult = await weatherAgent.CallPluginMethodAsync("UpdateLocation", new object[] { "London" });
        logger.LogInformation("Location update result: {Result}", updateResult);

        // Create a weather alert
        logger.LogInformation("Creating a test weather alert...");
        var alertId = await weatherAgent.CallPluginMethodAsync("CreateAlert", 
            new object[] { "Test Alert", "This is a demonstration alert", 30 });
        logger.LogInformation("Created alert with ID: {AlertId}", alertId);

        // Get alerts
        var alerts = await weatherAgent.CallPluginMethodAsync("GetAlerts", new object[] { true });
        if (alerts is List<WeatherAlert> alertList)
        {
            logger.LogInformation("Active alerts count: {Count}", alertList.Count);
            foreach (var alert in alertList)
            {
                logger.LogInformation("Alert: {Id} - {Type}: {Message}", alert.Id, alert.Type, alert.Message);
            }
        }

        // Get health status
        var healthStatus = await weatherAgent.CallPluginMethodAsync("GetHealthStatus", Array.Empty<object>());
        if (healthStatus is AgentHealthInfo health)
        {
            logger.LogInformation("Agent health: {Status} (Healthy: {IsHealthy}), Active alerts: {AlertCount}", 
                health.Status, health.IsHealthy, health.ActiveAlerts);
        }

        // Start weather monitoring (background task)
        logger.LogInformation("Starting weather monitoring...");
        await weatherAgent.CallPluginMethodAsync("StartWeatherMonitoring", new object[] { 5 });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error demonstrating weather functionality");
    }
}

static async Task DemonstrateInterAgentCommunication(
    IPluginGAgentFactory factory, 
    IPluginGAgentHost primaryAgent, 
    ILogger logger)
{
    logger.LogInformation("=== Inter-Agent Communication Demo ===");
    
    try
    {
        // Create a second weather agent for different location
        var secondAgentId = Guid.NewGuid();
        var secondAgentConfig = new Dictionary<string, object>
        {
            ["Location"] = "Tokyo",
            ["UpdateInterval"] = "10 minutes"
        };

        logger.LogInformation("Creating second WeatherAgent for inter-agent communication...");
        var secondAgent = await factory.CreatePluginGAgentAsync(
            secondAgentId, 
            "WeatherAgent", 
            "1.0.0", 
            secondAgentConfig);

        // Wait a moment for the second agent to initialize
        await Task.Delay(2000);

        // Demonstrate agent-to-agent weather data request
        logger.LogInformation("Requesting weather data from second agent...");
        var remoteWeather = await primaryAgent.CallPluginMethodAsync("RequestWeatherFromAgent", 
            new object[] { secondAgentId });
        
        if (remoteWeather is WeatherInfo weather)
        {
            logger.LogInformation("Received weather from remote agent: {Location} - {Temperature}°C", 
                weather.Location, weather.Temperature);
        }

        // Send alert to second agent
        logger.LogInformation("Sending alert to second agent...");
        var alertSent = await primaryAgent.CallPluginMethodAsync("SendAlertToAgent", 
            new object[] { secondAgentId, "Cross-Agent Alert", "Alert sent from primary agent to secondary agent" });
        logger.LogInformation("Alert sent result: {Result}", alertSent);

        // Synchronize data between agents
        logger.LogInformation("Synchronizing data between agents...");
        var syncResults = await primaryAgent.CallPluginMethodAsync("SyncDataWithAgents", 
            new object[] { new[] { secondAgentId } });
        
        if (syncResults is List<WeatherSyncResult> results)
        {
            foreach (var result in results)
            {
                logger.LogInformation("Sync with {AgentId}: {Success} - {Weather}", 
                    result.AgentId, result.Success ? "Success" : "Failed", 
                    result.RemoteWeather?.Location ?? "No data");
                
                if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    logger.LogWarning("Sync error: {Error}", result.ErrorMessage);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error demonstrating inter-agent communication");
    }
} 