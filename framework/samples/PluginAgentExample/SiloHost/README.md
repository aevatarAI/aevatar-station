# Orleans Plugin System Demo - SiloHost

This project demonstrates a complete Orleans Silo implementation that loads and executes plugins through the Aevatar plugin system, enabling version-independent agent development.

## 🏗️ Architecture Overview

The plugin system provides a clean separation between Orleans infrastructure and business logic:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐    ┌─────────────────┐
│   Orleans       │    │   Plugin System  │    │  WeatherAgentPlugin │    │   Orleans       │
│   Silo Host     │───▶│   Components     │───▶│  (Zero Dependencies)│◀───│   Clients       │
└─────────────────┘    └──────────────────┘    └─────────────────────┘    └─────────────────┘
```

### Key Components

1. **WeatherAgentPlugin.cs** - Business logic with zero Orleans dependencies
2. **AgentPluginLoader.cs** - Loads plugins from assemblies
3. **PluginGAgentHost.cs** - Orleans grain that wraps plugins
4. **OrleansMethodRouter.cs** - Routes method calls between Orleans and plugins
5. **SiloHost** - Orleans Silo with integrated plugin system

## 🎯 Key Benefits

- **Version Independence**: Plugins have zero Orleans dependencies
- **Full Orleans Features**: Clustering, persistence, state management, etc.
- **Type Safety**: Compile-time checking with runtime flexibility
- **Hot Reload**: Plugin updates without silo restart
- **External Access**: Standard Orleans grain interfaces for clients
- **Scalability**: Distributed across Orleans cluster

## 🔧 Plugin Methods

The WeatherAgentPlugin exposes these methods through the Orleans grain interface:

- `GetCurrentWeather()` - Get current weather data
- `GetForecast(int days)` - Get weather forecast
- `UpdateLocation(string location)` - Update weather location
- `CreateAlert(string condition, double threshold)` - Create weather alert
- `GetAlerts()` - Get active alerts
- `GetHealthStatus()` - Get plugin health status

## 🚀 Usage Examples

### Starting the Silo

```csharp
// The silo automatically loads and registers the plugin system
var host = await CreateHostBuilder(args).Build();
await host.StartAsync();
```

### Client Usage

```csharp
// Connect to the Orleans cluster
var client = new ClientBuilder()
    .UseLocalhostClustering()
    .Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "PluginAgentService";
    })
    .Build();

await client.Connect();

// Get the plugin grain
var grain = client.GetGrain<IPluginGAgentHost>("weather-plugin");

// Call plugin methods
var weather = await grain.CallPluginMethodAsync("GetCurrentWeather", Array.Empty<object>());
var forecast = await grain.CallPluginMethodAsync("GetForecast", new object[] { 7 });
var alerts = await grain.CallPluginMethodAsync("GetAlerts", Array.Empty<object>());
```

### Plugin State Management

```csharp
// Update plugin location
await grain.CallPluginMethodAsync("UpdateLocation", new object[] { "San Francisco, CA" });

// Create weather alerts
await grain.CallPluginMethodAsync("CreateAlert", new object[] { "temperature", 85.0 });

