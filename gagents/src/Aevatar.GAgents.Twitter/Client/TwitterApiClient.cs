using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.GAgents.Twitter.Authentication;
using Aevatar.GAgents.Twitter.RateLimiting;

namespace Aevatar.GAgents.Twitter.Client;

/// <summary>
/// Wrapper for Twitter API HTTP operations with authentication and rate limiting
/// </summary>
    public class TwitterApiClient : ITwitterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITwitterAuthenticationHandler _authHandler;
    private readonly ITwitterRateLimiter _rateLimiter;
    private readonly ILogger<TwitterApiClient> _logger;
    private readonly TwitterApiClientConfiguration _configuration;

    public TwitterApiClient(
        HttpClient httpClient,
        ITwitterAuthenticationHandler authHandler,
        ITwitterRateLimiter rateLimiter,
        ILogger<TwitterApiClient> logger,
        TwitterApiClientConfiguration configuration)
    {
        _httpClient = httpClient;
        _authHandler = authHandler;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _configuration = configuration;
    }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
            var response = await SendRequestAsync(method, endpoint, payload, cancellationToken);
            return JsonSerializer.Deserialize<T>(response, JsonOptions)!;
    }

    public async Task<string> SendRequestAsync(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        // Apply rate limiting
        var rateLimitKey = $"{method.Method} {endpoint}";
        await _rateLimiter.WaitForAvailabilityAsync(rateLimitKey, cancellationToken);

        // Prepare authentication
        var authRequest = new TwitterAuthenticationRequest
        {
            Method = method,
            Url = _configuration.BaseApiUrl + endpoint,
            Parameters = ExtractQueryParameters(endpoint)
        };

        var authCredentials = new TwitterAuthenticationCredentials
        {
            BearerToken = _configuration.BearerToken,
            ConsumerKey = _configuration.ConsumerKey,
            ConsumerSecret = _configuration.ConsumerSecret,
            OAuthToken = _configuration.OAuthToken,
            OAuthTokenSecret = _configuration.OAuthTokenSecret
        };

        var authResult = _authHandler.PrepareAuthentication(authRequest, authCredentials);
        if (!authResult.IsSuccess)
        {
            throw new InvalidOperationException($"Authentication failed: {authResult.ErrorMessage}");
        }

        // Create request
        var request = new HttpRequestMessage(method, _configuration.BaseApiUrl + endpoint);
        
        // Add authentication headers
        foreach (var header in authResult.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        // Add payload if present
        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // Set timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_configuration.RequestTimeoutSeconds));

        try
        {
            // Send request
            var response = await _httpClient.SendAsync(request, cts.Token);

            // Check for rate limit headers
            UpdateRateLimitInfo(response);

            // Handle response
            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                HandleApiError(response.StatusCode, responseContent, endpoint);
            }

            _logger.LogDebug("API request successful: {Method} {Endpoint}", method, endpoint);
            return responseContent;
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"Request to {endpoint} timed out after {_configuration.RequestTimeoutSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Method} {Endpoint}", method, endpoint);
            throw;
        }
    }

    private Dictionary<string, string>? ExtractQueryParameters(string endpoint)
    {
        var questionIndex = endpoint.IndexOf('?');
        if (questionIndex < 0)
        {
            return null;
        }

        var queryString = endpoint.Substring(questionIndex + 1);
        var parameters = new Dictionary<string, string>();

        foreach (var param in queryString.Split('&'))
        {
            var parts = param.Split('=');
            if (parts.Length == 2)
            {
                parameters[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }

        return parameters;
    }

    private void UpdateRateLimitInfo(HttpResponseMessage response)
    {
        // Twitter returns rate limit info in headers
        if (response.Headers.TryGetValues("x-rate-limit-limit", out var limitValues))
        {
            var limit = limitValues.FirstOrDefault();
            _logger.LogDebug("Rate limit: {Limit}", limit);
        }

        if (response.Headers.TryGetValues("x-rate-limit-remaining", out var remainingValues))
        {
            var remaining = remainingValues.FirstOrDefault();
            _logger.LogDebug("Rate limit remaining: {Remaining}", remaining);
        }

        if (response.Headers.TryGetValues("x-rate-limit-reset", out var resetValues))
        {
            var reset = resetValues.FirstOrDefault();
            _logger.LogDebug("Rate limit reset: {Reset}", reset);
        }
    }

    private void HandleApiError(HttpStatusCode statusCode, string responseContent, string endpoint)
    {
        _logger.LogError("API error for {Endpoint}: {StatusCode} - {Response}", 
            endpoint, statusCode, responseContent);

        var errorMessage = $"Twitter API error: {statusCode}";

        try
        {
            // Try to parse Twitter error response
            var errorDoc = JsonDocument.Parse(responseContent);
            if (errorDoc.RootElement.TryGetProperty("errors", out var errors) && 
                errors.GetArrayLength() > 0)
            {
                var firstError = errors[0];
                if (firstError.TryGetProperty("message", out var message))
                {
                    errorMessage = $"Twitter API error: {message.GetString()}";
                }
            }
            else if (errorDoc.RootElement.TryGetProperty("detail", out var detail))
            {
                errorMessage = $"Twitter API error: {detail.GetString()}";
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }

        // Throw standard HttpRequestException so Orleans can serialize across grain boundaries
        throw new HttpRequestException(errorMessage, null, statusCode);
    }
}

/// <summary>
/// Configuration for Twitter API client
/// </summary>
public class TwitterApiClientConfiguration
{
    public string BaseApiUrl { get; set; } = "https://api.twitter.com/2";
    public string? BearerToken { get; set; }
    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    public string? OAuthToken { get; set; }
    public string? OAuthTokenSecret { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Interface for Twitter API client
/// </summary>
public interface ITwitterApiClient
{
    Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default);

    Task<string> SendRequestAsync(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Base exception for Twitter API errors
/// </summary>
public class TwitterApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public TwitterApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
        : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception for rate limit errors
/// </summary>
public class TwitterRateLimitException : TwitterApiException
{
    public TwitterRateLimitException(string message) 
        : base(message, HttpStatusCode.TooManyRequests)
    {
    }
}

/// <summary>
/// Exception for authentication errors
/// </summary>
public class TwitterAuthenticationException : TwitterApiException
{
    public TwitterAuthenticationException(string message) 
        : base(message, HttpStatusCode.Unauthorized)
    {
    }
}

/// <summary>
/// Exception for authorization errors
/// </summary>
public class TwitterAuthorizationException : TwitterApiException
{
    public TwitterAuthorizationException(string message) 
        : base(message, HttpStatusCode.Forbidden)
    {
    }
}

/// <summary>
/// Exception for not found errors
/// </summary>
public class TwitterNotFoundException : TwitterApiException
{
    public TwitterNotFoundException(string message) 
        : base(message, HttpStatusCode.NotFound)
    {
    }
}