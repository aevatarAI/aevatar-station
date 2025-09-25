namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// WebAPI GAgent usage examples and patterns
/// </summary>
public static class WebApiGAgentExamples
{
    /// <summary>
    /// Example: Create a WebAPI GAgent with Bearer Token authentication
    /// </summary>
    public static WebApiGAgentConfiguration CreateBearerTokenConfig()
    {
        return new WebApiGAgentConfiguration
        {
            BaseUrl = "https://api.example.com/v1",
            Authentication = new WebApiAuthenticationConfig
            {
                Type = WebApiAuthenticationType.BearerToken,
                BearerToken = "your-bearer-token-here"
            },
            DefaultHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["User-Agent"] = "Aevatar-WebAPI-GAgent/1.0"
            },
            Timeouts = new WebApiTimeoutConfig
            {
                RequestTimeoutSeconds = 30,
                ConnectionTimeoutSeconds = 10
            },
            Retry = new WebApiRetryConfig
            {
                EnableRetry = true,
                MaxAttempts = 3,
                BaseDelaySeconds = 1,
                BackoffMultiplier = 2.0
            },
            EnableLogging = true,
            EnableMetrics = true
        };
    }

    /// <summary>
    /// Example: Create a WebAPI GAgent with API Key authentication
    /// </summary>
    public static WebApiGAgentConfiguration CreateApiKeyConfig()
    {
        return new WebApiGAgentConfiguration
        {
            BaseUrl = "https://api.service.com",
            Authentication = new WebApiAuthenticationConfig
            {
                Type = WebApiAuthenticationType.ApiKey,
                ApiKey = "your-api-key-here",
                ApiKeyHeader = "X-API-Key" // or "Authorization", etc.
            },
            RateLimit = new WebApiRateLimitConfig
            {
                EnableRateLimit = true,
                RequestsPerMinute = 100,
                BurstSize = 20
            }
        };
    }

    /// <summary>
    /// Example: Create a WebAPI GAgent with Basic Authentication
    /// </summary>
    public static WebApiGAgentConfiguration CreateBasicAuthConfig()
    {
        return new WebApiGAgentConfiguration
        {
            BaseUrl = "https://api.internal.com",
            Authentication = new WebApiAuthenticationConfig
            {
                Type = WebApiAuthenticationType.BasicAuth,
                Username = "your-username",
                Password = "your-password"
            },
            DefaultHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["Content-Type"] = "application/json"
            }
        };
    }

    /// <summary>
    /// Example: Create a WebAPI GAgent with Custom Headers authentication
    /// </summary>
    public static WebApiGAgentConfiguration CreateCustomHeadersConfig()
    {
        return new WebApiGAgentConfiguration
        {
            BaseUrl = "https://api.custom.com",
            Authentication = new WebApiAuthenticationConfig
            {
                Type = WebApiAuthenticationType.CustomHeaders,
                CustomHeaders = new Dictionary<string, string>
                {
                    ["X-Custom-Auth"] = "custom-auth-value",
                    ["X-Client-ID"] = "client-12345",
                    ["X-Signature"] = "computed-signature"
                }
            }
        };
    }

    /// <summary>
    /// Example: High-throughput configuration with aggressive retry and caching
    /// </summary>
    public static WebApiGAgentConfiguration CreateHighThroughputConfig()
    {
        return new WebApiGAgentConfiguration
        {
            BaseUrl = "https://api.fast.com",
            Authentication = new WebApiAuthenticationConfig
            {
                Type = WebApiAuthenticationType.BearerToken,
                BearerToken = "fast-api-token"
            },
            Timeouts = new WebApiTimeoutConfig
            {
                RequestTimeoutSeconds = 10,
                ConnectionTimeoutSeconds = 5
            },
            Retry = new WebApiRetryConfig
            {
                EnableRetry = true,
                MaxAttempts = 5,
                BaseDelaySeconds = 1,
                MaxDelaySeconds = 10,
                BackoffMultiplier = 1.5,
                RetryableStatusCodes = new List<int> { 429, 502, 503, 504 }
            },
            RateLimit = new WebApiRateLimitConfig
            {
                EnableRateLimit = true,
                RequestsPerMinute = 300,
                BurstSize = 50
            },
            EnableMetrics = true
        };
    }
}

