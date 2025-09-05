using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Util;

namespace Aevatar.GAgents.TestBase.Mocks;

/// <summary>
/// Mock implementation of IBytePlusModelArkClient for unit testing
/// Provides predictable responses for testing video generation scenarios
/// </summary>
public class MockBytePlusModelArkClient : IBytePlusModelArkClient
{
    public bool ShouldSimulateFailure { get; set; } = false;
    public string FailureMessage { get; set; } = "Mock API failure";
    public bool ShouldSimulateTimeout { get; set; } = false;
    public string MockTaskId { get; set; } = Guid.NewGuid().ToString();
    public string MockVideoUrl { get; set; } = "https://mock-video-storage.example.com/videos/test-video.mp4";
    public string MockStatus { get; set; } = "succeeded";

    public Task<BytePlusVideoTaskResponse> CreateVideoGenerationTaskAsync(string prompt, string apiKey, string baseUrl, string? imageUrl = null, VideoGenerationConfigDto? options = null)
    {
        if (ShouldSimulateTimeout)
        {
            throw new TimeoutException("Mock timeout simulation");
        }

        if (ShouldSimulateFailure)
        {
            throw new Exception(FailureMessage);
        }

        if (string.IsNullOrEmpty(imageUrl) == false && !IsValidUrl(imageUrl))
        {
            throw new ArgumentException($"Invalid image URL format: {imageUrl}", nameof(imageUrl));
        }

        var response = new BytePlusVideoTaskResponse
        {
            Id = MockTaskId,
            Model = "mock-video-model",
            Status = "submitted",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return Task.FromResult(response);
    }

    public Task<BytePlusVideoTaskStatus> GetVideoGenerationTaskAsync(string taskId, string apiKey, string baseUrl)
    {
        if (ShouldSimulateFailure)
        {
            throw new Exception(FailureMessage);
        }

        var status = new BytePlusVideoTaskStatus
        {
            Id = taskId,
            Model = "mock-video-model",
            Status = MockStatus,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Set content based on status
        if (MockStatus == "succeeded")
        {
            status.Content = new BytePlusVideoContent
            {
                VideoUrl = MockVideoUrl
            };
        }
        else if (MockStatus == "failed")
        {
            status.Error = new BytePlusError
            {
                Code = "MOCK_ERROR",
                Message = FailureMessage
            };
        }

        return Task.FromResult(status);
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
