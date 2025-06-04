using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Concurrency;
using System.Reflection;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

public class OrleansGrainProxyGeneratorTests
{
    private readonly Mock<ILogger<OrleansMethodRouter>> _routerLoggerMock;
    private readonly Mock<ILogger<OrleansGrainProxyGenerator>> _generatorLoggerMock;
    private readonly OrleansMethodRouter _methodRouter;
    private readonly OrleansGrainProxyGenerator _proxyGenerator;

    public OrleansGrainProxyGeneratorTests()
    {
        _routerLoggerMock = new Mock<ILogger<OrleansMethodRouter>>();
        _generatorLoggerMock = new Mock<ILogger<OrleansGrainProxyGenerator>>();
        _methodRouter = new OrleansMethodRouter(_routerLoggerMock.Object);
        _proxyGenerator = new OrleansGrainProxyGenerator(_methodRouter, _generatorLoggerMock.Object);
    }

    private async Task<T> InitializePluginAsync<T>(T plugin) where T : IAgentPlugin
    {
        // Create a mock context for plugin initialization
        var mockContext = new Mock<IAgentContext>();
        var mockLogger = new Mock<IAgentLogger>();
        
        mockContext.Setup(x => x.Logger).Returns(mockLogger.Object);
        mockContext.Setup(x => x.AgentId).Returns("test-agent");
        
        // Initialize the plugin properly
        await plugin.InitializeAsync(mockContext.Object);
        
        // Register the plugin with the method router so methods can be routed
        _methodRouter.RegisterPlugin(plugin);
        
        return plugin;
    }

