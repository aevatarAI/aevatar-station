# Plugin Agent Example

This example demonstrates how to create version-independent agents using the plugin system. The plugin approach completely eliminates dependencies on Orleans or GAgentBase, allowing developers to create agents that work with any version of Aevatar Station.

## Key Benefits

🔹 **Zero Orleans Dependencies**: Agents are pure C# with no Orleans references  
🔹 **Version Independent**: Works with any station version without recompilation  
🔹 **Orleans Features Preserved**: Full support for attributes like `[ReadOnly]`, `[AlwaysInterleave]`  
🔹 **Hot Reload Support**: Plugins can be reloaded without restarting the station  
🔹 **Type Safety**: Strong typing with compile-time checking  

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      DEVELOPER PACKAGE                         │
│  ┌─────────────────┐    ┌──────────────────────────────────────┐ │
│  │  Plugin SDK     │    │       WeatherAgentPlugin            │ │
│  │                 │    │                                      │ │
│  │ • IAgentPlugin  │    │ • Pure C# business logic            │ │
│  │ • Attributes    │    │ • Weather data management           │ │
│  │ • AgentContext  │    │ • Event handling                    │ │
│  │ • No Orleans!   │    │ • Zero Orleans dependencies         │ │
│  └─────────────────┘    └──────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                      │
                                      │ Plugin Loading
                                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AEVATAR STATION                             │
│  ┌─────────────────┐    ┌──────────────────────────────────────┐ │
│  │ PluginGAgentHost│    │         Plugin System               │ │
│  │                 │    │                                      │ │
│  │ • GAgentBase    │◄───┤ • AgentPluginLoader                 │ │
│  │ • Orleans       │    │ • OrleansMethodRouter               │ │
│  │ • State mgmt    │    │ • AgentContext                      │ │
│  │ • Event streams │    │ • Attribute mapping                 │ │
│  └─────────────────┘    └──────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Weather Agent Example

The `WeatherAgentPlugin` demonstrates a complete agent implementation with:

### Features
- **Current Weather**: Get real-time weather data
- **Forecasting**: Multi-day weather forecasts  
- **Alerts**: Create and manage weather alerts
- **Location Management**: Update monitoring location
- **Health Monitoring**: Agent health status and metrics

### Method Examples

```csharp
// ReadOnly method - Orleans optimizes for concurrent access
[AgentMethod("GetCurrentWeather", IsReadOnly = true)]
public async Task<WeatherInfo> GetCurrentWeatherAsync()

// Interleaving method - Can run concurrently with other methods
[AgentMethod("StartWeatherMonitoring", AlwaysInterleave = true)]
public async Task StartWeatherMonitoringAsync(int intervalMinutes = 5)

// Standard method - Exclusive access
[AgentMethod("UpdateLocation")]
public async Task<bool> UpdateLocationAsync(string newLocation)
```

### Event Handling

```csharp
// Specific event handler
[AgentEventHandler("WeatherUpdateRequest")]
public async Task HandleWeatherUpdateRequestAsync(IAgentEvent agentEvent)

// Generic event handler for unhandled events
[AgentEventHandler]
public async Task HandleGenericEventAsync(IAgentEvent agentEvent)
```

## Running the Example

```bash
cd framework/samples/PluginAgentExample
dotnet run
```

The example runs three demonstrations:

### 1. Direct Plugin Loading
Shows how to load and interact with a plugin directly without Orleans infrastructure.

### 2. Orleans Host Simulation  
Demonstrates how the plugin system integrates with Orleans through `PluginGAgentHost` and `OrleansMethodRouter`.

### 3. Hot Reload
Shows how plugins can be unloaded and reloaded without system restart.

## Sample Output

```
🌤️  Weather Agent Plugin Example
=================================
This example demonstrates a plugin-based agent with zero Orleans dependencies.

📦 Example 1: Loading Plugin Directly
-------------------------------------
✅ Loaded plugin: WeatherAgent v1.0.0
   Description: Weather monitoring and forecasting agent
✅ Plugin initialized
🌤️  Current weather: WeatherInfo { ... }
📅 3-day forecast retrieved: True
📨 Event handled successfully
🧹 Plugin disposed

🎭 Example 2: Simulating Orleans Host Scenario
----------------------------------------------
✅ Plugin registered in registry: weather-agent-orleans-001
✅ Plugin initialized with Orleans context
🔍 Testing ReadOnly method: IsReadOnly = True
🌤️  Weather from router: True
🚨 Active alerts: True
💾 Plugin state retrieved: True
🧹 Plugin unregistered from registry

🔄 Example 3: Hot Reload Demonstration
-------------------------------------
✅ Initial plugin loaded and registered
🌤️  Initial weather: True
🔄 Plugin unloaded
✅ Plugin reloaded
🌤️  Weather after reload: True
🧹 Hot reload example completed
```

## Developer Workflow

### 1. Create Plugin Project
```bash
dotnet new classlib -n MyWeatherPlugin
cd MyWeatherPlugin
dotnet add reference Aevatar.Core.Abstractions
```

### 2. Implement Plugin
```csharp
[AgentPlugin("MyWeatherAgent", "1.0.0")]
public class MyWeatherPlugin : AgentPluginBase
{
    [AgentMethod("GetWeather", IsReadOnly = true)]
    public async Task<string> GetWeatherAsync()
    {
        return "Sunny, 25°C";
    }
}
```

### 3. Build and Deploy
```bash
dotnet build
# Copy DLL to station's plugin directory
# Station automatically loads and manages the plugin
```

### 4. Use from Orleans Clients
```csharp
// Station creates PluginGAgentHost automatically
var weather = grainFactory.GetGrain<IWeatherGrain>(grainId);
var currentWeather = await weather.GetWeather();
```

## Migration from GAgentBase

### Before (Tightly Coupled)
```csharp
public class OldWeatherAgent : GAgentBase<WeatherState, WeatherEvent>
{
    // Directly inherits from GAgentBase
    // Must be recompiled when GAgentBase changes
}
```

### After (Plugin-Based)
```csharp
[AgentPlugin("WeatherAgent", "1.0.0")]
public class NewWeatherAgent : AgentPluginBase
{
    // Zero GAgentBase dependency
    // Works with any station version
}
```

## Key Advantages

1. **Future Proof**: Agents work with future station versions
2. **Independent Deployment**: Update agents without station updates  
3. **Orleans Features**: Full attribute and interface support
4. **Hot Reload**: Update running agents without restart
5. **Type Safety**: Compile-time checking and IntelliSense
6. **Easy Testing**: Pure C# with mockable dependencies

This approach solves the version compatibility problem while preserving all Orleans capabilities and providing an excellent developer experience.