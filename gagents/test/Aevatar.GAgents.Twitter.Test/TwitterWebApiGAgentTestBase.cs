using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using Aevatar.GAgents.Twitter.GAgents;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace Aevatar.GAgents.Twitter.Test;

/// <summary>
/// Base class for Twitter GAgent tests following Aevatar testing patterns
/// </summary>
public abstract class AevatarTwitterTestBase : AevatarGAgentTestBase<AevatarTwitterTestModule>
{
    protected Mock<HttpMessageHandler> HttpMessageHandler { get; }
    protected HttpClient HttpClient { get; }
    
    protected AevatarTwitterTestBase()
    {
        HttpMessageHandler = new Mock<HttpMessageHandler>();
        HttpClient = new HttpClient(HttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.twitter.com/2")
        };

        // Provide custom handler to the shared TestHttpClientFactory used by the Silo
        Aevatar.GAgents.TestBase.Http.TestHttpClientFactoryProvider.CustomHandler = HttpMessageHandler.Object;
    }
    
    #region Helper Methods
    
    /// <summary>
    /// Create a test configuration for TwitterWebApiGAgent
    /// </summary>
    protected TwitterWebApiGAgentConfiguration CreateTestConfiguration(
        bool useBearerToken = true,
        bool useOAuth = true)
    {
        var config = new TwitterWebApiGAgentConfiguration
        {
            BaseApiUrl = "https://api.twitter.com/2",
            RequestTimeoutSeconds = 30,
            EnableRateLimiting = false // Disable for testing
        };
        
        if (useBearerToken)
        {
            config.BearerToken = "test-bearer-token";
        }
        
        if (useOAuth)
        {
            config.ConsumerKey = "test-consumer-key";
            config.ConsumerSecret = "test-consumer-secret";
            config.OAuthToken = "test-oauth-token";
            config.OAuthTokenSecret = "test-oauth-token-secret";
        }
        
        return config;
    }
    
    /// <summary>
    /// Setup HTTP response mock
    /// </summary>
    protected void SetupHttpResponse(HttpMethod method, string path, object responseData, 
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var responseContent = responseData is string str ? str : JsonSerializer.Serialize(responseData);
        
        HttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
    }

    protected void SetupHttpResponseSequence(HttpMethod method, string path, IEnumerable<object> responses,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var setup = HttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(path)),
                ItExpr.IsAny<CancellationToken>());

        foreach (var responseData in responses)
        {
            var responseContent = responseData is string s ? s : JsonSerializer.Serialize(responseData);
            setup = setup.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
        }
    }
    
    /// <summary>
    /// Create test tweet data
    /// </summary>
    protected static object CreateTestTweetResponse(string id = "1234567890", string text = "Test tweet")
    {
        return new
        {
            data = new
            {
                id = id,
                text = text,
                author_id = "user123",
                created_at = DateTime.UtcNow.ToString("O"),
                edit_history_tweet_ids = new[] { id }
            }
        };
    }
    
    /// <summary>
    /// Create test user profile data
    /// </summary>
    protected static object CreateTestUserResponse(string id = "user123", string username = "testuser")
    {
        return new
        {
            data = new
            {
                id = id,
                username = username,
                name = "Test User",
                description = "Test user description",
                created_at = DateTime.UtcNow.ToString("O"),
                verified = false
            }
        };
    }
    
    /// <summary>
    /// Create error response
    /// </summary>
    protected static object CreateErrorResponse(string message = "Error occurred", int code = 400)
    {
        return new
        {
            errors = new[]
            {
                new { message = message, code = code }
            }
        };
    }
    
    #endregion
}