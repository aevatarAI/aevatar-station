using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// WebAPI GAgent State
/// </summary>
[GenerateSerializer]
public class WebApiGAgentState : StateBase
{
    /// <summary>
    /// GAgent configuration
    /// </summary>
    [Id(1)] public WebApiGAgentConfiguration? Configuration { get; set; }
    
    /// <summary>
    /// Request history (limited to latest records)
    /// </summary>
    [Id(2)] public List<WebApiRequestRecord> RequestHistory { get; set; } = new();
    
    /// <summary>
    /// API metrics
    /// </summary>
    [Id(3)] public WebApiMetrics Metrics { get; set; } = new();
    
    /// <summary>
    /// Current health status
    /// </summary>
    [Id(4)] public WebApiHealthStatus HealthStatus { get; set; } = new();
    
    /// <summary>
    /// Last activity timestamp
    /// </summary>
    [Id(5)] public DateTime LastActivityAt { get; set; }
    
    /// <summary>
    /// Custom properties for extensibility
    /// </summary>
    [Id(6)] public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// WebAPI GAgent Configuration
/// </summary>
[GenerateSerializer]
public class WebApiGAgentConfiguration : ConfigurationBase
{
    /// <summary>
    /// Base URL for API calls
    /// </summary>
    [Id(0)] public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    [Id(1)] public WebApiAuthenticationConfig Authentication { get; set; } = new();
    
    /// <summary>
    /// Timeout settings
    /// </summary>
    [Id(2)] public WebApiTimeoutConfig Timeouts { get; set; } = new();
    
    /// <summary>
    /// Retry configuration
    /// </summary>
    [Id(3)] public WebApiRetryConfig Retry { get; set; } = new();
    
    /// <summary>
    /// Default headers to include in all requests
    /// </summary>
    [Id(4)] public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    
    /// <summary>
    /// Enable request/response logging
    /// </summary>
    [Id(5)] public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// Enable metrics collection
    /// </summary>
    [Id(6)] public bool EnableMetrics { get; set; } = true;
    
    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    [Id(7)] public WebApiRateLimitConfig RateLimit { get; set; } = new();
}

/// <summary>
/// Authentication configuration
/// </summary>
[GenerateSerializer]
public class WebApiAuthenticationConfig
{
    /// <summary>
    /// Authentication type
    /// </summary>
    [Id(0)] public WebApiAuthenticationType Type { get; set; } = WebApiAuthenticationType.None;
    
    /// <summary>
    /// Bearer token for Bearer authentication
    /// </summary>
    [Id(1)] public string? BearerToken { get; set; }
    
    /// <summary>
    /// API key value
    /// </summary>
    [Id(2)] public string? ApiKey { get; set; }
    
    /// <summary>
    /// API key header name (default: X-API-Key)
    /// </summary>
    [Id(3)] public string? ApiKeyHeader { get; set; } = "X-API-Key";
    
    /// <summary>
    /// Username for Basic authentication
    /// </summary>
    [Id(4)] public string? Username { get; set; }
    
    /// <summary>
    /// Password for Basic authentication
    /// </summary>
    [Id(5)] public string? Password { get; set; }
    
    /// <summary>
    /// Custom headers for authentication
    /// </summary>
    [Id(6)] public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Authentication types supported
/// </summary>
[GenerateSerializer]
public enum WebApiAuthenticationType
{
    None,
    BearerToken,
    ApiKey,
    BasicAuth,
    CustomHeaders
}

/// <summary>
/// Timeout configuration
/// </summary>
[GenerateSerializer]
public class WebApiTimeoutConfig
{
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Id(0)] public int RequestTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Id(1)] public int ConnectionTimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Retry configuration
/// </summary>
[GenerateSerializer]
public class WebApiRetryConfig
{
    /// <summary>
    /// Enable retry mechanism
    /// </summary>
    [Id(0)] public bool EnableRetry { get; set; } = true;
    
    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    [Id(1)] public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Base delay between retries in seconds
    /// </summary>
    [Id(2)] public int BaseDelaySeconds { get; set; } = 1;
    
    /// <summary>
    /// Maximum delay between retries in seconds
    /// </summary>
    [Id(3)] public int MaxDelaySeconds { get; set; } = 30;
    
