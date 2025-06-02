# WeatherAgent Plugin Client

This client project demonstrates how to interact with the `WeatherAgentPlugin` through the Aevatar plugin system. It showcases the full capabilities of plugin-based agents including initialization, method calls, and inter-agent communication.

## Prerequisites

1. .NET 9.0 SDK
2. Running PluginGAgent.Silo (the Orleans silo hosting the WeatherAgent plugins)

## Architecture Overview

The client uses the following key components:
- **IPluginGAgentFactory**: Creates and initializes plugin-based agents
- **IPluginGAgentHost**: Provides the Orleans grain interface for plugin interaction
- **WeatherAgentPlugin**: The actual plugin implementation with weather functionality

## Features Demonstrated

### Single Agent Operations
- **Plugin Creation**: Creating WeatherAgent instances with custom configurations
- **Metadata Access**: Retrieving plugin information and capabilities
- **Weather Data**: Getting current weather and forecasts
- **Location Management**: Updating agent location settings
- **Alert System**: Creating, managing, and dismissing weather alerts
- **Health Monitoring**: Checking agent health status
- **Background Tasks**: Starting weather monitoring services

### Inter-Agent Communication
- **Multi-Agent Setup**: Creating multiple WeatherAgent instances
- **Data Sharing**: Requesting weather data from remote agents
- **Event Broadcasting**: Sending alerts between agents
- **State Synchronization**: Synchronizing data across multiple agents
- **Error Handling**: Graceful handling of communication failures

## Usage

### 1. Start the Silo
First, ensure the PluginGAgent.Silo is running:
```bash
cd ../PluginGAgent.Silo
dotnet run
```

### 2. Run the Client
```bash
cd framework/samples/PluginGAgentExample/PluginGAgent.Client
dotnet run
```

## Configuration

The client demonstrates different configuration scenarios:

### Basic Configuration
```csharp
var weatherAgentConfig = new Dictionary<string, object>
{
    ["Location"] = "New York",
    ["UpdateInterval"] = "5 minutes",
    ["AlertThreshold"] = 35.0m
};
```

### Agent Creation
```csharp
var weatherAgent = await pluginFactory.CreatePluginGAgentAsync(
    "weather-agent-001",    // Agent ID
    "WeatherAgent",         // Plugin name
    "1.0.0",               // Plugin version
    weatherAgentConfig);    // Configuration
```

## Key Methods Demonstrated

### Weather Operations
- `GetCurrentWeather()`: Retrieve current weather conditions
- `GetForecast(days)`: Get weather forecast for specified days
- `UpdateLocation(location)`: Change the agent's location
- `CreateAlert(type, message, duration)`: Create weather alerts
- `GetAlerts(activeOnly)`: Retrieve current alerts
- `GetHealthStatus()`: Check agent health

### Inter-Agent Methods
- `RequestWeatherFromAgent(agentId)`: Request data from another agent
- `SendAlertToAgent(agentId, type, message)`: Send alerts to other agents
- `SyncDataWithAgents(agentIds)`: Synchronize with multiple agents

## Example Output

```
WeatherAgent Plugin Client starting...
Creating WeatherAgent plugin instance: weather-agent-001
Plugin loaded: WeatherAgent v1.0.0 - Weather monitoring and forecasting agent
Supported locations: New York, London, Tokyo, Sydney

=== Weather Functionality Demo ===
Getting current weather...
Current weather in New York: 21.2°C, Partly Cloudy, Humidity: 68%
Getting 3-day forecast...
Forecast for 2024-01-15: 23°C/18°C, Sunny, 15% chance of rain
...

=== Inter-Agent Communication Demo ===
Creating second WeatherAgent for inter-agent communication...
Requesting weather data from second agent...
Received weather from remote agent: Tokyo - 8.5°C
...
```

## Error Handling

The client includes comprehensive error handling for:
- Plugin loading failures
- Method call exceptions
- Inter-agent communication errors
- Network timeouts
- Configuration issues

## Logging

All operations are logged with structured logging including:
- Plugin lifecycle events
- Method call results
- Error conditions
- Inter-agent communication status

## Best Practices Demonstrated

1. **SOLID Principles**: Clean separation of concerns
2. **Proper Logging**: Structured logging at important checkpoints
3. **Error Handling**: Graceful handling of various failure scenarios
4. **Resource Management**: Proper disposal of Orleans client resources
5. **Configuration Management**: Flexible agent configuration
6. **Scalability**: Multi-agent scenarios for scalability testing

## Dependencies

- **Aevatar.Core.Abstractions**: Core agent abstractions
- **Aevatar**: Main Aevatar framework
- **PluginGAgent.Grains**: WeatherAgent plugin implementation
- **Microsoft.Orleans.Client**: Orleans client libraries
- **Microsoft.Extensions.Hosting**: .NET hosting framework
- **Microsoft.Extensions.Logging**: Logging infrastructure

## Related Projects

- `PluginGAgent.Silo`: The Orleans silo hosting the plugins
- `PluginGAgent.Grains`: The WeatherAgent plugin implementation
- `WeatherAgentPlugin.cs`: The actual plugin with weather functionality 