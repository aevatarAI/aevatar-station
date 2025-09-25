using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// Event published when a WebAPI request is made
/// </summary>
[GenerateSerializer]
public class WebApiRequestEvent : EventBase
{
    /// <summary>
    /// HTTP method
    /// </summary>
    [Id(0)] public string Method { get; init; } = string.Empty;
    
    /// <summary>
    /// API endpoint
    /// </summary>
    [Id(1)] public string Endpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// Request payload (serialized)
    /// </summary>
    [Id(2)] public string? PayloadJson { get; init; }
    
    /// <summary>
    /// Request headers
    /// </summary>
    [Id(3)] public Dictionary<string, string> Headers { get; init; } = new();
    
    /// <summary>
    /// Request timestamp
    /// </summary>
    [Id(4)] public DateTime RequestTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a WebAPI response is received
/// </summary>
[GenerateSerializer]
public class WebApiResponseEvent : EventBase
{
    /// <summary>
    /// Request identifier
    /// </summary>
    [Id(0)] public string RequestId { get; init; } = string.Empty;
    
    /// <summary>
    /// HTTP status code
    /// </summary>
    [Id(1)] public int StatusCode { get; init; }
    
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [Id(2)] public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    [Id(3)] public long ElapsedMilliseconds { get; init; }
    
    /// <summary>
    /// Error message if request failed
    /// </summary>
    [Id(4)] public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Response headers
    /// </summary>
    [Id(5)] public Dictionary<string, string> ResponseHeaders { get; init; } = new();
    
    /// <summary>
    /// Response timestamp
    /// </summary>
    [Id(6)] public DateTime ResponseTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a WebAPI error occurs
/// </summary>
[GenerateSerializer]
public class WebApiErrorEvent : EventBase
{
    /// <summary>
    /// Request identifier
    /// </summary>
    [Id(0)] public string RequestId { get; init; } = string.Empty;
    
    /// <summary>
    /// Error type
    /// </summary>
    [Id(1)] public string ErrorType { get; init; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    [Id(2)] public string ErrorMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// Endpoint that failed
    /// </summary>
    [Id(3)] public string Endpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    [Id(4)] public int RetryCount { get; init; }
    
    /// <summary>
    /// Error timestamp
    /// </summary>
    [Id(5)] public DateTime ErrorTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a health check is performed
/// </summary>
[GenerateSerializer]
public class WebApiHealthCheckEvent : EventBase
{
    /// <summary>
    /// Whether the API is healthy
    /// </summary>
    [Id(0)] public bool IsHealthy { get; init; }
    
    /// <summary>
    /// Health status message
    /// </summary>
    [Id(1)] public string StatusMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    [Id(2)] public int ConsecutiveFailures { get; init; }
    
    /// <summary>
    /// Health check timestamp
    /// </summary>
    [Id(3)] public DateTime CheckTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when configuration is updated
/// </summary>
[GenerateSerializer]
public class WebApiConfigurationUpdatedEvent : EventBase
{
    /// <summary>
    /// Base URL
    /// </summary>
    [Id(0)] public string BaseUrl { get; init; } = string.Empty;
    
    /// <summary>
    /// Authentication type
    /// </summary>
    [Id(1)] public string AuthenticationType { get; init; } = string.Empty;
    
    /// <summary>
    /// When configuration was updated
    /// </summary>
    [Id(2)] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when metrics are reset
/// </summary>
[GenerateSerializer]
public class WebApiMetricsResetEvent : EventBase
{
    /// <summary>
    /// Previous total requests
    /// </summary>
    [Id(0)] public long PreviousTotalRequests { get; init; }
    
    /// <summary>
    /// Previous successful requests
    /// </summary>
    [Id(1)] public long PreviousSuccessfulRequests { get; init; }
    
    /// <summary>
    /// Previous failed requests
    /// </summary>
    [Id(2)] public long PreviousFailedRequests { get; init; }
    
    /// <summary>
    /// When metrics were reset
    /// </summary>
    [Id(3)] public DateTime ResetAt { get; init; } = DateTime.UtcNow;
}
