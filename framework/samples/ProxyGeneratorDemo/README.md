# PluginGAgentHost Demo

This demo showcases **Approach 1** from the [agent-development-to-runtime-flow.md](../../docs/proxy/agent-development-to-runtime-flow.md) documentation - using `PluginGAgentHost` to bridge plugins to Orleans.

## Architecture Overview

```
User Plugin (IAgentPlugin) → PluginGAgentHost (GAgentBase) → Orleans Runtime
```

The `PluginGAgentHost` serves as the bridge that:
- Extends `GAgentBase` (providing full Orleans integration)
- Hosts `IAgentPlugin` implementations (user business logic)
- Routes method calls between Orleans and plugins
- Manages plugin lifecycle and state

## Key Benefits

1. **Zero Orleans Dependencies**: Plugins have no Orleans dependencies
2. **Full Orleans Integration**: Complete event sourcing, clustering, persistence
3. **Plugin Lifecycle Management**: Automatic loading, initialization, disposal
4. **State Persistence**: Plugin state managed through Orleans event sourcing
5. **Event Integration**: Orleans events automatically routed to plugins
6. **Hot Reload Support**: Runtime plugin updates without system restart

## Projects Structure

### PluginGAgentHostDemo.Silo
Orleans silo configured with PluginGAgentHost support:
- Registers `IAgentPluginLoader`, `IAgentPluginRegistry`, `IPluginGAgentFactory`
- Configures event sourcing with in-memory storage
- Loads plugin assemblies and makes them available

### PluginGAgentHostDemo.Client
Client that demonstrates using `IPluginGAgentHost` interface:
- Creates plugin agents using `IPluginGAgentFactory`
- Calls plugin methods through `CallPluginMethodAsync`
- Manages plugin state via `GetPluginStateAsync` / `SetPluginStateAsync`
- Demonstrates error handling and hot reload

### ProxyGeneratorDemo.Plugins
Sample plugins with **zero Orleans dependencies**:
- `WeatherServicePlugin`: Weather forecasting with caching and monitoring
- `CalculatorPlugin`: Mathematical operations with complex calculations

## Running the Demo

### Option 1: Using the provided script
```bash
./run-plugingagenthost-demo.sh
```

### Option 2: Manual steps
1. **Build the solution:**
   ```bash
   dotnet build PluginGAgentHostDemo.sln
   ```

2. **Start the silo:**
   ```bash
   cd PluginGAgentHostDemo.Silo
   dotnet run
   ```

3. **In another terminal, run the client:**
   ```bash
   cd PluginGAgentHostDemo.Client
   dotnet run
   ```

## Expected Output

The demo will show:

1. **Plugin Agent Creation**: Using factory to create WeatherService and Calculator agents
2. **Method Execution**: Calling plugin methods through Orleans interfaces
3. **State Management**: Setting and retrieving plugin state
4. **Orleans Attributes**: ReadOnly, OneWay methods working transparently
5. **Error Handling**: Graceful handling of non-existent methods
6. **Hot Reload**: Plugin reload simulation
7. **Metadata Access**: Plugin information retrieval

## Code References

Key classes and their locations:

### Core Framework
- `PluginGAgentHost`: `framework/src/Aevatar.Core/Plugin/PluginGAgentHost.cs`
- `IPluginGAgentHost`: `framework/src/Aevatar.Core/Plugin/IPluginGAgentHost.cs`
- `IAgentPlugin`: `framework/src/Aevatar.Core.Abstractions/Plugin/IAgentPlugin.cs`
- `AgentPluginBase`: `framework/src/Aevatar.Core.Abstractions/Plugin/AgentPluginBase.cs`

### Demo Implementation
- Silo: `PluginGAgentHostDemo.Silo/Program.cs`
- Client: `PluginGAgentHostDemo.Client/Program.cs`
- Weather Plugin: `ProxyGeneratorDemo.Plugins/WeatherServicePlugin.cs`
- Calculator Plugin: `ProxyGeneratorDemo.Plugins/CalculatorPlugin.cs`

## Comparison with Proxy Generator Approach

| Aspect | PluginGAgentHost | Orleans Proxy Generator |
|--------|------------------|------------------------|
| **Integration** | Full GAgent with Orleans features | Interface-based proxy routing |
| **Event Sourcing** | Built-in via GAgentBase | External implementation needed |
| **State Management** | Orleans persistence | Manual state handling |
| **Performance** | Direct grain calls | Proxy interception overhead |
| **Flexibility** | GAgent patterns | Any grain interface |
| **Use Case** | Full Orleans integration | Interface migration |

## Success Criteria

✅ **Plugins have zero Orleans dependencies**  
✅ **Full Orleans integration (clustering, persistence, events)**  
✅ **Type-safe method calling**  
✅ **State management through Orleans**  
✅ **Plugin metadata access**  
✅ **Hot reload capability**  
✅ **Robust error handling**  
✅ **Multiple plugin instances**

## Validation

This demo validates that `PluginGAgentHost` successfully:
- Bridges `IAgentPlugin` (user's business logic) with `GAgentBase` (Orleans + event sourcing)
- Provides complete decoupling without sacrificing Orleans features
- Enables plugin-based architecture with enterprise-grade reliability