    /// <summary>
    /// Backoff multiplier for exponential backoff
    /// </summary>
    [Id(4)] public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// HTTP status codes that should trigger retry
    /// </summary>
    [Id(5)] public List<int> RetryableStatusCodes { get; set; } = new() { 429, 500, 502, 503, 504 };
}

/// <summary>
/// Rate limiting configuration
/// </summary>
[GenerateSerializer]
public class WebApiRateLimitConfig
{
    /// <summary>
    /// Enable rate limiting
    /// </summary>
    [Id(0)] public bool EnableRateLimit { get; set; } = false;
    
    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    [Id(1)] public int RequestsPerMinute { get; set; } = 60;
    
    /// <summary>
    /// Burst capacity
    /// </summary>
    [Id(2)] public int BurstSize { get; set; } = 10;
}

/// <summary>
/// WebAPI response wrapper
/// </summary>
[GenerateSerializer]
public class WebApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [Id(0)] public bool IsSuccess { get; set; }
    
    /// <summary>
    /// HTTP status code
    /// </summary>
    [Id(1)] public int StatusCode { get; set; }
    
    /// <summary>
    /// Response data
    /// </summary>
    [Id(2)] public T? Data { get; set; }
    
    /// <summary>
    /// Error message if request failed
    /// </summary>
    [Id(3)] public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Response headers
    /// </summary>
    [Id(4)] public Dictionary<string, string> Headers { get; set; } = new();
    
    /// <summary>
    /// Request timestamp
    /// </summary>
    [Id(5)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    [Id(6)] public long ElapsedMilliseconds { get; set; }
    
    /// <summary>
    /// Unique request identifier
    /// </summary>
    [Id(7)] public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Request record for history tracking
/// </summary>
[GenerateSerializer]
public class WebApiRequestRecord
{
    /// <summary>
    /// Request identifier
    /// </summary>
    [Id(0)] public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP method
    /// </summary>
    [Id(1)] public string Method { get; set; } = string.Empty;
    
    /// <summary>
    /// Request endpoint
    /// </summary>
    [Id(2)] public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Request timestamp
    /// </summary>
    [Id(3)] public DateTime RequestTime { get; set; }
    
    /// <summary>
    /// Response timestamp
    /// </summary>
    [Id(4)] public DateTime ResponseTime { get; set; }
    
    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    [Id(5)] public long ElapsedMilliseconds { get; set; }
    
    /// <summary>
    /// HTTP status code
    /// </summary>
    [Id(6)] public int StatusCode { get; set; }
    
    /// <summary>
    /// Whether request was successful
    /// </summary>
    [Id(7)] public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if request failed
    /// </summary>
    [Id(8)] public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    [Id(9)] public int RetryCount { get; set; }
    
    /// <summary>
    /// Request headers
    /// </summary>
    [Id(10)] public Dictionary<string, string> RequestHeaders { get; set; } = new();
    
    /// <summary>
    /// Response headers
    /// </summary>
    [Id(11)] public Dictionary<string, string> ResponseHeaders { get; set; } = new();
}

/// <summary>
/// API metrics
/// </summary>
[GenerateSerializer]
public class WebApiMetrics
{
    /// <summary>
    /// Total number of requests
    /// </summary>
    [Id(0)] public long TotalRequests { get; set; }
    
    /// <summary>
    /// Number of successful requests
    /// </summary>
    [Id(1)] public long SuccessfulRequests { get; set; }
    
    /// <summary>
    /// Number of failed requests
    /// </summary>
    [Id(2)] public long FailedRequests { get; set; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    [Id(3)] public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Total number of retries
    /// </summary>
    [Id(4)] public long TotalRetries { get; set; }
    
    /// <summary>
    /// Count by status code
    /// </summary>
    [Id(5)] public Dictionary<int, long> StatusCodeCounts { get; set; } = new();
    
    /// <summary>
    /// When metrics were last reset
    /// </summary>
    [Id(6)] public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health status
/// </summary>
[GenerateSerializer]
public class WebApiHealthStatus
{
    /// <summary>
    /// Whether the API is healthy
    /// </summary>
    [Id(0)] public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Status description
    /// </summary>
    [Id(1)] public string StatusMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Last health check timestamp
    /// </summary>
    [Id(2)] public DateTime LastCheckAt { get; set; }
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    [Id(3)] public int ConsecutiveFailures { get; set; }
    
    /// <summary>
    /// Additional health details
    /// </summary>
    [Id(4)] public Dictionary<string, object> HealthDetails { get; set; } = new();
}