/// <summary>
/// Common data models for WebAPI examples
/// </summary>
public class ExampleModels
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
    }
}

/// <summary>
/// Usage patterns and best practices
/// </summary>
public static class WebApiGAgentUsagePatterns
{
    /// <summary>
    /// Pattern: Basic CRUD operations
    /// </summary>
    /// <example>
    /// <code>
    /// // Configure the GAgent
    /// var config = WebApiGAgentExamples.CreateBearerTokenConfig();
    /// var webApiAgent = await gAgentFactory.GetGAgentAsync&lt;IWebApiGAgent&gt;(Guid.NewGuid(), config);
    /// 
    /// // GET request
    /// var user = await webApiAgent.GetAsync&lt;ExampleModels.User&gt;("/users/123");
    /// 
    /// // POST request
    /// var newUser = new ExampleModels.CreateUserRequest { Name = "John", Email = "john@example.com" };
    /// var created = await webApiAgent.PostAsync&lt;ExampleModels.User&gt;("/users", newUser);
    /// 
    /// // PUT request
    /// var update = new ExampleModels.UpdateUserRequest { Name = "John Updated" };
    /// var updated = await webApiAgent.PutAsync&lt;ExampleModels.User&gt;($"/users/{created.Data?.Id}", update);
    /// 
    /// // DELETE request
    /// var deleted = await webApiAgent.DeleteAsync&lt;string&gt;($"/users/{created.Data?.Id}");
    /// </code>
    /// </example>
    public static string BasicCrudPattern => "See example above";

    /// <summary>
    /// Pattern: Error handling and retry logic
    /// </summary>
    /// <example>
    /// <code>
    /// var response = await webApiAgent.GetAsync&lt;ExampleModels.User&gt;("/users/123");
    /// 
    /// if (response.IsSuccess)
    /// {
    ///     Console.WriteLine($"User: {response.Data?.Name}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Error: {response.ErrorMessage} (Status: {response.StatusCode})");
    ///     
    ///     // Check if it's a temporary error that might succeed on retry
    ///     if (response.StatusCode >= 500)
    ///     {
    ///         // Server error - might retry manually or check health
    ///         var health = await webApiAgent.GetHealthStatusAsync();
    ///         if (!health.IsHealthy)
    ///         {
    ///             Console.WriteLine($"API is unhealthy: {health.StatusMessage}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public static string ErrorHandlingPattern => "See example above";

    /// <summary>
    /// Pattern: Monitoring and metrics
    /// </summary>
    /// <example>
    /// <code>
    /// // Get current metrics
    /// var metrics = await webApiAgent.GetMetricsAsync();
    /// Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
    /// Console.WriteLine($"Success Rate: {(metrics.SuccessfulRequests * 100.0 / metrics.TotalRequests):F1}%");
    /// Console.WriteLine($"Average Response Time: {metrics.AverageResponseTimeMs:F1}ms");
    /// 
    /// // Get request history
    /// var history = await webApiAgent.GetRequestHistoryAsync(10);
    /// foreach (var record in history)
    /// {
    ///     Console.WriteLine($"{record.RequestTime}: {record.Method} {record.Endpoint} - {record.StatusCode} ({record.ElapsedMilliseconds}ms)");
    /// }
    /// 
    /// // Test connection health
    /// var isHealthy = await webApiAgent.TestConnectionAsync();
    /// Console.WriteLine($"Connection Health: {(isHealthy ? "OK" : "Failed")}");
    /// </code>
    /// </example>
    public static string MonitoringPattern => "See example above";

    /// <summary>
    /// Pattern: Dynamic configuration updates
    /// </summary>
    /// <example>
    /// <code>
    /// // Get current configuration
    /// var currentConfig = await webApiAgent.GetConfigurationAsync();
    /// 
    /// // Update configuration (e.g., new API key)
    /// var newConfig = currentConfig;
    /// newConfig.Authentication.BearerToken = "new-token-here";
    /// newConfig.Retry.MaxAttempts = 5; // Increase retry attempts
    /// 
    /// var updated = await webApiAgent.UpdateConfigurationAsync(newConfig);
    /// if (updated)
    /// {
    ///     Console.WriteLine("Configuration updated successfully");
    /// }
    /// </code>
    /// </example>
    public static string DynamicConfigPattern => "See example above";
}
