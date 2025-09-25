using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// Base class for WebAPI state log events
/// </summary>
[GenerateSerializer]
public abstract class WebApiStateLogEvent : StateLogEventBase<WebApiStateLogEvent>
{
}

/// <summary>
/// Event raised when configuration is updated
/// </summary>
[GenerateSerializer]
public class ConfigurationUpdatedLogEvent : WebApiStateLogEvent
{
    /// <summary>
    /// Serialized configuration JSON
    /// </summary>
    [Id(0)] public string ConfigurationJson { get; init; } = string.Empty;
    
    /// <summary>
    /// When configuration was updated
    /// </summary>
    [Id(1)] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a request is executed
/// </summary>
[GenerateSerializer]
public class RequestExecutedLogEvent : WebApiStateLogEvent
{
    /// <summary>
    /// Request record details
    /// </summary>
    [Id(0)] public WebApiRequestRecord RequestRecord { get; init; } = new();
}

/// <summary>
/// Event raised when metrics are updated
/// </summary>
[GenerateSerializer]
public class MetricsUpdatedLogEvent : WebApiStateLogEvent
{
    /// <summary>
    /// Updated metrics
    /// </summary>
    [Id(0)] public WebApiMetrics Metrics { get; init; } = new();
}

/// <summary>
/// Event raised when health status changes
/// </summary>
[GenerateSerializer]
public class HealthStatusChangedLogEvent : WebApiStateLogEvent
{
    /// <summary>
    /// New health status
    /// </summary>
    [Id(0)] public WebApiHealthStatus HealthStatus { get; init; } = new();
    
    /// <summary>
    /// Previous health status
    /// </summary>
    [Id(1)] public WebApiHealthStatus PreviousStatus { get; init; } = new();
    
    /// <summary>
    /// When status changed
    /// </summary>
    [Id(2)] public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when request history is cleaned up
/// </summary>
[GenerateSerializer]
public class RequestHistoryCleanupLogEvent : WebApiStateLogEvent
{
    /// <summary>
    /// Number of records removed
    /// </summary>
    [Id(0)] public int RecordsRemoved { get; init; }
    
    /// <summary>
    /// When cleanup occurred
    /// </summary>
    [Id(1)] public DateTime CleanedAt { get; init; } = DateTime.UtcNow;
}