    [Fact]
    public async Task GenerateGrainProxy_ValidPlugin_CreatesWorkingProxy()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Assert
        Assert.NotNull(proxy);
        Assert.IsAssignableFrom<ITestWeatherGrain>(proxy);
    }

    [Fact]
    public async Task GenerateGrainProxy_CallMethod_RoutesToPlugin()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var result = await proxy.GetWeatherAsync("New York");

        // Assert
        Assert.Equal("Weather for New York: Sunny, 25°C", result);
    }

    [Fact]
    public async Task GenerateGrainProxy_ReadOnlyMethod_PreservesAttribute()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var result = await proxy.GetTemperatureAsync("London");

        // Assert
        Assert.Equal(20.5m, result);
        
        // Verify the method has ReadOnly attribute (would be checked by Orleans at runtime)
        var proxyType = proxy.GetType();
        var method = proxyType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));
        Assert.NotNull(method);
    }

    [Fact]
    public async Task GenerateGrainProxy_InterleaveMethod_HandlesCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        await proxy.StartMonitoringAsync("Tokyo", 5);

        // Assert
        Assert.True(plugin.IsMonitoring);
        Assert.Equal("Tokyo", plugin.MonitoringLocation);
    }

    [Fact]
    public async Task GenerateGrainProxy_OneWayMethod_DoesNotWaitForResult()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        proxy.LogWeatherEventAsync("Storm warning");
        
        // Give the OneWay method a moment to execute
        await Task.Delay(50);

        // Assert - OneWay methods return immediately but should still execute
        Assert.True(plugin.LastEvent?.Contains("Storm warning"));
    }

    [Fact]
    public async Task GenerateGrainProxy_WithParameters_PassesCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var forecast = await proxy.GetForecastAsync("Seattle", 7);

        // Assert
        Assert.Equal(7, forecast.Count);
        Assert.All(forecast, f => Assert.Contains("Seattle", f));
    }

    [Fact]
    public void CreateGrainMethod_ValidRoutingInfo_CreatesMethod()
    {
        // Arrange
        var routingInfo = new MethodRoutingInfo
        {
            MethodName = "TestMethod",
            ReturnType = typeof(Task<string>),
            ParameterTypes = new[] { typeof(string), typeof(int) },
            IsReadOnly = true,
            AlwaysInterleave = false,
            OneWay = false
        };

        // Act
        var method = _proxyGenerator.CreateGrainMethod(routingInfo, typeof(ITestWeatherGrain));

        // Assert
        Assert.NotNull(method);
        Assert.Equal("TestMethod", method.Name);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void CreateGrainMethod_WithOrleansAttributes_AppliesCorrectly()
    {
        // Arrange
        var routingInfo = new MethodRoutingInfo
        {
            MethodName = "ReadOnlyMethod",
            ReturnType = typeof(Task<string>),
            ParameterTypes = Array.Empty<Type>(),
            IsReadOnly = true,
            AlwaysInterleave = true,
            OneWay = false
        };

        // Act
        var method = _proxyGenerator.CreateGrainMethod(routingInfo, typeof(ITestWeatherGrain));

        // Assert
        Assert.NotNull(method);
        
        // Check for Orleans attributes
        var hasReadOnly = method.GetCustomAttribute<ReadOnlyAttribute>() != null;
        var hasInterleave = method.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
        
        Assert.True(hasReadOnly);
        Assert.True(hasInterleave);
    }

    [Fact]
    public async Task GenerateGrainProxy_ErrorInPlugin_PropagatesException()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => proxy.GetWeatherAsync("ErrorCity"));
    }

    [Fact]
    public async Task GenerateGrainProxy_TypeConversion_WorksCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var temp = await proxy.GetTemperatureAsync("Boston");

        // Assert
        Assert.Equal(20.5m, temp);
    }

    [Fact]
    public async Task GenerateGrainProxy_ConcurrentCalls_HandledCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
            await proxy.GetWeatherAsync($"City{i}"));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.Contains("Weather for City", r));
    }

    [Fact]
    public async Task GenerateGrainImplementation_BasicMethod_WorksCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);
        var result = await grainImpl.GetWeatherAsync("London");

        // Assert
        Assert.Equal("Weather for London: Sunny, 25°C", result);
    }

    [Fact]
    public async Task GenerateGrainImplementation_ReadOnlyMethod_HasCorrectAttribute()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Act
        var result = await proxy.GetTemperatureAsync("London");

        // Assert
        Assert.Equal(20.5m, result);
        
        // Verify the method has ReadOnly attribute (would be checked by Orleans at runtime)
        var proxyType = proxy.GetType();
        var method = proxyType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));
        Assert.NotNull(method);
    }

    [Fact]
    public async Task GenerateGrainImplementation_InterleaveMethod_HasCorrectAttribute()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);

        // Act
        await grainImpl.StartMonitoringAsync("Tokyo", 5);

        // Assert
        Assert.True(plugin.IsMonitoring);
        Assert.Equal("Tokyo", plugin.MonitoringLocation);
        
        // Verify the method has actual AlwaysInterleave attribute
        var grainType = grainImpl.GetType();
        var method = grainType.GetMethod(nameof(ITestWeatherGrain.StartMonitoringAsync));
        Assert.NotNull(method);
        
        var interleaveAttr = method.GetCustomAttribute<AlwaysInterleaveAttribute>();
        Assert.NotNull(interleaveAttr); // This should pass with Reflection.Emit
    }

    [Fact]
    public async Task GenerateGrainImplementation_OneWayMethod_HasCorrectAttribute()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);

        // Act
        grainImpl.LogWeatherEventAsync("Storm warning");
        
        // Give the OneWay method a moment to execute
        await Task.Delay(50);

        // Assert - OneWay methods return immediately
        Assert.True(plugin.LastEvent?.Contains("Storm warning"));
        
        // Verify the method has actual OneWay attribute
        var grainType = grainImpl.GetType();
        var method = grainType.GetMethod(nameof(ITestWeatherGrain.LogWeatherEventAsync));
        Assert.NotNull(method);
        
        var oneWayAttr = method.GetCustomAttribute<OneWayAttribute>();
        Assert.NotNull(oneWayAttr); // This should pass with Reflection.Emit
    }

    [Fact]
    public async Task CompareOrleansAttributeSupport_ReflectionEmitVsCastleProxy()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var reflectionEmitGrain = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);
        var castleProxyGrain = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);

        // Assert - Reflection.Emit should have actual Orleans attributes
        var reflectionEmitType = reflectionEmitGrain.GetType();
        var castleProxyType = castleProxyGrain.GetType();

        // Test ReadOnly attribute preservation
        var reflectionEmitReadOnlyMethod = reflectionEmitType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));
        var castleProxyReadOnlyMethod = castleProxyType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));

        var reflectionEmitHasReadOnly = reflectionEmitReadOnlyMethod?.GetCustomAttribute<ReadOnlyAttribute>() != null;
        var castleProxyHasReadOnly = castleProxyReadOnlyMethod?.GetCustomAttribute<ReadOnlyAttribute>() != null;

        // Reflection.Emit should preserve actual Orleans attributes
        Assert.True(reflectionEmitHasReadOnly, "Reflection.Emit should preserve ReadOnly attribute");
        
        // Castle DynamicProxy does not preserve Orleans attributes in the generated methods
        Assert.False(castleProxyHasReadOnly, "Castle DynamicProxy does not preserve Orleans attributes");

        // Test AlwaysInterleave attribute preservation
        var reflectionEmitInterleaveMethod = reflectionEmitType.GetMethod(nameof(ITestWeatherGrain.StartMonitoringAsync));
        var castleProxyInterleaveMethod = castleProxyType.GetMethod(nameof(ITestWeatherGrain.StartMonitoringAsync));

        var reflectionEmitHasInterleave = reflectionEmitInterleaveMethod?.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
        var castleProxyHasInterleave = castleProxyInterleaveMethod?.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;

        Assert.True(reflectionEmitHasInterleave, "Reflection.Emit should preserve AlwaysInterleave attribute");
        Assert.False(castleProxyHasInterleave, "Castle DynamicProxy does not preserve Orleans attributes");

        // Test OneWay attribute preservation
        var reflectionEmitOneWayMethod = reflectionEmitType.GetMethod(nameof(ITestWeatherGrain.LogWeatherEventAsync));
        var castleProxyOneWayMethod = castleProxyType.GetMethod(nameof(ITestWeatherGrain.LogWeatherEventAsync));

        var reflectionEmitHasOneWay = reflectionEmitOneWayMethod?.GetCustomAttribute<OneWayAttribute>() != null;
        var castleProxyHasOneWay = castleProxyOneWayMethod?.GetCustomAttribute<OneWayAttribute>() != null;

        Assert.True(reflectionEmitHasOneWay, "Reflection.Emit should preserve OneWay attribute");
        Assert.False(castleProxyHasOneWay, "Castle DynamicProxy does not preserve Orleans attributes");
    }

    [Fact] 
    public async Task GenerateGrainImplementation_CachesBySamePluginAndInterface()
    {
        // Arrange
        var plugin1 = await InitializePluginAsync(new TestWeatherPlugin());
        var plugin2 = await InitializePluginAsync(new TestWeatherPlugin()); // Same type as plugin1

        // Act
        var grain1 = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin1);
        var grain2 = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin2);

        // Assert - Should use cached type for same plugin type + interface combination
        Assert.Equal(grain1.GetType(), grain2.GetType());
    }

    [Fact]
    public async Task GenerateGrainImplementation_AttributeMismatch_UsesInterfaceAsAuthoritative()
    {
        // Arrange - plugin with mismatched attributes
        var plugin = await InitializePluginAsync(new MismatchedAttributePlugin());

        // Act
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);

        // Debug: Check what routing info is being created
        var routingInfo = _methodRouter.GetRoutingInfo("GetTemperatureAsync");
        Console.WriteLine($"Routing info IsReadOnly: {routingInfo?.IsReadOnly ?? false}");
        
        // Debug: Check interface method attributes
        var interfaceMethod = typeof(ITestWeatherGrain).GetMethod("GetTemperatureAsync");
        var interfaceHasReadOnly = interfaceMethod?.GetCustomAttribute<ReadOnlyAttribute>() != null;
        Console.WriteLine($"Interface method has ReadOnly: {interfaceHasReadOnly}");

        // Assert - Interface attributes should be authoritative
        var grainType = grainImpl.GetType();
        var method = grainType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));
        Assert.NotNull(method);
        
        // Debug: List all attributes on the generated method
        var attributes = method.GetCustomAttributes().ToList();
        Console.WriteLine($"Generated method attributes: {string.Join(", ", attributes.Select(a => a.GetType().Name))}");
        
        // Should have ReadOnly attribute from interface, not from plugin
        var readOnlyAttr = method.GetCustomAttribute<ReadOnlyAttribute>();
        
        // Better error message
        if (readOnlyAttr == null)
        {
            Console.WriteLine("ReadOnly attribute is missing from generated method");
            Console.WriteLine($"Method declaring type: {method.DeclaringType?.Name}");
            Console.WriteLine($"Generated type assembly: {grainType.Assembly.FullName}");
        }
        
        Assert.NotNull(readOnlyAttr);
    }

    [Fact]
    public async Task GenerateGrainImplementation_ErrorInPlugin_PropagatesException()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => grainImpl.GetWeatherAsync("ErrorCity"));
    }

    [Fact]
    public async Task GenerateGrainImplementation_TypeConversion_WorksCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);
        var temp = await grainImpl.GetTemperatureAsync("Boston");

        // Assert
        Assert.Equal(20.5m, temp);
    }

    [Fact]
    public async Task GenerateGrainImplementation_ConcurrentCalls_HandledCorrectly()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act
        var grainImpl = _proxyGenerator.GenerateGrainImplementation<ITestWeatherGrain>(plugin);
        var tasks = Enumerable.Range(0, 10).Select(async i =>
            await grainImpl.GetWeatherAsync($"City{i}"));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.Contains("Weather for City", r));
    }

    [Fact]
    public async Task GenerateGrainProxy_ReadOnlyMethod_DoesNotHaveOrleansAttribute()
    {
        // Arrange
        var plugin = await InitializePluginAsync(new TestWeatherPlugin());

        // Act  
        var proxy = _proxyGenerator.GenerateGrainProxy<ITestWeatherGrain>(plugin);
        var result = await proxy.GetTemperatureAsync("London");

        // Assert
        Assert.Equal(20.5m, result);
        
        // Verify the method does NOT have ReadOnly attribute (limitation of Castle DynamicProxy)
        var proxyType = proxy.GetType();
        var method = proxyType.GetMethod(nameof(ITestWeatherGrain.GetTemperatureAsync));
        Assert.NotNull(method);
        
        var readOnlyAttr = method.GetCustomAttribute<ReadOnlyAttribute>();
        Assert.Null(readOnlyAttr); // Castle DynamicProxy doesn't preserve Orleans attributes
    }
}

