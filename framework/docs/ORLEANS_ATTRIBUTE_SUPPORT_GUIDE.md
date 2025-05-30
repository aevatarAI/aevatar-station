# Orleans Attribute Support Guide

This guide explains the differences between the two proxy generation approaches available in the Aevatar framework and when to use each for optimal Orleans integration.

## Overview

The Aevatar framework provides two methods for generating Orleans grain proxies from plugins:

1. **Reflection.Emit Approach** (Recommended for full Orleans support)
2. **Castle DynamicProxy Approach** (Fallback with limited attribute support)

## Reflection.Emit Approach (Recommended)

### Features
- ✅ **Full Orleans attribute support** with actual `[ReadOnly]`, `[AlwaysInterleave]`, and `[OneWay]` attributes
- ✅ **Runtime optimization** by Orleans framework
- ✅ **True attribute preservation** - Orleans sees actual attributes, not emulated behavior
- ✅ **Better performance** - Orleans can optimize method calls based on actual attributes
- ✅ **Complete interface compliance** - Generated types fully implement grain interfaces

### Usage

```csharp
// Primary approach for Orleans integration
public class OrleansGrainService
{
    private readonly OrleansGrainProxyGenerator _proxyGenerator;

    public TGrainInterface CreateGrain<TGrainInterface>(IAgentPlugin plugin) 
        where TGrainInterface : class
    {
        // Use this method for full Orleans attribute support
        return _proxyGenerator.GenerateGrainImplementation<TGrainInterface>(plugin);
    }
}
```

### Grain Interface Example

```csharp
public interface IWeatherServiceGrain
{
    Task<string> GetCurrentWeatherAsync(string location);

    [ReadOnly] // Actual Orleans attribute - preserved in generated implementation
    Task<decimal> GetTemperatureAsync(string location);

    [AlwaysInterleave] // Allows concurrent execution
    Task StartMonitoringAsync(string location, int intervalMinutes);

    [OneWay] // Fire-and-forget semantics
    Task LogWeatherEventAsync(string eventMessage);
}
```

### Plugin Implementation

```csharp
[AgentPlugin("WeatherService", "1.0.0")]
public class WeatherServicePlugin : AgentPluginBase
{
    // Plugin method attributes are checked for compatibility
    [AgentMethod("GetTemperature", IsReadOnly = true)]
    public async Task<decimal> GetTemperatureAsync(string location)
    {
        // Plugin implementation - no Orleans dependencies
        return await GetWeatherDataAsync(location);
    }

    [AgentMethod("StartMonitoring", AlwaysInterleave = true)]
    public async Task StartMonitoringAsync(string location, int intervalMinutes)
    {
        // Concurrent execution supported
        await StartBackgroundMonitoring(location, intervalMinutes);
    }
}
```

### How It Works

1. **Interface Analysis**: Scans the grain interface for Orleans attributes
2. **Type Generation**: Creates complete implementation using `Reflection.Emit`
3. **Attribute Preservation**: Applies actual Orleans attributes to generated methods
4. **Method Routing**: Routes calls through the plugin system
5. **Optimization**: Orleans can optimize based on actual attributes

## Castle DynamicProxy Approach (Fallback)

### Features
- ⚠️ **Limited Orleans attribute support** - attributes are emulated, not preserved
- ⚠️ **No runtime optimization** by Orleans framework
- ✅ **Simpler implementation** - easier to debug and understand
- ✅ **Good for development** and scenarios where Orleans optimization isn't critical

### When to Use
- Development and testing scenarios
- When Orleans attribute optimization isn't required
- As a fallback when Reflection.Emit fails
- For rapid prototyping

### Usage

```csharp
// Fallback approach - use only when necessary
public TGrainInterface CreateGrainProxy<TGrainInterface>(IAgentPlugin plugin) 
    where TGrainInterface : class
{
    // Limited Orleans attribute support
    return _proxyGenerator.GenerateGrainProxy<TGrainInterface>(plugin);
}
```

## Attribute Compatibility Matrix

| Orleans Attribute | Reflection.Emit | Castle DynamicProxy |
|-------------------|-----------------|---------------------|
| `[ReadOnly]` | ✅ Actual attribute | ⚠️ Emulated behavior |
| `[AlwaysInterleave]` | ✅ Actual attribute | ⚠️ Emulated behavior |
| `[OneWay]` | ✅ Actual attribute | ⚠️ Emulated behavior |
| Runtime Optimization | ✅ Full support | ❌ No optimization |
| Concurrency Control | ✅ Orleans-managed | ⚠️ Application-managed |

## Best Practices

### 1. Choose the Right Approach

