using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Analytics;
using Aevatar.Application.Contracts.Services;
using Aevatar.Options;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services;

public class GoogleAnalyticsServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<GoogleAnalyticsOptions>> _optionsMock;
    private readonly Mock<ILogger<GoogleAnalyticsService>> _loggerMock;
    private readonly GoogleAnalyticsOptions _gaOptions;
    private readonly GoogleAnalyticsService _service;

    public GoogleAnalyticsServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _gaOptions = new GoogleAnalyticsOptions
        {
            MeasurementId = "G-TEST123456",
            ApiSecret = "test-api-secret",
            ApiEndpoint = "https://www.google-analytics.com/mp/collect",
            TimeoutSeconds = 10
        };
        
        _optionsMock = new Mock<IOptions<GoogleAnalyticsOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_gaOptions);
        
        _loggerMock = new Mock<ILogger<GoogleAnalyticsService>>();
        
        _service = new GoogleAnalyticsService(_httpClient, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task TrackEventAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "login_success",
            ClientId = "test-client-id",
            UserId = "test-user-123",
            Parameters = new Dictionary<string, object>
            {
                ["provider"] = "google",
                ["email"] = "test@example.com"
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Message.ShouldBe("Event tracked successfully");
    }

    [Fact]
    public async Task TrackEventAsync_WithHttpError_ShouldReturnFailure()
    {
        // Arrange
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "test_event",
            ClientId = "test-client-id"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            });

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Failed to track event");
    }
} 