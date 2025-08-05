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
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<GoogleAnalyticsOptions>> _optionsMock;
    private readonly Mock<ILogger<GoogleAnalyticsService>> _loggerMock;
    private readonly GoogleAnalyticsOptions _gaOptions;
    private readonly GoogleAnalyticsService _service;

    public GoogleAnalyticsServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        // Setup IHttpClientFactory mock
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient("GoogleAnalytics"))
            .Returns(_httpClient);
        
        _gaOptions = new GoogleAnalyticsOptions
        {
            MeasurementId = "G-TEST123456",
            ApiSecret = "test-api-secret",
            ApiEndpoint = "https://www.google-analytics.com/mp/collect",
            TimeoutSeconds = 10,
            EnableAnalytics = true
        };
        
        _optionsMock = new Mock<IOptions<GoogleAnalyticsOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_gaOptions);
        
        _loggerMock = new Mock<ILogger<GoogleAnalyticsService>>();
        
        _service = new GoogleAnalyticsService(_httpClientFactoryMock.Object, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task TrackEventAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "test_event",
            ClientId = "test-client-id",
            Parameters = new Dictionary<string, object>
            {
                { "parameter1", "value1" },
                { "parameter2", 123 }
            }
        };

        _httpMessageHandlerMock.Protected()
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
        // Add debug output to see what the actual error is
        if (!result.Success)
        {
            Console.WriteLine($"Test failed with error: {result.ErrorMessage}");
        }
        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        
        // Verify HttpClientFactory was called with correct name
        _httpClientFactoryMock.Verify(x => x.CreateClient("GoogleAnalytics"), Times.Once);
    }

    [Fact]
    public async Task TrackEventAsync_WithDisabledAnalytics_ShouldReturnFailure()
    {
        // Arrange
        _gaOptions.EnableAnalytics = false;
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "test_event",
            ClientId = "test-client-id"
        };

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Analytics reporting is disabled");
        
        // Verify HttpClientFactory was not called when analytics is disabled
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TrackEventAsync_WithMissingConfiguration_ShouldReturnFailure()
    {
        // Arrange
        _gaOptions.MeasurementId = "";
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "test_event",
            ClientId = "test-client-id"
        };

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Google Analytics not configured");
        
        // Verify HttpClientFactory was not called when configuration is missing
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
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

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request Error")
            });

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("HTTP BadRequest");
        
        // Verify HttpClientFactory was called
        _httpClientFactoryMock.Verify(x => x.CreateClient("GoogleAnalytics"), Times.Once);
    }

    [Fact]
    public async Task TrackEventAsync_WithException_ShouldReturnFailure()
    {
        // Arrange
        var request = new GoogleAnalyticsEventRequestDto
        {
            EventName = "test_event",
            ClientId = "test-client-id"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.TrackEventAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Network error");
        
        // Verify HttpClientFactory was called
        _httpClientFactoryMock.Verify(x => x.CreateClient("GoogleAnalytics"), Times.Once);
    }
} 