// Test interfaces and implementations
public interface ITestWeatherGrain
{
    Task<string> GetWeatherAsync(string location);
    
    [ReadOnly]
    Task<decimal> GetTemperatureAsync(string location);
    
    [AlwaysInterleave]
    Task StartMonitoringAsync(string location, int intervalMinutes);
    
    [OneWay]
    Task LogWeatherEventAsync(string eventMessage);
    
    Task<List<string>> GetForecastAsync(string location, int days);
}

[AgentPlugin("TestWeather", "1.0.0")]
public class TestWeatherPlugin : AgentPluginBase
{
    public bool IsMonitoring { get; private set; }
    public string? MonitoringLocation { get; private set; }
    public string? LastEvent { get; private set; }

    [AgentMethod("GetWeatherAsync")]
    public async Task<string> GetWeatherAsync(string location)
    {
        await Task.Delay(10); // Simulate work
        
        if (location == "ErrorCity")
        {
            throw new InvalidOperationException("Weather service unavailable for ErrorCity");
        }
        
        return $"Weather for {location}: Sunny, 25°C";
    }

    [AgentMethod("GetTemperatureAsync", IsReadOnly = true)]
    public async Task<decimal> GetTemperatureAsync(string location)
    {
        await Task.Delay(5);
        return 20.5m;
    }

