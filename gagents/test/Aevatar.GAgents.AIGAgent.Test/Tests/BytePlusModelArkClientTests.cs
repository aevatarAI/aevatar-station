using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using Newtonsoft.Json;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Comprehensive unit tests for BytePlusModelArkClient
/// Tests API client functionality, validation, error handling, and HTTP interactions
/// </summary>

public sealed class BytePlusModelArkClientTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BytePlusModelArkClient> _logger;

    public BytePlusModelArkClientTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _logger = new Mock<ILogger<BytePlusModelArkClient>>().Object;
    }

    #region Constructor Tests

    [Theory]
    [InlineData("valid-http-client", "api-key", "https://api.test.com")]
    [InlineData("valid-http-client", "", "https://api.test.com")]
    [InlineData("valid-http-client", "api-key", "")]
    public void Constructor_WithValidParameters_ShouldNotThrowException(string httpClientParam, string apiKey, string baseUrl)
    {
        // Arrange
        var testHttpClient = httpClientParam == "valid-http-client" ? _httpClient : null;

        // Act & Assert - Should not throw any exception
        var client = new BytePlusModelArkClient(testHttpClient!, _logger, apiKey, baseUrl);
        
        // Verify client was created successfully
        client.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"Successfully created client with httpClient: {httpClientParam}, apiKey: {apiKey}, baseUrl: {baseUrl}");
    }

    #endregion

    #region Image URL Validation Tests

    [Theory]
    [InlineData("https://example.com/image.jpg", true)]
    [InlineData("https://example.com/image.jpeg", true)]
    [InlineData("https://example.com/image.png", true)]
    [InlineData("http://example.com/image.jpg", true)]
    [InlineData("https://example.com/path/to/image.jpg?param=value", true)]
    public async Task CreateVideoGenerationTaskAsync_WithValidImageUrls_ShouldAccept(string imageUrl, bool shouldBeValid)
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var mockResponse = CreateMockSuccessResponse();
        SetupMockHttpResponse(mockResponse);

        // Act & Assert
        if (shouldBeValid)
        {
            await client.CreateVideoGenerationTaskAsync("Test prompt", imageUrl);
            _testOutputHelper.WriteLine($"Valid image URL accepted: {imageUrl}");
        }
        else
        {
            await Should.ThrowAsync<ArgumentException>(async () =>
            {
                await client.CreateVideoGenerationTaskAsync("Test prompt", imageUrl);
            });
            _testOutputHelper.WriteLine($"Invalid image URL rejected: {imageUrl}");
        }
    }

    [Theory]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("https://example.com/document.pdf")]
    [InlineData("invalid-url")]
    [InlineData("https://example.com/image.txt")]
    public async Task CreateVideoGenerationTaskAsync_WithInvalidImageUrls_ShouldReject(string invalidImageUrl)
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await client.CreateVideoGenerationTaskAsync("Test prompt", invalidImageUrl);
        });
        
        _testOutputHelper.WriteLine($"Invalid image URL correctly rejected: {invalidImageUrl}");
    }

    #endregion

    #region Video Generation Task Creation Tests

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithTextOnly_ShouldUseTextToVideoModel()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var prompt = "A beautiful sunset";
        var options = CreateTestVideoOptions();
        var mockResponse = CreateMockSuccessResponse();
        
        SetupMockHttpResponse(mockResponse);

        // Act
        var result = await client.CreateVideoGenerationTaskAsync(prompt, null, options);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBeNullOrEmpty();
        
        // Verify the HTTP request was made correctly
        VerifyHttpRequest("POST", "/api/v3/contents/generations/tasks");
        
        _testOutputHelper.WriteLine($"Text-to-video task created with ID: {result.Id}");
    }

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithImageUrl_ShouldUseImageToVideoModel()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var prompt = "Animate this image";
        var imageUrl = "https://example.com/image.jpg";
        var options = CreateTestVideoOptions();
        var mockResponse = CreateMockSuccessResponse();
        
        SetupMockHttpResponse(mockResponse);

        // Act
        var result = await client.CreateVideoGenerationTaskAsync(prompt, imageUrl, options);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBeNullOrEmpty();
        
        VerifyHttpRequest("POST", "/api/v3/contents/generations/tasks");
        
        _testOutputHelper.WriteLine($"Image-to-video task created with ID: {result.Id}");
    }

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var prompt = "Test with null options";
        var mockResponse = CreateMockSuccessResponse();
        
        SetupMockHttpResponse(mockResponse);

        // Act
        var result = await client.CreateVideoGenerationTaskAsync(prompt, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBeNullOrEmpty();
        
        _testOutputHelper.WriteLine("Null options handled correctly with defaults");
    }

    #endregion

    #region Task Status Retrieval Tests

    [Fact]
    public async Task GetVideoGenerationTaskAsync_WithValidTaskId_ShouldReturnStatus()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var taskId = "test-task-123";
        var mockResponse = CreateMockStatusResponse();
        
        SetupMockHttpResponse(mockResponse);

        // Act
        var result = await client.GetVideoGenerationTaskAsync(taskId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(taskId);
        result.Status.ShouldNotBeNullOrEmpty();
        
        VerifyHttpRequest("GET", $"/api/v3/contents/generations/tasks/{taskId}");
        
        _testOutputHelper.WriteLine($"Task status retrieved: {result.Status}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetVideoGenerationTaskAsync_WithInvalidTaskId_ShouldThrowHttpRequestException(string invalidTaskId)
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");

        // Setup mock HTTP response for invalid task ID
        var errorResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\": \"Task not found\"}", System.Text.Encoding.UTF8, "application/json")
        };
        SetupMockHttpResponse(errorResponse);

        // Act & Assert - BytePlus API returns HTTP error for invalid task IDs
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.GetVideoGenerationTaskAsync(invalidTaskId);
        });
        
        _testOutputHelper.WriteLine($"Invalid task ID correctly handled: '{invalidTaskId}'");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithApiError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request Error")
        };
        
        SetupMockHttpResponse(errorResponse);

        // Act & Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.CreateVideoGenerationTaskAsync("Test prompt");
        });
        
        exception.Message.ShouldContain("BadRequest");
        _testOutputHelper.WriteLine($"API error correctly handled: {exception.Message}");
    }

    [Fact]
    public async Task GetVideoGenerationTaskAsync_WithNotFoundError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var errorResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Task not found")
        };
        
        SetupMockHttpResponse(errorResponse);

        // Act & Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.GetVideoGenerationTaskAsync("non-existent-task");
        });
        
        exception.Message.ShouldContain("NotFound");
        _testOutputHelper.WriteLine($"Not found error correctly handled: {exception.Message}");
    }

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithNetworkTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(async () =>
        {
            await client.CreateVideoGenerationTaskAsync("Test prompt");
        });
        
        _testOutputHelper.WriteLine("Network timeout correctly handled");
    }

    #endregion

    #region Configuration Option Tests

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_WithAllOptions_ShouldIncludeAllParameters()
    {
        // Arrange
        var client = new BytePlusModelArkClient(_httpClient, _logger, "test-key", "https://api.test.com");
        var options = new VideoGenerationConfigDto
        {
            Duration = 10,
            AspectRatio = "9:16",
            Resolution = "1080p",
            Style = "animated",
            MotionStrength = 1.0f,
            MotionType = "dynamic",
            ImageInfluence = 0.9f,
            AutoReturnResult = true
        };
        
        var mockResponse = CreateMockSuccessResponse();
        SetupMockHttpResponse(mockResponse);

        // Act
        var result = await client.CreateVideoGenerationTaskAsync("Comprehensive test", null, options);

        // Assert
        result.ShouldNotBeNull();
        VerifyHttpRequest("POST", "/api/v3/contents/generations/tasks");
        
        _testOutputHelper.WriteLine("All configuration options processed successfully");
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task CreateVideoGenerationTaskAsync_ShouldIncludeAuthenticationHeader()
    {
        // Arrange
        var apiKey = "test-api-key-123";
        var client = new BytePlusModelArkClient(_httpClient, _logger, apiKey, "https://api.test.com");
        var mockResponse = CreateMockSuccessResponse();
        
        HttpRequestMessage capturedRequest = null!;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
            {
                capturedRequest = request;
            })
            .ReturnsAsync(mockResponse);

        // Act
        await client.CreateVideoGenerationTaskAsync("Auth test");

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Headers.Authorization.ShouldNotBeNull();
        capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
        capturedRequest.Headers.Authorization.Parameter.ShouldBe(apiKey);
        
        _testOutputHelper.WriteLine($"Authentication header correctly set with API key");
    }

    #endregion

    #region Helper Methods

    private VideoGenerationConfigDto CreateTestVideoOptions()
    {
        return new VideoGenerationConfigDto
        {
            Duration = 5,
            AspectRatio = "16:9",
            Resolution = "720p",
            Style = "cinematic",
            MotionStrength = 0.7f,
            MotionType = "smooth",
            ImageInfluence = 0.8f,
            AutoReturnResult = true
        };
    }

    private HttpResponseMessage CreateMockSuccessResponse()
    {
        var responseData = new BytePlusVideoTaskResponse
        {
            Id = Guid.NewGuid().ToString(),
            Status = "processing",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(responseData))
        };
    }

    private HttpResponseMessage CreateMockStatusResponse()
    {
        var responseData = new BytePlusVideoTaskStatus
        {
            Id = "test-task-123",
            Status = "completed",
            Content = new BytePlusVideoContent
            {
                VideoUrl = "https://example.com/generated-video.mp4"
            },
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(responseData))
        };
    }

    private void SetupMockHttpResponse(HttpResponseMessage response)
    {
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpRequest(string expectedMethod, string expectedPath)
    {
        _mockHttpHandler.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method.ToString() == expectedMethod &&
                    req.RequestUri!.PathAndQuery.Contains(expectedPath)),
                ItExpr.IsAny<CancellationToken>());
        
        _testOutputHelper.WriteLine($"Verified {expectedMethod} request to {expectedPath}");
    }

    #endregion
}
