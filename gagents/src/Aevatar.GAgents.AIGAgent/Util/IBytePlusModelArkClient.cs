using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.Util;

/// <summary>
/// Interface for BytePlus Model Ark client to enable dependency injection and mocking in unit tests
/// </summary>
public interface IBytePlusModelArkClient
{
    /// <summary>
    /// Creates a new video generation task using the BytePlus API
    /// </summary>
    /// <param name="prompt">The text prompt for video generation</param>
    /// <param name="apiKey">API key for authentication</param>
    /// <param name="baseUrl">Base URL for the BytePlus API</param>
    /// <param name="imageUrl">Optional image URL for image-to-video generation</param>
    /// <param name="options">Optional configuration options for video generation</param>
    /// <returns>Response containing the task ID and initial status</returns>
    Task<BytePlusVideoTaskResponse> CreateVideoGenerationTaskAsync(string prompt, string apiKey, string baseUrl, string? imageUrl = null, VideoGenerationConfigDto? options = null);

    /// <summary>
    /// Gets the current status of a video generation task
    /// </summary>
    /// <param name="taskId">The BytePlus task ID to check status for</param>
    /// <param name="apiKey">API key for authentication</param>
    /// <param name="baseUrl">Base URL for the BytePlus API</param>
    /// <returns>Current status of the video generation task</returns>
    Task<BytePlusVideoTaskStatus> GetVideoGenerationTaskAsync(string taskId, string apiKey, string baseUrl);
}
