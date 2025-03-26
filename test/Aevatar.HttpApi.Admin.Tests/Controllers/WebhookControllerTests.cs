using System.Threading.Tasks;
using Aevatar.Admin.Controllers;
using Aevatar.Service;
using Aevatar.Webhook;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Aevatar.HttpApi.Admin.Tests.Controllers;

public class WebhookControllerTests
{
    private readonly Mock<IWebhookService> _webhookServiceMock;
    private readonly WebhookController _controller;

    public WebhookControllerTests()
    {
        _webhookServiceMock = new Mock<IWebhookService>();
        _controller = new WebhookController(_webhookServiceMock.Object);
    }

    [Fact]
    public async Task UploadCodeAsync_ShouldCallService()
    {
        // Arrange
        var webhookId = "test-webhook";
        var version = "1.0.0";
        var input = new CreateWebhookDto
        {
            Code = new FormFile(
                baseStream: new System.IO.MemoryStream(new byte[] { 1, 2, 3 }),
                baseStreamOffset: 0,
                length: 3,
                name: "code",
                fileName: "test.js"
            )
        };

        // Act
        await _controller.UploadCodeAsync(webhookId, version, input);

        // Assert
        _webhookServiceMock.Verify(x => x.CreateWebhookAsync(
            webhookId,
            version,
            It.Is<byte[]>(b => b.Length == 3)), 
            Times.Once);
    }

    [Fact]
    public async Task GetWebhookCodeAsync_ShouldReturnCode()
    {
        // Arrange
        var webhookId = "test-webhook";
        var version = "1.0.0";
        var expectedCode = "console.log('test');";
        _webhookServiceMock.Setup(x => x.GetWebhookCodeAsync(webhookId, version))
            .ReturnsAsync(expectedCode);

        // Act
        var result = await _controller.GetWebhookCodeAsync(webhookId, version);

        // Assert
        Assert.Equal(expectedCode, result);
        _webhookServiceMock.Verify(x => x.GetWebhookCodeAsync(webhookId, version), Times.Once);
    }

    [Fact]
    public async Task DestroyAppAsync_ShouldCallService()
    {
        // Arrange
        var input = new DestroyWebhookDto
        {
            WebhookId = "test-webhook",
            Version = "1.0.0"
        };

        // Act
        await _controller.DestroyAppAsync(input);

        // Assert
        _webhookServiceMock.Verify(x => x.DestroyWebhookAsync(input.WebhookId, input.Version), Times.Once);
    }
}