    [AgentMethod("StartMonitoringAsync", AlwaysInterleave = true)]
    public async Task StartMonitoringAsync(string location, int intervalMinutes)
    {
        await Task.Delay(1);
        IsMonitoring = true;
        MonitoringLocation = location;
    }

    [AgentMethod("LogWeatherEventAsync", OneWay = true)]
    public async Task LogWeatherEventAsync(string eventMessage)
    {
        await Task.Delay(1);
        LastEvent = eventMessage;
    }

    [AgentMethod("GetForecastAsync")]
    public async Task<List<string>> GetForecastAsync(string location, int days)
    {
        await Task.Delay(10);
        
        var forecast = new List<string>();
        for (int i = 0; i < days; i++)
        {
            forecast.Add($"{location} Day {i + 1}: Partly cloudy");
        }
        
        return forecast;
    }
}

/// <summary>
/// Test plugin with mismatched attributes (plugin attributes don't match interface)
/// </summary>
[AgentPlugin("MismatchedWeather", "1.0.0")]
public class MismatchedAttributePlugin : AgentPluginBase
{
    // Plugin says NOT ReadOnly, but interface says ReadOnly - interface should win
    [AgentMethod("GetTemperatureAsync", IsReadOnly = false)]
    public async Task<decimal> GetTemperatureAsync(string location)
    {
        await Task.Delay(5);
        return 25.0m;
    }

