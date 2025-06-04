using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

public class OrleansMethodRouterTests
{
    private readonly Mock<ILogger<OrleansMethodRouter>> _loggerMock;
    private readonly Mock<IAgentContext> _contextMock;
    private readonly OrleansMethodRouter _router;
    
    public OrleansMethodRouterTests()
    {
        _loggerMock = new Mock<ILogger<OrleansMethodRouter>>();
        _contextMock = new Mock<IAgentContext>();
        
        // Setup the context mock with required properties
        _contextMock.Setup(x => x.AgentId).Returns("test-agent-123");
        _contextMock.Setup(x => x.Logger).Returns(new Mock<IAgentLogger>().Object);
        _contextMock.Setup(x => x.Configuration).Returns(new Dictionary<string, object>());
        
        _router = new OrleansMethodRouter(_loggerMock.Object);
    }

    [Fact]
    public async Task RegisterPlugin_ValidPlugin_CachesMethodsCorrectly()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        // Act
        _router.RegisterPlugin(plugin);
        
        // Assert
        var routingInfo = _router.GetRoutingInfo("TestMethod");
        Assert.NotNull(routingInfo);
        Assert.Equal("TestMethod", routingInfo.MethodName);
        Assert.False(routingInfo.IsReadOnly);
        
        var readOnlyInfo = _router.GetRoutingInfo("ReadOnlyMethod");
        Assert.NotNull(readOnlyInfo);
        Assert.True(readOnlyInfo.IsReadOnly);
        
        var interleaveInfo = _router.GetRoutingInfo("InterleaveMethod");
        Assert.NotNull(interleaveInfo);
        Assert.True(interleaveInfo.AlwaysInterleave);
    }

    [Fact]
    public async Task RouteMethodCallAsync_ValidMethod_CallsPlugin()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var result = await _router.RouteMethodCallAsync(plugin, "TestMethod", new object[] { "test-input" });
        
        // Assert
        Assert.Equal("Routed: test-input", result);
    }

    [Fact]
    public async Task RouteMethodCallAsync_InvalidMethod_ThrowsException()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _router.RouteMethodCallAsync(plugin, "NonExistentMethod", Array.Empty<object>()));
    }

    [Fact]
    public async Task IsReadOnly_ReadOnlyMethod_ReturnsTrue()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var isReadOnly = _router.IsReadOnly("ReadOnlyMethod");
        
        // Assert
        Assert.True(isReadOnly);
    }

    [Fact]
    public async Task IsReadOnly_NonReadOnlyMethod_ReturnsFalse()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var isReadOnly = _router.IsReadOnly("TestMethod");
        
        // Assert
        Assert.False(isReadOnly);
    }

    [Fact]
    public async Task AlwaysInterleave_InterleaveMethod_ReturnsTrue()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var alwaysInterleave = _router.AlwaysInterleave("InterleaveMethod");
        
        // Assert
        Assert.True(alwaysInterleave);
    }

    [Fact]
    public async Task IsOneWay_OneWayMethod_ReturnsTrue()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var isOneWay = _router.IsOneWay("OneWayMethod");
        
        // Assert
        Assert.True(isOneWay);
    }

    [Fact]
    public async Task GetRoutingInfo_NonExistentMethod_ReturnsNull()
    {
        // Arrange
        var plugin = new TestRoutingPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        _router.RegisterPlugin(plugin);
        
        // Act
        var routingInfo = _router.GetRoutingInfo("NonExistentMethod");
        
        // Assert
        Assert.Null(routingInfo);
    }
}

/// <summary>
/// Test plugin for method routing tests
/// </summary>
[AgentPlugin("TestRoutingAgent", "1.0.0")]
public class TestRoutingPlugin : AgentPluginBase
{
    [AgentMethod("TestMethod")]
    public async Task<string> TestMethodAsync(string input)
    {
        await Task.Delay(1);
        return $"Routed: {input}";
    }

    [AgentMethod("ReadOnlyMethod", IsReadOnly = true)]
    public Task<string> ReadOnlyMethodAsync()
    {
        return Task.FromResult("ReadOnly result");
    }

    [AgentMethod("InterleaveMethod", AlwaysInterleave = true)]
    public Task<string> InterleaveMethodAsync()
    {
        return Task.FromResult("Interleave result");
    }

    [AgentMethod("OneWayMethod", OneWay = true)]
    public Task OneWayMethodAsync()
    {
        return Task.CompletedTask;
    }

    [AgentMethod("ParameterMethod")]
    public Task<int> ParameterMethodAsync(int input, string text)
    {
        return Task.FromResult(input + text.Length);
    }
}

public class OrleansAttributeMapperTests
{
    [Fact]
    public void MapToOrleansAttributes_ReadOnlyAttribute_MapsCorrectly()
    {
        // Arrange
        var agentMethodAttr = new AgentMethodAttribute { IsReadOnly = true };
        
        // Act
        var attributes = OrleansAttributeMapper.MapToOrleansAttributes(agentMethodAttr).ToList();
        
        // Assert
        Assert.Single(attributes);
        Assert.IsType<Orleans.Concurrency.ReadOnlyAttribute>(attributes[0]);
    }

    [Fact]
    public void MapToOrleansAttributes_AllAttributes_MapsCorrectly()
    {
        // Arrange
        var agentMethodAttr = new AgentMethodAttribute 
        { 
            IsReadOnly = true, 
            AlwaysInterleave = true, 
            OneWay = true 
        };
        
        // Act
        var attributes = OrleansAttributeMapper.MapToOrleansAttributes(agentMethodAttr).ToList();
        
        // Assert
        Assert.Equal(3, attributes.Count);
        Assert.Contains(attributes, a => a is Orleans.Concurrency.ReadOnlyAttribute);
        Assert.Contains(attributes, a => a is Orleans.Concurrency.AlwaysInterleaveAttribute);
        Assert.Contains(attributes, a => a is Orleans.Concurrency.OneWayAttribute);
    }

    [Fact]
    public void MapToOrleansAttributes_NoAttributes_ReturnsEmpty()
    {
        // Arrange
        var agentMethodAttr = new AgentMethodAttribute();
        
        // Act
        var attributes = OrleansAttributeMapper.MapToOrleansAttributes(agentMethodAttr).ToList();
        
        // Assert
        Assert.Empty(attributes);
    }
}