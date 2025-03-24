using Aevatar.CQRS;
using Aevatar.Query;
using Aevatar.Service;
using Moq;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace Aevatar.HttpApi.Tests.Controllers;

public class QueryControllerTests
{
    private readonly Mock<ICqrsService> _cqrsServiceMock;
    private readonly Mock<IIndexingService> _indexingServiceMock;
    private readonly QueryController _controller;

    public QueryControllerTests()
    {
        _cqrsServiceMock = new Mock<ICqrsService>();
        _indexingServiceMock = new Mock<IIndexingService>();
        _controller = new QueryController(_cqrsServiceMock.Object, _indexingServiceMock.Object);
    }

    [Fact]
    public async Task GetEventLogs_ShouldReturnLogs()
    {
        // Arrange
        var id = Guid.NewGuid();
        var agentType = "TestAgent";
        var pageIndex = 0;
        var pageSize = 20;
        var expectedLogs = new AgentEventLogsDto();

        _cqrsServiceMock.Setup(x => x.QueryGEventAsync(id, agentType, pageIndex, pageSize))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetEventLogs(id, agentType, pageIndex, pageSize);

        // Assert
        Assert.Equal(expectedLogs, result);
        _cqrsServiceMock.Verify(x => x.QueryGEventAsync(id, agentType, pageIndex, pageSize), Times.Once);
    }

    [Fact]
    public async Task GetStates_ShouldReturnStates()
    {
        // Arrange
        var stateName = "TestState";
        var id = Guid.NewGuid();
        var expectedState = new AgentStateDto();

        _cqrsServiceMock.Setup(x => x.QueryStateAsync(stateName, id))
            .ReturnsAsync(expectedState);

        // Act
        var result = await _controller.GetStates(stateName, id);

        // Assert
        Assert.Equal(expectedState, result);
        _cqrsServiceMock.Verify(x => x.QueryStateAsync(stateName, id), Times.Once);
    }

    [Fact]
    public async Task QueryEs_ShouldReturnResults()
    {
        // Arrange
        var request = new LuceneQueryDto { QueryString = "test query" };
        var expectedResults = new PagedResultDto<Dictionary<string, object>>();

        _indexingServiceMock.Setup(x => x.QueryWithLuceneAsync(request))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.QueryEs(request);

        // Assert
        Assert.Equal(expectedResults, result);
        _indexingServiceMock.Verify(x => x.QueryWithLuceneAsync(request), Times.Once);
    }
}