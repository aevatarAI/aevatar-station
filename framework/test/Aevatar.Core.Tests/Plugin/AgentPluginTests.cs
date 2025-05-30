using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Reflection;

namespace Aevatar.Core.Tests.Plugin;

public class AgentPluginTests
{
    private readonly Mock<ILogger<AgentPluginLoader>> _loggerMock;
    private readonly Mock<IAgentContext> _contextMock;
    
    public AgentPluginTests()
    {
        _loggerMock = new Mock<ILogger<AgentPluginLoader>>();
        _contextMock = new Mock<IAgentContext>();
        
        _contextMock.Setup(x => x.AgentId).Returns("test-agent-123");
        _contextMock.Setup(x => x.Logger).Returns(new Mock<IAgentLogger>().Object);
        _contextMock.Setup(x => x.Configuration).Returns(new Dictionary<string, object>());
    }

    [Fact]
    public async Task AgentPluginBase_InitializeAsync_SetsContextCorrectly()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        
        // Act
        await plugin.InitializeAsync(_contextMock.Object);
        
        // Assert
        Assert.NotNull(plugin.TestContext);
        Assert.Equal("test-agent-123", plugin.TestContext.AgentId);
    }

    [Fact]
    public async Task AgentPluginBase_ExecuteMethodAsync_CallsCorrectMethod()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        // Act
        var result = await plugin.ExecuteMethodAsync("TestMethod", new object[] { "test-input" });
        
        // Assert
        Assert.Equal("Processed: test-input", result);
    }

    [Fact]
    public async Task AgentPluginBase_ExecuteMethodAsync_ThrowsForInvalidMethod()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => plugin.ExecuteMethodAsync("NonExistentMethod", Array.Empty<object>()));
    }

    [Fact]
    public async Task AgentPluginBase_HandleEventAsync_CallsEventHandler()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        var agentEvent = new AgentEvent
        {
            EventType = "TestEvent",
            Data = "test-data",
            Timestamp = DateTime.UtcNow
        };
        
        // Act
        await plugin.HandleEventAsync(agentEvent);
        
        // Assert
        Assert.Equal("test-data", plugin.LastEventData);
    }

    [Fact]
    public async Task AgentPluginBase_HandleEventAsync_CallsGenericHandlerForUnknownEvent()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        var agentEvent = new AgentEvent
        {
            EventType = "UnknownEvent",
            Data = "unknown-data",
            Timestamp = DateTime.UtcNow
        };
        
        // Act
        await plugin.HandleEventAsync(agentEvent);
        
        // Assert
        Assert.Equal("unknown-data", plugin.LastGenericEventData);
    }

    [Fact]
    public async Task AgentPluginBase_StateManagement_WorksCorrectly()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        var testState = new { Value = "test-state" };
        
        // Act
        await plugin.SetStateAsync(testState);
        var retrievedState = await plugin.GetStateAsync();
        
        // Assert
        Assert.Equal(testState, retrievedState);
    }

    [Fact]
    public async Task AgentPluginBase_PublishEventAsync_CallsContext()
    {
        // Arrange
        var plugin = new TestAgentPlugin();
        await plugin.InitializeAsync(_contextMock.Object);
        
        // Act
        await plugin.TestPublishEventAsync("TestEventType", "test-data");
        
        // Assert
        _contextMock.Verify(x => x.PublishEventAsync(
            It.Is<IAgentEvent>(e => e.EventType == "TestEventType" && e.Data.Equals("test-data")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

/// <summary>
/// Test implementation of AgentPluginBase for unit testing
/// </summary>
[AgentPlugin("TestAgent", "1.0.0", Description = "Test Agent Plugin")]
public class TestAgentPlugin : AgentPluginBase
{
    public IAgentContext? TestContext => Context;
    public object? LastEventData { get; private set; }
    public object? LastGenericEventData { get; private set; }

    public TestAgentPlugin()
    {
        // Initialize metadata early so it's available before InitializeAsync() is called
        var type = GetType();
        var pluginAttr = type.GetCustomAttribute<AgentPluginAttribute>();
        
        if (pluginAttr != null)
        {
            Metadata = new AgentPluginMetadata(
                pluginAttr.Name,
                pluginAttr.Version,
                pluginAttr.Description ?? "Agent Plugin");
        }
    }

    [AgentMethod("TestMethod")]
    public async Task<string> TestMethodAsync(string input)
    {
        await Task.Delay(1); // Simulate async work
        return $"Processed: {input}";
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

    [AgentEventHandler("TestEvent")]
    public async Task HandleTestEventAsync(IAgentEvent agentEvent)
    {
        await Task.Delay(1);
        LastEventData = agentEvent.Data;
    }

    [AgentEventHandler] // Generic handler (no specific event type)
    public async Task HandleGenericEventAsync(IAgentEvent agentEvent)
    {
        await Task.Delay(1);
        LastGenericEventData = agentEvent.Data;
    }

    public async Task TestPublishEventAsync(string eventType, object data)
    {
        await PublishEventAsync(eventType, data);
    }

    public async Task<TResponse> TestPublishEventWithResponseAsync<TResponse>(string eventType, object data, TimeSpan? timeout = null)
        where TResponse : class
    {
        return await PublishEventWithResponseAsync<TResponse>(eventType, data, timeout);
    }
}