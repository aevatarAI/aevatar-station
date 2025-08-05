using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
    public string client_id { get; set; } = string.Empty;
    public string? user_id { get; set; }
    public long? timestamp_micros { get; set; }
    public List<GAEvent> events { get; set; } = new List<GAEvent>();
}

/// <summary>
/// Google Analytics event internal data structure
/// </summary>
public class GAEvent
{
    public string name { get; set; } = string.Empty;
    public Dictionary<string, object> parameters { get; set; } = new Dictionary<string, object>();
} 