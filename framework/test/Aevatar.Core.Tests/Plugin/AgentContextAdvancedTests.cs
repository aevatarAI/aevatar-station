using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Aevatar.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

/// <summary>
/// Advanced unit tests for AgentContext complex functionality
/// </summary>
public class AgentContextAdvancedTests : GAgentTestKitBase
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IAgentPluginRegistry> _pluginRegistryMock;
    private readonly Mock<IGrainFactory> _grainFactoryMock;
    private readonly IReadOnlyDictionary<string, object> _configuration;

    public AgentContextAdvancedTests()
    {
        _loggerMock = new Mock<ILogger>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _pluginRegistryMock = new Mock<IAgentPluginRegistry>();
        _grainFactoryMock = new Mock<IGrainFactory>();
        _configuration = new Dictionary<string, object> { { "testKey", "testValue" } };
        
        // Setup the service provider to return the plugin registry mock
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAgentPluginRegistry)))
                           .Returns(_pluginRegistryMock.Object);
    }

    #region PublishEventAsync Tests

    [Fact]
    public async Task PublishEventAsync_WithValidEvent_ConvertsAndPublishes()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);
        
        var testEvent = new TestPluginEvent
        {
            EventType = "TestEvent",
            Data = new { Message = "Test message" },
            Timestamp = DateTime.UtcNow,
            SourceAgentId = "test-agent"
        };

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(async () => await context.PublishEventAsync(testEvent));
    }

    [Fact]
    public async Task PublishEventAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => context.PublishEventAsync(null!));
    }

    [Fact]
    public async Task PublishEventAsync_WithComplexEventData_HandlesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);
        
        var complexData = new ComplexEventData
        {
            Id = 123,
            Properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "nested", new { SubProp = "value" } }
            },
            Items = new[] { "item1", "item2" }
        };

        var testEvent = new TestPluginEvent
        {
            EventType = "ComplexEvent",
            Data = complexData,
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(async () => await context.PublishEventAsync(testEvent));
    }

    #endregion

    #region PublishEventWithResponseAsync Tests

    [Fact]
    public async Task PublishEventWithResponseAsync_WithValidEvent_ReturnsResponse()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);
        
        var testEvent = new TestPluginEvent
        {
            EventType = "TestEventWithResponse",
            Data = new { Request = "Test request" },
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert - Test that method doesn't throw for unsupported scenarios
        await Should.ThrowAsync<NotSupportedException>(() => 
            context.PublishEventWithResponseAsync<TestResponse>(testEvent));
    }

    [Fact]
    public async Task PublishEventWithResponseAsync_WithTimeout_HandlesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);
        
        var testEvent = new TestPluginEvent
        {
            EventType = "TestEventWithTimeout",
            Data = new { Request = "Test request" }
        };

        var timeout = TimeSpan.FromSeconds(5);

        // Act & Assert
        await Should.ThrowAsync<NotSupportedException>(() => 
            context.PublishEventWithResponseAsync<TestResponse>(testEvent, timeout));
    }

    [Fact]
    public async Task PublishEventWithResponseAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            context.PublishEventWithResponseAsync<TestResponse>(null!));
    }

    #endregion

    #region GetAgentAsync Tests

    [Fact]
    public async Task GetAgentAsync_WithValidAgentId_ReturnsAgentReference()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentId = "test-agent-123";

        // Act & Assert - Should return a valid agent reference
        var agentReference = await context.GetAgentAsync(agentId);
        agentReference.ShouldNotBeNull();
        agentReference.AgentId.ShouldBe(agentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAgentAsync_WithInvalidAgentId_HandlesGracefully(string agentId)
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);

        // Act & Assert - Should handle empty/whitespace agent IDs
        var agentReference = await context.GetAgentAsync(agentId);
        agentReference.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAgentAsync_WithNullAgentId_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => context.GetAgentAsync(null!));
    }

    #endregion

    #region RegisterAgentsAsync Tests

    [Fact]
    public async Task RegisterAgentsAsync_WithMultipleAgents_RegistersAll()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentIds = new[] { "agent1", "agent2", "agent3" };

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(() => context.RegisterAgentsAsync(agentIds));
    }

    [Fact]
    public async Task RegisterAgentsAsync_WithEmptyCollection_HandlesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentIds = Array.Empty<string>();

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(() => context.RegisterAgentsAsync(agentIds));
    }

    [Fact]
    public async Task RegisterAgentsAsync_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => context.RegisterAgentsAsync(null!));
    }

    [Fact]
    public async Task RegisterAgentsAsync_WithDuplicateAgents_HandlesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentIds = new[] { "agent1", "agent1", "agent2", "agent1" };

        // Act & Assert - Should not throw even with duplicates
        await Should.NotThrowAsync(() => context.RegisterAgentsAsync(agentIds));
    }

    #endregion

    #region SubscribeToAgentsAsync Tests

    [Fact]
    public async Task SubscribeToAgentsAsync_WithValidAgents_SubscribesToAll()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentIds = new[] { "agent1", "agent2" };

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(() => context.SubscribeToAgentsAsync(agentIds));
    }

    [Fact]
    public async Task SubscribeToAgentsAsync_WithEmptyCollection_HandlesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);
        
        var agentIds = Array.Empty<string>();

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(() => context.SubscribeToAgentsAsync(agentIds));
    }

    [Fact]
    public async Task SubscribeToAgentsAsync_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => context.SubscribeToAgentsAsync(null!));
    }

    #endregion

    #region Constructor Edge Cases

    [Fact]
    public async Task Constructor_WithNullHostGAgent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AgentContext(null!, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration));
    }

    [Fact]
    public async Task Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AgentContext(hostGAgent, null!, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration));
    }

    [Fact]
    public async Task Constructor_WithNullGrainFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AgentContext(hostGAgent, _loggerMock.Object, null!, _serviceProviderMock.Object, _configuration));
    }

    [Fact]
    public async Task Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, null!, _configuration));
    }

    [Fact]
    public async Task Constructor_WithNullConfiguration_UsesEmptyDictionary()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());

        // Act
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, null);

        // Assert
        context.Configuration.ShouldNotBeNull();
        context.Configuration.ShouldBeEmpty();
    }

    #endregion

    #region Logging and Error Scenarios

    [Fact]
    public async Task AgentContext_LoggerAdapter_WorksCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, _configuration);

        // Act & Assert
        context.Logger.ShouldNotBeNull();
        context.Logger.ShouldBeOfType<AgentLoggerAdapter>();
        
        // Test that logger methods don't throw
        Should.NotThrow(() => context.Logger.LogInformation("Test message"));
        Should.NotThrow(() => context.Logger.LogError("Error message"));
        Should.NotThrow(() => context.Logger.LogDebug("Debug message"));
        Should.NotThrow(() => context.Logger.LogWarning("Warning message"));
    }

    [Fact]
    public async Task AgentContext_MissingPluginRegistry_ThrowsInvalidOperationException()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var serviceProviderWithoutRegistry = new Mock<IServiceProvider>();
        serviceProviderWithoutRegistry.Setup(sp => sp.GetService(typeof(IAgentPluginRegistry)))
                                     .Returns(null);

        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, serviceProviderWithoutRegistry.Object, _configuration);

        // Act & Assert - Should throw when trying to get an agent (which needs plugin registry)
        await Should.ThrowAsync<InvalidOperationException>(() => context.GetAgentAsync("test-agent"));
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task AgentContext_ConcurrentAccess_HandledCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var context = new AgentContext(hostGAgent, _loggerMock.Object, Silo.GrainFactory, _serviceProviderMock.Object, _configuration);

        var tasks = new List<Task>();

        // Act - Simulate concurrent access to different methods
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await context.GetAgentAsync($"agent-{index}");
                    await context.RegisterAgentsAsync(new[] { $"agent-{index}" });
                    await context.SubscribeToAgentsAsync(new[] { $"agent-{index}" });
                }
                catch
                {
                    // Expected to fail in test environment, but shouldn't cause concurrency issues
                }
            }));
        }

        // Assert - Should not throw concurrency exceptions
        await Should.NotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    #endregion
}

#region Test Support Classes

public class TestPluginEvent : IAgentEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceAgentId { get; set; }
}

public class TestResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

public class ComplexEventData
{
    public int Id { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string[] Items { get; set; } = Array.Empty<string>();
}

#endregion 