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
        // Initialize mock logger
        _mockLogger = new Mock<ILogger<DaprProvider>>();

        // Initialize mock DaprClient
        _mockDaprClient = new Mock<DaprClient>();

        // Initialize mock configuration options
        _mockOptions = new Mock<IOptions<AbpDaprOptions>>();

        // Set up the mock to return default config values
        _mockOptions.Setup(o => o.Value).Returns(new AbpDaprOptions());

        // Create DaprProvider instance using the mock objects
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

        // Simulate a successful call to DaprClient.PublishEventAsync with no exception thrown
        _mockDaprClient.Setup(c => c.PublishEventAsync(pubsubName, topicName, message, default));

        // Act
        await _daprProvider.PublishEventAsync(pubsubName, topicName, message);

        // Assert

        // Verify that DaprClient.PublishEventAsync was called once
        _mockDaprClient.Verify(
            c => c.PublishEventAsync(pubsubName, topicName, message, default),
            Times.Once
        );

        // Verify that a success message was logged
        _mockLogger.Verify(logger =>
                logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Dapr event published")),
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

        // Simulate an exception thrown by DaprClient.PublishEventAsync
        var exception = new Exception("Test exception");
        _mockDaprClient
            .Setup(c => c.PublishEventAsync(pubsubName, topicName, message, default))
            .ThrowsAsync(exception);

        // Act
        await _daprProvider.PublishEventAsync(pubsubName, topicName, message);

        // Assert

        // Verify that DaprClient.PublishEventAsync was called once
        _mockDaprClient.Verify(
            c => c.PublishEventAsync(pubsubName, topicName, message, default),
            Times.Once
        );

        // Verify that an error message was logged
        _mockLogger.Verify(logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Error publishing event")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}