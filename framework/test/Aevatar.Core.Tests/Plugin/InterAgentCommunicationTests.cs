using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

public class InterAgentCommunicationTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly AgentPluginRegistry _registry;
    private readonly Dictionary<string, IAgentPlugin> _mockRegistry;

    public InterAgentCommunicationTests()
    {
        _loggerMock = new Mock<ILogger>();
        _registry = new AgentPluginRegistry(Mock.Of<ILogger<AgentPluginRegistry>>());
        _mockRegistry = new Dictionary<string, IAgentPlugin>();
    }

    [Fact]
    public async Task AgentReference_CallMethodAsync_ReturnsCorrectResult()
    {
        // Arrange
        var targetAgentId = "target-agent";
        var methodName = "TestMethod";
        var expectedResult = "test-result";

        var targetPluginMock = new Mock<IAgentPlugin>();
        targetPluginMock.Setup(p => p.ExecuteMethodAsync(methodName, It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);

        _mockRegistry[targetAgentId] = targetPluginMock.Object;

        var agentReference = new TestAgentReference(targetAgentId, _mockRegistry, _loggerMock.Object);

        // Act
        var result = await agentReference.CallMethodAsync<string>(methodName, "test-param");

        // Assert
        Assert.Equal(expectedResult, result);
        targetPluginMock.Verify(p => p.ExecuteMethodAsync(methodName, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgentReference_SendEventAsync_DeliversEvent()
    {
        // Arrange
        var targetAgentId = "target-agent";
        var agentEvent = new AgentEvent
        {
            EventType = "TestEvent",
            Data = "test-data",
            Timestamp = DateTime.UtcNow,
            SourceAgentId = "source-agent"
        };

        var targetPluginMock = new Mock<IAgentPlugin>();
        targetPluginMock.Setup(p => p.HandleEventAsync(agentEvent, It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _mockRegistry[targetAgentId] = targetPluginMock.Object;

        var agentReference = new TestAgentReference(targetAgentId, _mockRegistry, _loggerMock.Object);

        // Act
        await agentReference.SendEventAsync(agentEvent);

        // Assert
        targetPluginMock.Verify(p => p.HandleEventAsync(agentEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgentReference_CallMethodAsync_HandlesTypeConversion()
    {
        // Arrange
        var targetAgentId = "target-agent";
        var methodName = "NumberMethod";
        var numericResult = 42;

        var targetPluginMock = new Mock<IAgentPlugin>();
        targetPluginMock.Setup(p => p.ExecuteMethodAsync(methodName, It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(numericResult);

        _mockRegistry[targetAgentId] = targetPluginMock.Object;

        var agentReference = new TestAgentReference(targetAgentId, _mockRegistry, _loggerMock.Object);

        // Act
        var result = await agentReference.CallMethodAsync<string>(methodName);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public async Task AgentReference_CallMethodAsync_AgentNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentAgentId = "non-existent-agent";
        var agentReference = new TestAgentReference(nonExistentAgentId, _mockRegistry, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agentReference.CallMethodAsync<string>("TestMethod"));
    }

    [Fact]
    public async Task AgentReference_SendEventAsync_AgentNotFound_DoesNotThrow()
    {
        // Arrange
        var nonExistentAgentId = "non-existent-agent";
        var agentEvent = new AgentEvent
        {
            EventType = "TestEvent",
            Data = "test-data",
            Timestamp = DateTime.UtcNow
        };

        var agentReference = new TestAgentReference(nonExistentAgentId, _mockRegistry, _loggerMock.Object);

        // Act & Assert
        // Should not throw - just logs warning
        await agentReference.SendEventAsync(agentEvent);
    }

    [Fact]
    public async Task MultipleAgents_Communication_WorksCorrectly()
    {
        // Arrange
        var agent1Id = "weather-agent-1";
        var agent2Id = "weather-agent-2";

        // Create two mock plugins
        var plugin1Mock = new Mock<IAgentPlugin>();
        var plugin2Mock = new Mock<IAgentPlugin>();

        // Setup plugin1 to return weather data
        var weatherData = new { Location = "New York", Temperature = 25.0 };
        plugin1Mock.Setup(p => p.ExecuteMethodAsync("GetWeather", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(weatherData);

        // Setup plugin2 to handle weather events
        plugin2Mock.Setup(p => p.HandleEventAsync(It.IsAny<IAgentEvent>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        _mockRegistry[agent1Id] = plugin1Mock.Object;
        _mockRegistry[agent2Id] = plugin2Mock.Object;

        var agent1Ref = new TestAgentReference(agent1Id, _mockRegistry, _loggerMock.Object);
        var agent2Ref = new TestAgentReference(agent2Id, _mockRegistry, _loggerMock.Object);

        // Act
        // Agent2 calls Agent1 to get weather - Fixed: use agent1Ref instead of agent2Ref
        var weather = await agent1Ref.CallMethodAsync<object>("GetWeather");

        // Agent1 sends event to Agent2
        var weatherEvent = new AgentEvent
        {
            EventType = "WeatherUpdate",
            Data = weatherData,
            Timestamp = DateTime.UtcNow,
            SourceAgentId = agent1Id
        };
        await agent2Ref.SendEventAsync(weatherEvent);

        // Assert
        Assert.NotNull(weather);
        plugin1Mock.Verify(p => p.ExecuteMethodAsync("GetWeather", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        plugin2Mock.Verify(p => p.HandleEventAsync(It.Is<IAgentEvent>(e => e.EventType == "WeatherUpdate"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BatchCommunication_MultipleAgents_WorksEfficiently()
    {
        // Arrange
        var agentIds = new[] { "agent-1", "agent-2", "agent-3" };
        var results = new List<string>();

        foreach (var agentId in agentIds)
        {
            var pluginMock = new Mock<IAgentPlugin>();
            pluginMock.Setup(p => p.ExecuteMethodAsync("GetStatus", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync($"Status from {agentId}");
            _mockRegistry[agentId] = pluginMock.Object;
        }

        // Act
        var tasks = agentIds.Select(async agentId =>
        {
            var agentRef = new TestAgentReference(agentId, _mockRegistry, _loggerMock.Object);
            return await agentRef.CallMethodAsync<string>("GetStatus");
        });

        var batchResults = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(3, batchResults.Length);
        Assert.All(batchResults, result => Assert.Contains("Status from", result));
    }
}

/// <summary>
/// Test implementation of IAgentReference for testing inter-agent communication
/// </summary>
public class TestAgentReference : IAgentReference
{
    private readonly Dictionary<string, IAgentPlugin> _registry;
    private readonly ILogger _logger;

    public TestAgentReference(string agentId, Dictionary<string, IAgentPlugin> registry, ILogger logger)
    {
        AgentId = agentId;
        _registry = registry;
        _logger = logger;
    }

    public string AgentId { get; }

    public async Task<TResult> CallMethodAsync<TResult>(string methodName, params object?[] parameters)
    {
        await Task.Delay(1); // Simulate minimal network delay

        if (_registry.TryGetValue(AgentId, out var plugin))
        {
            try
            {
                var result = await plugin.ExecuteMethodAsync(methodName, parameters);

                if (result is TResult typedResult)
                {
                    return typedResult;
                }

                // Type conversion
                if (result != null)
                {
                    if (typeof(TResult) == typeof(string))
                    {
                        return (TResult)(object)result.ToString()!;
                    }

                    return (TResult)Convert.ChangeType(result, typeof(TResult));
                }

                return default(TResult)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call method {MethodName} on agent {AgentId}", methodName, AgentId);
                throw;
            }
        }

        throw new InvalidOperationException($"Agent {AgentId} not found in registry");
    }

    public async Task SendEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate minimal network delay

        if (_registry.TryGetValue(AgentId, out var plugin))
        {
            try
            {
                await plugin.HandleEventAsync(agentEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event {EventType} to agent {AgentId}", agentEvent.EventType, AgentId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found for event {EventType}", AgentId, agentEvent.EventType);
        }
    }
}