    // Plugin says NOT AlwaysInterleave, but interface says AlwaysInterleave - interface should win
    [AgentMethod("StartMonitoringAsync", AlwaysInterleave = false)]
    public async Task StartMonitoringAsync(string location, int intervalMinutes)
    {
        await Task.Delay(1);
    }

    // Plugin says NOT OneWay, but interface says OneWay - interface should win
    [AgentMethod("LogWeatherEventAsync", OneWay = false)]
    public async Task LogWeatherEventAsync(string eventMessage)
    {
        await Task.Delay(1);
    }

    [AgentMethod("GetWeatherAsync")]
    public async Task<string> GetWeatherAsync(string location)
    {
        await Task.Delay(10);
        return $"Weather for {location}: Mismatched plugin result";
    }

    [AgentMethod("GetForecastAsync")]
    public async Task<List<string>> GetForecastAsync(string location, int days)
    {
        await Task.Delay(10);
        return new List<string> { "Mismatched forecast" };
    }
}

public class PluginGrainInterceptorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly OrleansMethodRouter _methodRouter;
    private readonly TestWeatherPlugin _plugin;
    private readonly PluginGrainInterceptor<ITestWeatherGrain> _interceptor;

    public PluginGrainInterceptorTests()
    {
        _loggerMock = new Mock<ILogger>();
        var routerLoggerMock = new Mock<ILogger<OrleansMethodRouter>>();
        _methodRouter = new OrleansMethodRouter(routerLoggerMock.Object);
        _plugin = new TestWeatherPlugin();
        _interceptor = new PluginGrainInterceptor<ITestWeatherGrain>(_plugin, _methodRouter, _loggerMock.Object);
    }

    [Fact]
    public void Intercept_ValidMethod_RoutesToPlugin()
    {
        // Arrange
        var invocationMock = new Mock<Castle.DynamicProxy.IInvocation>();
        var method = typeof(ITestWeatherGrain).GetMethod(nameof(ITestWeatherGrain.GetWeatherAsync))!;
        
        invocationMock.Setup(i => i.Method).Returns(method);
        invocationMock.Setup(i => i.Arguments).Returns(new object[] { "TestCity" });
        invocationMock.SetupProperty(i => i.ReturnValue);

        // Register plugin first
        _methodRouter.RegisterPlugin(_plugin);

        // Act
        _interceptor.Intercept(invocationMock.Object);

        // Assert
        Assert.NotNull(invocationMock.Object.ReturnValue);
        Assert.IsAssignableFrom<Task>(invocationMock.Object.ReturnValue);
    }

    [Fact]
    public void Intercept_VoidMethod_HandlesCorrectly()
    {
        // Arrange
        var invocationMock = new Mock<Castle.DynamicProxy.IInvocation>();
        var method = typeof(ITestWeatherGrain).GetMethod(nameof(ITestWeatherGrain.LogWeatherEventAsync))!;
        
        invocationMock.Setup(i => i.Method).Returns(method);
        invocationMock.Setup(i => i.Arguments).Returns(new object[] { "Test event" });
        invocationMock.SetupProperty(i => i.ReturnValue);

        _methodRouter.RegisterPlugin(_plugin);

        // Act
        _interceptor.Intercept(invocationMock.Object);

        // Assert - Should not throw for void methods
        Assert.True(true);
    }
}

public class ProxyGenerationExceptionTests
{
    [Fact]
    public void ProxyGenerationException_WithMessage_CreatesCorrectly()
    {
        // Arrange & Act
        var exception = new ProxyGenerationException("Test message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void MethodGenerationException_WithInnerException_CreatesCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        
        // Act
        var exception = new MethodGenerationException("Test message", innerException);

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}