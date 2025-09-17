using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Device.Abstractions;
using Confluent.Kafka;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.GEvent;

namespace Aevatar.GAgents.Device.State;

/// <summary>
/// Device GAgent state class
/// </summary>
[GenerateSerializer]
public class DeviceGAgentState : MemberState
{
    /// <summary>
    /// Device connection configuration
    /// </summary>
    [Id(0)] public DeviceConnectionConfig? ConnectionConfig { get; set; }
    
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(1)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device name
    /// </summary>
    [Id(2)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Device type
    /// </summary>
    [Id(3)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether connected
    /// </summary>
    [Id(4)] public bool IsConnected { get; set; }
    
    /// <summary>
    /// Connection status
    /// </summary>
    [Id(5)] public DeviceConnectionStatus ConnectionStatus { get; set; } = DeviceConnectionStatus.Disconnected;
    
    /// <summary>
    /// Connection time
    /// </summary>
    [Id(6)] public DateTime? ConnectedAt { get; set; }
    
    /// <summary>
    /// Last status update time
    /// </summary>
    [Id(7)] public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether monitoring is enabled
    /// </summary>
    [Id(8)] public bool MonitoringEnabled { get; set; }
    
    /// <summary>
    /// Monitoring interval (seconds)
    /// </summary>
    [Id(9)] public int MonitoringIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Monitoring state change time
    /// </summary>
    [Id(10)] public DateTime? MonitoringStateChangedAt { get; set; }
    
    /// <summary>
    /// Last error message
    /// </summary>
    [Id(11)] public string? LastErrorMessage { get; set; }
    
    /// <summary>
    /// Property change history records
    /// </summary>
    [Id(12)] public List<DevicePropertyChangeRecord> PropertyChangeHistory { get; set; } = new();
    
    /// <summary>
    /// Last property change time
    /// </summary>
    [Id(13)] public DateTime? LastPropertyChangeAt { get; set; }
    
    /// <summary>
    /// Device statistics
    /// </summary>
    [Id(14)] public DeviceStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Device connection configuration
/// </summary>
[GenerateSerializer]
public class DeviceConnectionConfig : MemberConfigDto
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(0)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device name
    /// </summary>
    [Id(1)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Device type
    /// </summary>
    [Id(2)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection string or configuration information
    /// </summary>
    [Id(3)] public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection timeout (seconds)
    /// </summary>
    [Id(4)] public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Retry attempts
    /// </summary>
    [Id(5)] public int RetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Retry interval (seconds)
    /// </summary>
    [Id(6)] public int RetryIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Extended configuration properties
    /// </summary>
    [Id(7)] public Dictionary<string, string> ExtendedProperties { get; set; } = new();
}

/// <summary>
/// Device property change record
/// </summary>
[GenerateSerializer]
public class DevicePropertyChangeRecord
{
    /// <summary>
    /// Property name
    /// </summary>
    [Id(0)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Old value
    /// </summary>
    [Id(1)] public object? OldValue { get; set; }
    
    /// <summary>
    /// New value
    /// </summary>
    [Id(2)] public object? NewValue { get; set; }
    
    /// <summary>
    /// Change time
    /// </summary>
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device statistics
/// </summary>
[GenerateSerializer]
public class DeviceStatistics
{
    /// <summary>
    /// Total connections
    /// </summary>
    [Id(0)] public long TotalConnections { get; set; }
    
    /// <summary>
    /// Connection failures
    /// </summary>
    [Id(1)] public long ConnectionFailures { get; set; }
    
    /// <summary>
    /// Property reads
    /// </summary>
    [Id(2)] public long PropertyReads { get; set; }
    
    /// <summary>
    /// Property writes
    /// </summary>
    [Id(3)] public long PropertyWrites { get; set; }
    
    /// <summary>
    /// Action executions
    /// </summary>
    [Id(4)] public long ActionExecutions { get; set; }
    
    /// <summary>
    /// Error count
    /// </summary>
    [Id(5)] public long ErrorCount { get; set; }
    
    /// <summary>
    /// Last reset time
    /// </summary>
    [Id(6)] public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device status summary
/// </summary>
[GenerateSerializer]
public class DeviceStatusSummary
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(0)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device name
    /// </summary>
    [Id(1)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Device type
    /// </summary>
    [Id(2)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether connected
    /// </summary>
    [Id(3)] public bool IsConnected { get; set; }
    
    /// <summary>
    /// Connection status
    /// </summary>
    [Id(4)] public DeviceConnectionStatus Status { get; set; }
    
    /// <summary>
    /// Whether healthy
    /// </summary>
    [Id(5)] public bool IsHealthy { get; set; } = true;
    
    /// <summary>
    /// Last updated time
    /// </summary>
    [Id(6)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Current property values
    /// </summary>
    [Id(7)] public Dictionary<string, object?> Properties { get; set; } = new();
    
    /// <summary>
    /// Health status details
    /// </summary>
    [Id(8)] public Dictionary<string, object> HealthDetails { get; set; } = new();
    
    /// <summary>
    /// Error message
    /// </summary>
    [Id(9)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// Device GAgent state log event base class
/// </summary>
[GenerateSerializer]
public abstract class DeviceGAgentStateLogEvent : StateLogEventBase<DeviceGAgentStateLogEvent>
{
}

/// <summary>
/// Device connection initialization log event
/// </summary>
[GenerateSerializer]
public class DeviceConnectionInitializedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// Connection configuration
    /// </summary>
    [Id(0)] public DeviceConnectionConfig Config { get; set; } = new();
    
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(1)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device name
    /// </summary>
    [Id(2)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Device type
    /// </summary>
    [Id(3)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection time
    /// </summary>
    [Id(4)] public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device monitoring state change log event
/// </summary>
[GenerateSerializer]
public class DeviceMonitoringStateChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// Whether monitoring is enabled
    /// </summary>
    [Id(0)] public bool Enabled { get; set; }
    
    /// <summary>
    /// Change time
    /// </summary>
    [Id(1)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event of device Connection status changes logs
/// </summary>
[GenerateSerializer]
public class DeviceConnectionStatusChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// Old status
    /// </summary>
    [Id(0)] public DeviceConnectionStatus OldStatus { get; set; }
    
    /// <summary>
    /// New status
    /// </summary>
    [Id(1)] public DeviceConnectionStatus NewStatus { get; set; }
    
    /// <summary>
    /// Change time
    /// </summary>
    [Id(2)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Change reason
    /// </summary>
    [Id(3)] public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Device property change log event
/// </summary>
[GenerateSerializer]
public class DevicePropertyChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// Property name
    /// </summary>
    [Id(0)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Old value
    /// </summary>
    [Id(1)] public object? OldValue { get; set; }
    
    /// <summary>
    /// New value
    /// </summary>
    [Id(2)] public object? NewValue { get; set; }
    
    /// <summary>
    /// Change time
    /// </summary>
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
