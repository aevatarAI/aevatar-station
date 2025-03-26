using System;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Dapr;
using Xunit;

namespace Aevatar.Dapr;

public class DaprProviderTests
{
    private readonly Mock<ILogger<DaprProvider>> _mockLogger;
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly Mock<IOptions<AbpDaprOptions>> _mockOptions;
    private readonly DaprProvider _daprProvider;

    public DaprProviderTests()
    {
        // 初始化 Mock 对象
        _mockLogger = new Mock<ILogger<DaprProvider>>();
        _mockDaprClient = new Mock<DaprClient>();
        _mockOptions = new Mock<IOptions<AbpDaprOptions>>();

        // Mock 返回一个有效的配置（例如默认值）
        _mockOptions.Setup(o => o.Value).Returns(new AbpDaprOptions());

        // 初始化 DaprProvider 实例
        _daprProvider = new DaprProvider(
            _mockLogger.Object,
            _mockDaprClient.Object,
            _mockOptions.Object
        );
    }

    [Fact]
    public async Task PublishEventAsync_ShouldCallDaprClientAndLogSuccess()
    {
        // Arrange
        string pubsubName = "pubsub";
        string topicName = "test-topic";
        string message = "Hello, World!";

        // 设置 DaprClient 的 PublishEventAsync 不抛出任何异常
        _mockDaprClient.Setup(c => c.PublishEventAsync(pubsubName, topicName, message, default));

        // Act
        await _daprProvider.PublishEventAsync(pubsubName, topicName, message);

        // Assert
        // 验证 DaprClient 的 PublishEventAsync 是否被调用一次
        _mockDaprClient.Verify(c => c.PublishEventAsync(pubsubName, topicName, message, default), Times.Once);

        // 验证日志是否记录了成功信息
        _mockLogger.Verify(logger =>
                logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Dapr event published")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishEventAsync_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        string pubsubName = "pubsub";
        string topicName = "test-topic";
        string message = "Hello, World!";

        // 模拟异常
        var exception = new Exception("Test exception");
        _mockDaprClient
            .Setup(c => c.PublishEventAsync(pubsubName, topicName, message, default))
            .ThrowsAsync(exception);

        // Act
        await _daprProvider.PublishEventAsync(pubsubName, topicName, message);

        // Assert
        // 验证 DaprClient 的 PublishEventAsync 是否被调用一次
        _mockDaprClient.Verify(c => c.PublishEventAsync(pubsubName, topicName, message, default), Times.Once);

        // 验证日志是否记录了错误信息
        _mockLogger.Verify(logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error publishing event")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}