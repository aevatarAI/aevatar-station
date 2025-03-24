using Aevatar.Agent;
using Aevatar.CQRS.Dto;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aevatar.HttpApi.Tests.Controllers;

public class AgentControllerTests
{
    private readonly Mock<ILogger<AgentController>> _loggerMock;
    private readonly Mock<IAgentService> _agentServiceMock;
    private readonly Mock<SubscriptionAppService> _subscriptionAppServiceMock;
    private readonly AgentController _controller;

    public AgentControllerTests()
    {
        _loggerMock = new Mock<ILogger<AgentController>>();
        _agentServiceMock = new Mock<IAgentService>();
        _subscriptionAppServiceMock = new Mock<SubscriptionAppService>();
        _controller = new AgentController(_loggerMock.Object, _subscriptionAppServiceMock.Object,
            _agentServiceMock.Object);
    }

    [Fact]
    public async Task GetAgentLogs_ShouldReturnLogs()
    {
        // Arrange
        var agentId = "test-agent";
        var pageIndex = 1;
        var pageSize = 10;
        var expectedResult = new Tuple<long, List<AgentGEventIndex>>(1, new List<AgentGEventIndex>());

        _agentServiceMock.Setup(x => x.GetAgentEventLogsAsync(agentId, pageIndex, pageSize))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAgentLogs(agentId, pageIndex, pageSize);

        // Assert
        Assert.Equal(expectedResult, result);
        _agentServiceMock.Verify(x => x.GetAgentEventLogsAsync(agentId, pageIndex, pageSize), Times.Once);
    }

    [Fact]
    public async Task GetAgent_ShouldReturnAgent()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expectedAgent = new AgentDto();

        _agentServiceMock.Setup(x => x.GetAgentAsync(guid))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await _controller.GetAgent(guid);

        // Assert
        Assert.Equal(expectedAgent, result);
        _agentServiceMock.Verify(x => x.GetAgentAsync(guid), Times.Once);
    }

    [Fact]
    public async Task CreateAgent_ShouldCreateAndReturnAgent()
    {
        // Arrange
        var input = new CreateAgentInputDto();
        var expectedAgent = new AgentDto();

        _agentServiceMock.Setup(x => x.CreateAgentAsync(input))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await _controller.CreateAgent(input);

        // Assert
        Assert.Equal(expectedAgent, result);
        _agentServiceMock.Verify(x => x.CreateAgentAsync(input), Times.Once);
    }

    [Fact]
    public async Task UpdateAgent_ShouldUpdateAndReturnAgent()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var input = new UpdateAgentInputDto();
        var expectedAgent = new AgentDto();

        _agentServiceMock.Setup(x => x.UpdateAgentAsync(guid, input))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await _controller.UpdateAgent(guid, input);

        // Assert
        Assert.Equal(expectedAgent, result);
        _agentServiceMock.Verify(x => x.UpdateAgentAsync(guid, input), Times.Once);
    }

    [Fact]
    public async Task DeleteAgent_ShouldCallDeleteService()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        await _controller.DeleteAgent(guid);

        // Assert
        _agentServiceMock.Verify(x => x.DeleteAgentAsync(guid), Times.Once);
    }
}