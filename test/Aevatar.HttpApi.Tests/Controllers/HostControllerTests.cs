using Aevatar.Controllers;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Kubernetes.Enum;
using Aevatar.Options;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aevatar.HttpApi.Tests.Controllers;

public class HostControllerTests
{
    private readonly Mock<ILogService> _logServiceMock;
    private readonly Mock<IOptionsSnapshot<KubernetesOptions>> _kubernetesOptionsMock;
    private readonly HostController _controller;
    private readonly KubernetesOptions _kubernetesOptions;

    public HostControllerTests()
    {
        _logServiceMock = new Mock<ILogService>();
        _kubernetesOptionsMock = new Mock<IOptionsSnapshot<KubernetesOptions>>();
        _kubernetesOptions = new KubernetesOptions { AppNameSpace = "test-namespace" };

        _kubernetesOptionsMock.Setup(x => x.Value)
            .Returns(_kubernetesOptions);

        _controller = new HostController(_logServiceMock.Object, _kubernetesOptionsMock.Object);
    }

    [Fact]
    public async Task GetLatestRealTimeLogs_ShouldReturnLogs()
    {
        // Arrange
        var appId = "test-app";
        var hostType = HostTypeEnum.Silo;
        var offset = 0;
        var expectedLogs = new List<HostLogIndex>();
        var expectedIndexName = "test-index";

        _logServiceMock.Setup(x =>
                x.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, $"{appId}-{hostType.ToString().ToLower()}",
                    "1"))
            .Returns(expectedIndexName);

        _logServiceMock.Setup(x => x.GetHostLatestLogAsync(expectedIndexName, offset))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetLatestRealTimeLogs(appId, hostType, offset);

        // Assert
        Assert.Equal(expectedLogs, result);
        _logServiceMock.Verify(
            x => x.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, $"{appId}-{hostType.ToString().ToLower()}",
                "1"), Times.Once);
        _logServiceMock.Verify(x => x.GetHostLatestLogAsync(expectedIndexName, offset), Times.Once);
    }
}