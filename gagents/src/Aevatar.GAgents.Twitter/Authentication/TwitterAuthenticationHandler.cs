using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Twitter.Authentication;

/// <summary>
/// Handles Twitter API authentication (OAuth 1.0a and OAuth 2.0 Bearer Token)
/// </summary>
public class TwitterAuthenticationHandler : ITwitterAuthenticationHandler
{
    private readonly ILogger<TwitterAuthenticationHandler> _logger;

    public TwitterAuthenticationHandler(ILogger<TwitterAuthenticationHandler> logger)
    {
        _logger = logger;
    }

    public TwitterAuthenticationResult PrepareAuthentication(
        TwitterAuthenticationRequest request,
        TwitterAuthenticationCredentials credentials)
    {
        var result = new TwitterAuthenticationResult();

        // Determine authentication mode based on endpoint and available credentials
        var authMode = DetermineAuthenticationMode(request, credentials);

        switch (authMode)
        {
            case TwitterAuthenticationMode.BearerToken:
                if (string.IsNullOrEmpty(credentials.BearerToken))
                {
                    result.ErrorMessage = "Bearer token is required for this endpoint";
                    return result;
                }
                result.Headers["Authorization"] = $"Bearer {credentials.BearerToken}";
                result.AuthMode = TwitterAuthenticationMode.BearerToken;
                result.IsSuccess = true;
                break;

            case TwitterAuthenticationMode.OAuth1a:
                if (!ValidateOAuth1aCredentials(credentials))
                {
                    result.ErrorMessage = "OAuth 1.0a credentials are incomplete";
                    return result;
                }
                return PrepareOAuth1aHeaders(request, credentials);

            default:
                result.ErrorMessage = "No suitable authentication method available";
                break;
        }

        return result;
    }

    private TwitterAuthenticationMode DetermineAuthenticationMode(
        TwitterAuthenticationRequest request,
        TwitterAuthenticationCredentials credentials)
    {
        var requiresOAuth1a = RequiresOAuth1a(request.Method, request.Url);

        if (requiresOAuth1a)
        {
            return TwitterAuthenticationMode.OAuth1a;
        }

        // Prefer Bearer token for GET requests if available
        if (!string.IsNullOrEmpty(credentials.BearerToken))
        {
            return TwitterAuthenticationMode.BearerToken;
        }

        // Fall back to OAuth 1.0a if available
        if (ValidateOAuth1aCredentials(credentials))
        {
            return TwitterAuthenticationMode.OAuth1a;
        }

        return TwitterAuthenticationMode.None;
    }

    private bool RequiresOAuth1a(HttpMethod method, string url)
    {
        // These endpoints require OAuth 1.0a user context
        var oauth1aEndpoints = new[]
        {
            "/tweets", // POST, DELETE
            "/users/*/likes", // POST, DELETE
            "/users/*/retweets", // POST, DELETE
            "/users/*/following", // POST, DELETE
            "/users/me" // GET (when using OAuth 1.0a)
        };

        // Write operations generally require OAuth 1.0a
        if (method != HttpMethod.Get)
        {
            return true;
        }

        // Check specific endpoints
        return oauth1aEndpoints.Any(endpoint => 
            url.Contains(endpoint.Replace("*", "")));
    }

    private bool ValidateOAuth1aCredentials(TwitterAuthenticationCredentials credentials)
    {
        return !string.IsNullOrEmpty(credentials.ConsumerKey) &&
               !string.IsNullOrEmpty(credentials.ConsumerSecret) &&
               !string.IsNullOrEmpty(credentials.OAuthToken) &&
               !string.IsNullOrEmpty(credentials.OAuthTokenSecret);
    }

    private TwitterAuthenticationResult PrepareOAuth1aHeaders(
        TwitterAuthenticationRequest request,
        TwitterAuthenticationCredentials credentials)
    {
        var result = new TwitterAuthenticationResult();

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonce = GenerateNonce();

            var oauthParams = new Dictionary<string, string>
            {
                ["oauth_consumer_key"] = credentials.ConsumerKey!,
                ["oauth_nonce"] = nonce,
                ["oauth_signature_method"] = "HMAC-SHA1",
                ["oauth_timestamp"] = timestamp,
                ["oauth_token"] = credentials.OAuthToken!,
                ["oauth_version"] = "1.0"
            };

            // Add request parameters for signature generation
            var allParams = new Dictionary<string, string>(oauthParams);
            if (request.Parameters != null)
            {
                foreach (var param in request.Parameters)
                {
                    allParams[param.Key] = param.Value;
                }
            }

            // Generate signature
            var signature = GenerateOAuth1aSignature(
                request.Method,
                request.Url,
                allParams,
                credentials.ConsumerSecret!,
                credentials.OAuthTokenSecret!);

            oauthParams["oauth_signature"] = signature;

            // Build Authorization header
            var authHeader = "OAuth " + string.Join(", ",
                oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\""));

            result.Headers["Authorization"] = authHeader;
            result.IsSuccess = true;
            result.AuthMode = TwitterAuthenticationMode.OAuth1a;

            _logger.LogDebug("OAuth 1.0a authentication prepared for {Method} {Url}", 
                request.Method, request.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare OAuth 1.0a headers");
            result.ErrorMessage = $"OAuth 1.0a preparation failed: {ex.Message}";
        }

        return result;
    }

    private string GenerateNonce()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string GenerateOAuth1aSignature(
        HttpMethod method,
        string url,
        Dictionary<string, string> parameters,
        string consumerSecret,
        string tokenSecret)
    {
        // Sort parameters alphabetically
        var sortedParams = parameters
            .OrderBy(p => p.Key)
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
            .ToList();

        var paramString = string.Join("&", sortedParams);
        var baseString = $"{method.Method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
        var signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Request for Twitter authentication
/// </summary>
public class TwitterAuthenticationRequest
{
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
}

/// <summary>
/// Credentials for Twitter authentication
/// </summary>
public class TwitterAuthenticationCredentials
{
    public string? BearerToken { get; set; }
    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    public string? OAuthToken { get; set; }
    public string? OAuthTokenSecret { get; set; }
}

/// <summary>
/// Result of authentication preparation
/// </summary>
public class TwitterAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TwitterAuthenticationMode AuthMode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Twitter authentication modes
/// </summary>
public enum TwitterAuthenticationMode
{
    None,
    BearerToken,
    OAuth1a
}

/// <summary>
/// Interface for Twitter authentication handling
/// </summary>
public interface ITwitterAuthenticationHandler
{
    TwitterAuthenticationResult PrepareAuthentication(
        TwitterAuthenticationRequest request,
        TwitterAuthenticationCredentials credentials);
}