```csharp
public class GrainProxyFactory
{
    public TGrainInterface CreateOptimizedGrain<TGrainInterface>(IAgentPlugin plugin)
        where TGrainInterface : class
    {
        try
        {
            // Primary: Use Reflection.Emit for full Orleans support
            return _proxyGenerator.GenerateGrainImplementation<TGrainInterface>(plugin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to Castle DynamicProxy for {InterfaceType}", 
                typeof(TGrainInterface).Name);
            
            // Fallback: Use Castle DynamicProxy
            return _proxyGenerator.GenerateGrainProxy<TGrainInterface>(plugin);
        }
    }
}
```

### 2. Attribute Alignment

Ensure plugin attributes match interface attributes:

```csharp
// Grain Interface
public interface IDataServiceGrain
{
    [ReadOnly]
    Task<string> GetDataAsync(string key);
}

// Plugin Implementation - attributes should match
[AgentMethod("GetData", IsReadOnly = true)] // Matches interface
public async Task<string> GetDataAsync(string key) { ... }
```

### 3. Testing Orleans Attributes

```csharp
[Fact]
public void GeneratedGrain_HasCorrectOrleansAttributes()
{
    var plugin = new TestPlugin();
    var grain = _proxyGenerator.GenerateGrainImplementation<ITestGrain>(plugin);
    
    var grainType = grain.GetType();
    var method = grainType.GetMethod("GetDataAsync");
    
    // Verify actual Orleans attributes are present
    var readOnlyAttr = method.GetCustomAttribute<ReadOnlyAttribute>();
    Assert.NotNull(readOnlyAttr);
}
```

## Performance Implications

### Reflection.Emit Advantages
- **Orleans Optimization**: Orleans can optimize method calls based on actual attributes
- **Concurrency Control**: Orleans manages concurrency for `[ReadOnly]` and `[AlwaysInterleave]` methods
- **Memory Efficiency**: No interception overhead for attribute behavior

### Castle DynamicProxy Limitations
- **No Orleans Optimization**: Orleans cannot optimize based on missing attributes
- **Interception Overhead**: All method calls go through proxy interception
- **Manual Attribute Emulation**: Application must handle attribute behavior

## Migration Path

### Step 1: Update Interface Definitions
```csharp
// Add Orleans attributes to your grain interfaces
public interface IMyServiceGrain
{
    [ReadOnly]
    Task<string> GetReadOnlyDataAsync();
    
    [AlwaysInterleave]
    Task<int> GetConcurrentDataAsync();
}
```

### Step 2: Update Plugin Implementations
```csharp
// Ensure plugin attributes match interface attributes
[AgentMethod("GetReadOnlyData", IsReadOnly = true)]
public async Task<string> GetReadOnlyDataAsync() { ... }
```

### Step 3: Switch to Reflection.Emit
```csharp
// Replace GenerateGrainProxy with GenerateGrainImplementation
var grain = _proxyGenerator.GenerateGrainImplementation<IMyServiceGrain>(plugin);
```

### Step 4: Verify Attribute Preservation
```csharp
// Test that Orleans attributes are correctly applied
var method = grain.GetType().GetMethod("GetReadOnlyDataAsync");
Assert.NotNull(method.GetCustomAttribute<ReadOnlyAttribute>());
```

## Troubleshooting

### Common Issues

1. **Attribute Mismatch Warning**
   ```
   Orleans attribute mismatch between plugin method and interface method
   ```
   **Solution**: Ensure plugin `AgentMethodAttribute` flags match interface Orleans attributes

2. **Method Not Found Error**
   ```
   No corresponding plugin method found for interface method
   ```
   **Solution**: Add `[AgentMethod]` attribute to plugin methods that should be exposed

3. **Type Generation Failure**
   ```
   Failed to generate grain implementation
   ```
   **Solution**: Fall back to Castle DynamicProxy or check interface compatibility

### Debugging Tips

1. **Enable Trace Logging**: Set logging level to `Trace` for detailed generation info
2. **Check Generated Types**: Use debugger to inspect generated type structure
3. **Verify Attributes**: Use reflection to verify Orleans attributes are applied
4. **Test Incrementally**: Start with simple interfaces before adding complex attributes

## Conclusion

For production Orleans deployments where performance and proper attribute support matter, use the **Reflection.Emit approach** (`GenerateGrainImplementation`). This provides full Orleans attribute support with actual `[ReadOnly]`, `[AlwaysInterleave]`, and `[OneWay]` attributes that Orleans can optimize.

Use Castle DynamicProxy approach only as a fallback or for development scenarios where Orleans optimization isn't critical. 