// Check plugin health
var health = await grain.CallPluginMethodAsync("GetHealthStatus", Array.Empty<object>());
```

## 📊 Testing Scenarios

The implementation supports comprehensive testing:

1. **Basic Plugin Method Calls**
   - All WeatherAgentPlugin methods accessible through Orleans
   - Type-safe parameter passing and return values

2. **Plugin State Management**
   - Location updates persist across calls
   - Alert creation and retrieval
   - Health status monitoring

3. **Plugin Hot Reload**
   - Update plugin assembly without silo restart
   - Automatic reloading of plugin methods

4. **Inter-Agent Communication**
   - Multiple plugin instances can communicate
   - Shared state through Orleans persistence

5. **External Client Connectivity**
   - Standard Orleans client can access plugin methods
   - Full clustering and load balancing support

## 🔧 Configuration

### Silo Configuration

```csharp
.UseOrleans((context, siloBuilder) =>
{
    siloBuilder
┌─────────────────────────────────────────────────────────────┐
│                    ORLEANS SILO HOST                       │
│                                                             │
│  ┌─────────────────┐    ┌─────────────────────────────────┐ │
│  │  Orleans Silo   │    │      Plugin System              │ │
│  │                 │    │                                 │ │
│  │ • Grain Host    │◄───┤ • AgentPluginLoader             │ │
│  │ • Clustering    │    │ • PluginGAgentHost (Grain)      │ │
│  │ • Persistence   │    │ • OrleansMethodRouter           │ │
│  │ • Dashboard     │    │ • AgentPluginRegistry           │ │
│  └─────────────────┘    └─────────────────────────────────┘ │
│                                   │                         │
│                                   ▼                         │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              WeatherAgentPlugin                         │ │
│  │                                                         │ │
│  │ • Zero Orleans dependencies                             │ │
│  │ • Pure C# business logic                                │ │
│  │ • AgentMethod attributes                                │ │
│  │ • Event handling                                        │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────┐
│                   ORLEANS CLIENTS                          │
│                                                             │
│ • External applications                                     │
│ • Call plugin methods through Orleans grain interface      │
│ • Full Orleans benefits (clustering, persistence, etc.)    │
└─────────────────────────────────────────────────────────────┘
```

## Key Features

✅ **Plugin Loading**: WeatherAgentPlugin loaded through AgentPluginLoader  
✅ **Orleans Integration**: Plugin exposed as Orleans grain via PluginGAgentHost  
✅ **Method Routing**: Plugin methods callable through Orleans grain interface  
✅ **State Management**: Plugin state managed by Orleans persistence  
✅ **Hot Reload**: Plugins can be reloaded without restarting the silo  
✅ **External Clients**: External Orleans clients can call plugin methods  

## Prerequisites

- .NET 8.0 SDK
- Build the parent PluginAgentExample project first

## Building and Running

### 1. Build the Projects

```bash
# From the PluginAgentExample directory
cd framework/samples/PluginAgentExample
dotnet build

# Build the SiloHost project
cd SiloHost
dotnet build
```

### 2. Run the Orleans Silo

```bash
# Start the Orleans silo with plugin system
dotnet run
```

Expected output:
```
🚀 Plugin Agent Orleans Silo Host
=================================
✅ Orleans Silo started successfully
🔌 Plugin system is ready to load agents

📦 Loading WeatherAgentPlugin...
✅ Loaded plugin: WeatherAgent v1.0.0
   Description: Weather monitoring and forecasting agent
✅ Created PluginGAgent: weather-orleans-001

🔧 Testing grain method calls...
🌤️  Current weather: True
📅 3-day forecast: True
📍 Location updated: True
💾 Plugin state retrieved: True
ℹ️  Plugin metadata: WeatherAgent v1.0.0

📞 Testing external Orleans client calls...
🔗 Got grain reference from Orleans client
🌤️  Orleans client weather call: True
🚨 Orleans client alerts call: True
📨 Created alert via Orleans client: [alert-id]
✅ All Orleans client calls completed successfully

Press Enter to exit...
```

### 3. Run External Orleans Client (Optional)

In a separate terminal:

```bash
# Run the standalone Orleans client
dotnet run -- --client
```

This demonstrates how external applications can connect to the silo and call plugin methods.

## How It Works

### 1. Plugin Loading Flow

```csharp
// 1. Load plugin from assembly bytes
var assemblyBytes = File.ReadAllBytes(typeof(WeatherAgentPlugin).Assembly.Location);
var plugin = await pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(WeatherAgentPlugin).FullName);

// 2. Create PluginGAgentHost (Orleans grain that wraps the plugin)
var pluginGAgent = await pluginFactory.CreatePluginGAgentAsync(agentId, pluginName, pluginVersion);

// 3. Plugin is now accessible as Orleans grain
var weather = await pluginGAgent.CallMethodAsync("GetCurrentWeather", Array.Empty<object>());
```

### 2. Orleans Grain Integration

The `PluginGAgentHost` acts as a bridge between Orleans and the plugin:

```csharp
[GAgent]
public class PluginGAgentHost : GAgentBase<PluginAgentState, PluginStateLogEvent>
{
    // Routes Orleans method calls to plugin methods
    public async Task<object?> CallMethodAsync(string methodName, object?[] parameters)
    {
        return await _plugin.ExecuteMethodAsync(methodName, parameters);
    }
}
```

### 3. Plugin Method Mapping

Plugin methods are automatically exposed through Orleans:

```csharp
// Plugin method (zero Orleans dependencies)
[AgentMethod("GetCurrentWeather", IsReadOnly = true)]
public async Task<WeatherInfo> GetCurrentWeatherAsync() { ... }

// Callable through Orleans grain
var weather = await grain.CallMethodAsync("GetCurrentWeather", Array.Empty<object>());
```

## Testing Scenarios

### 1. Basic Method Calls
- `GetCurrentWeather` - ReadOnly method
- `GetForecast` - Method with parameters
- `UpdateLocation` - State-changing method
- `CreateAlert` - Method that creates data
- `GetAlerts` - Method that retrieves data

### 2. Plugin State Management
- Plugin state persistence through Orleans
- State retrieval and modification
- Plugin metadata access

### 3. Hot Reload
- Plugin can be reloaded without silo restart
- State is preserved during reload
- Methods continue to work after reload

### 4. External Client Connectivity
- External Orleans clients can connect to silo
- Plugin methods are callable from external clients
- Full Orleans clustering and load balancing support

## Configuration

The silo is configured with:

```csharp
services.Configure<PluginLoadOptions>(options =>
{
    options.EnableHotReload = true;
    options.IsolateInSeparateContext = false; // Simplified for demo
    options.LoadTimeout = TimeSpan.FromSeconds(30);
});
```

## Orleans Features

- **Clustering**: Uses localhost clustering for simplicity
- **Dashboard**: Orleans dashboard available at http://localhost:8080
- **Persistence**: Plugin state persisted through Orleans
- **Application Parts**: Grain assemblies automatically registered

## Benefits Demonstrated

1. **Version Independence**: Plugin has zero Orleans dependencies
2. **Orleans Integration**: Full Orleans features available (clustering, persistence, etc.)
3. **Type Safety**: Strongly typed method calls with compile-time checking
4. **Hot Reload**: Plugins can be updated without system restart
5. **External Access**: External applications can interact with plugins through Orleans
6. **Scalability**: Orleans clustering enables horizontal scaling
7. **State Management**: Plugin state automatically persisted by Orleans

## Troubleshooting

### Common Issues

1. **"Assembly not found"** - Ensure the parent project is built first
2. **"Connection failed"** - Make sure no other Orleans silo is running on the same ports
3. **"Plugin type not found"** - Verify the WeatherAgentPlugin assembly is accessible

### Logs

Enable detailed logging by setting the log level:

```csharp
logging.SetMinimumLevel(LogLevel.Debug);
```

## Next Steps

1. **Multiple Plugins**: Load different plugin types
2. **Real Persistence**: Configure SQL Server or other persistence providers
3. **Production Clustering**: Use real clustering (Azure, AWS, or on-premises)
4. **Plugin Store**: Implement a plugin repository/store
5. **Security**: Add authentication and authorization for plugin access

This demonstrates the full plugin system working within Orleans, providing version-independent agent development with full Orleans capabilities. 