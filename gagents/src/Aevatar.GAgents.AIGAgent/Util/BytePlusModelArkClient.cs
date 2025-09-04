using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.Util;

public class BytePlusModelArkClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _baseUrl;

    public BytePlusModelArkClient(HttpClient httpClient, ILogger logger, string apiKey, string baseUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = baseUrl;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<BytePlusVideoTaskResponse> CreateVideoGenerationTaskAsync(string prompt, string? imageUrl = null, VideoGenerationConfigDto? options = null)
    {
        var requestUrl = $"{_baseUrl}/api/v3/contents/generations/tasks";
        
        // Validate image URL if provided
        if (!string.IsNullOrEmpty(imageUrl) && !IsValidImageUrl(imageUrl))
        {
            throw new ArgumentException($"Invalid image URL format: {imageUrl}. Please provide a valid HTTP/HTTPS URL pointing to an accessible image.", nameof(imageUrl));
        }
        
        // Select appropriate model based on whether image is provided
        var model = string.IsNullOrEmpty(imageUrl) 
            ? "seedance-1-0-pro-250528"  // Text-to-Video
            : "seedance-1-0-pro-250528"; // Image-to-Video
            
        var content = new List<object>();
        
        // Add text content with parameters
        var textPrompt = BuildPromptWithParameters(prompt, options);
        content.Add(new { type = "text", text = textPrompt });
        
        // Add image if provided (for Image-to-Video)
        if (!string.IsNullOrEmpty(imageUrl))
        {
            content.Add(new 
            { 
                type = "image_url", 
                image_url = new { url = imageUrl } 
            });
        }

        var request = new
        {
            model = model,
            content = content
        };

        var jsonContent = JsonConvert.SerializeObject(request);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("Creating BytePlus video generation task: {Model}, Prompt: {Prompt}", model, prompt);

        try
        {
            var response = await _httpClient.PostAsync(requestUrl, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("BytePlus API error: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                throw new HttpRequestException($"BytePlus API returned {response.StatusCode}: {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<BytePlusVideoTaskResponse>(responseContent);
            _logger.LogInformation("BytePlus video generation task created successfully: {TaskId}", result?.Id);
            
            return result ?? throw new InvalidOperationException("Failed to deserialize BytePlus response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BytePlus video generation task");
            throw;
        }
    }

    public async Task<BytePlusVideoTaskStatus> GetVideoGenerationTaskAsync(string taskId)
    {
        var requestUrl = $"{_baseUrl}/api/v3/contents/generations/tasks/{taskId}";
        
        _logger.LogDebug("Checking BytePlus video generation task status: {TaskId}", taskId);

        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("BytePlus API error: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                throw new HttpRequestException($"BytePlus API returned {response.StatusCode}: {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<BytePlusVideoTaskStatus>(responseContent);
            _logger.LogDebug("BytePlus task status: {TaskId} -> {Status}", taskId, result?.Status);
            
            return result ?? throw new InvalidOperationException("Failed to deserialize BytePlus response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BytePlus video generation task status: {TaskId}", taskId);
            throw;
        }
    }

    private string BuildPromptWithParameters(string prompt, VideoGenerationConfigDto? options)
    {
        if (options == null) return prompt;

        var sb = new StringBuilder(prompt);
        
        // Add BytePlus-specific parameters as described in the documentation
        sb.AppendLine($" --resolution {ConvertResolution(options.Resolution)}");
        sb.AppendLine($" --duration {options.Duration}");
        
        // Convert our options to BytePlus format
        if (options.MotionStrength > 0)
        {
            sb.AppendLine($" --motion {ConvertMotionStrength(options.MotionStrength)}");
        }
        
        return sb.ToString();
    }

    private string ConvertResolution(string resolution)
    {
        return resolution switch
        {
            "1080p" => "1080p",
            "720p" => "720p", 
            "480p" => "480p",
            _ => "720p" // Default
        };
    }

    private string ConvertMotionStrength(float strength)
    {
        return strength switch
        {
            < 0.3f => "low",
            > 0.7f => "high", 
            _ => "medium"
        };
    }
    
    private bool IsValidImageUrl(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            
            // Must be HTTP or HTTPS
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Image URL must use HTTP or HTTPS protocol: {ImageUrl}", imageUrl);
                return false;
            }
            
            // Check for common image file extensions
            var path = uri.AbsolutePath.ToLowerInvariant();
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            
            // Allow URLs without extensions (many image hosting services don't use file extensions)
            // But if there is an extension, it should be a valid image format
            if (path.Contains('.'))
            {
                var hasValidExtension = validExtensions.Any(ext => path.EndsWith(ext));
                if (!hasValidExtension)
                {
                    _logger.LogWarning("Image URL should point to a valid image format: {ImageUrl}", imageUrl);
                    return false;
                }
            }
            
            return true;
        }
        catch (UriFormatException ex)
        {
            _logger.LogWarning(ex, "Invalid URL format: {ImageUrl}", imageUrl);
            return false;
        }
    }
}

// BytePlus API Response Models
public class BytePlusVideoTaskResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }
}

public class BytePlusVideoTaskStatus
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonProperty("content")]
    public BytePlusVideoContent? Content { get; set; }
    
    [JsonProperty("usage")]
    public BytePlusUsage? Usage { get; set; }
    
    [JsonProperty("error")]
    public BytePlusError? Error { get; set; }
    
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }
    
    [JsonProperty("updated_at")]
    public long UpdatedAt { get; set; }
}

public class BytePlusVideoContent
{
    [JsonProperty("video_url")]
    public string VideoUrl { get; set; } = string.Empty;
}

public class BytePlusUsage
{
    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }
}

public class BytePlusError
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
}
