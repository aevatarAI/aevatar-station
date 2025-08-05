using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Aevatar.Application.Contracts.Analytics;

/// <summary>
/// Google Analytics event tracking request DTO
/// </summary>
public class GoogleAnalyticsEventRequestDto
{
    /// <summary>
    /// Client ID for user identification
    /// </summary>
    public string? ClientId { get; set; }
    
    public string? UserId { get; set; }
    
    /// <summary>
    /// Event name, such as 'login_success', 'signup_success', etc.
    /// </summary>
    [Required]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Event parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Timestamp in microseconds
    /// </summary>
    public long? TimestampMicros { get; set; }
}

/// <summary>
/// Google Analytics tracking response DTO
/// </summary>
public class GoogleAnalyticsEventResponseDto
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Google Analytics Measurement Protocol event data structure
/// </summary>
public class GAMeasurementProtocolPayload
{
    /// <summary>
    /// Client ID for user identification
    /// </summary>
    [JsonProperty("client_id")]
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID for cross-platform user identification
    /// </summary>
    [JsonProperty("user_id")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// Timestamp in microseconds since Unix epoch
    /// </summary>
    [JsonProperty("timestamp_micros")]
    public long? TimestampMicros { get; set; }
    
    /// <summary>
    /// Array of events to send
    /// </summary>
    [JsonProperty("events")]
    public List<GAEvent> Events { get; set; } = new List<GAEvent>();
}

/// <summary>
/// Google Analytics event internal data structure
/// </summary>
public class GAEvent
{
    /// <summary>
    /// Event name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Event parameters
    /// </summary>
    [JsonProperty("params")]
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
} 