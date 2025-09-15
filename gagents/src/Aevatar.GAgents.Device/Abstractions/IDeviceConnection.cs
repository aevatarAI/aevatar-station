using System.ComponentModel;
using Orleans;

namespace Aevatar.GAgents.Device.Abstractions;

/// <summary>
/// Device connection abstraction interface that defines connection and communication protocols with physical or virtual devices
/// </summary>
public interface IDeviceConnection : IDisposable
{
    /// <summary>
    /// Device unique identifier
    /// </summary>
    string DeviceId { get; }
    
    /// <summary>
    /// Device name
    /// </summary>
    string DeviceName { get; }
    
    /// <summary>
    /// Device type
    /// </summary>
    string DeviceType { get; }
    
    /// <summary>
    /// Device connection status
    /// </summary>
    DeviceConnectionStatus Status { get; }
    
    /// <summary>
    /// Device properties collection containing all readable/writable properties of the device
    /// </summary>
    IReadOnlyDictionary<string, DeviceProperty> Properties { get; }
    
    /// <summary>
    /// Collection of operations/commands supported by the device
    /// </summary>
    IReadOnlyDictionary<string, DeviceAction> SupportedActions { get; }
    
    /// <summary>
    /// Device property change event
    /// </summary>
    event EventHandler<DevicePropertyChangedEventArgs>? PropertyChanged;
    
    /// <summary>
    /// Device connection status change event
    /// </summary>
    event EventHandler<DeviceConnectionStatusChangedEventArgs>? StatusChanged;
    
    /// <summary>
    /// Connect to device
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether connection was successful</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnect from device
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Read device property value
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property value</returns>
    Task<object?> ReadPropertyAsync(string propertyName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write device property value
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="value">Property value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether write was successful</returns>
    Task<bool> WritePropertyAsync(string propertyName, object value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute device operation/command
    /// </summary>
    /// <param name="actionName">Action name</param>
    /// <param name="parameters">Action parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action execution result</returns>
    Task<DeviceActionResult> ExecuteActionAsync(string actionName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get device health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device health status</returns>
    Task<DeviceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 设备连接状态枚举
/// </summary>
public enum DeviceConnectionStatus
{
    [Description("未连接")]
    Disconnected,
    
    [Description("正在连接")]
    Connecting,
    
    [Description("已连接")]
    Connected,
    
    [Description("Connection Error")]
    Error,
    
    [Description("Reconnecting")]
    Reconnecting
}

/// <summary>
/// Device property definition
/// </summary>
public class DeviceProperty
{
    /// <summary>
    /// Property name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Property display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Property description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Property data type
    /// </summary>
    public Type PropertyType { get; set; } = typeof(object);
    
    /// <summary>
    /// Property current value
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// Whether readable
    /// </summary>
    public bool IsReadable { get; set; } = true;
    
    /// <summary>
    /// Whether writable
    /// </summary>
    public bool IsWritable { get; set; } = false;
    
    /// <summary>
    /// Property unit (e.g., °C for temperature, % for humidity, etc.)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Property minimum value (applicable to numeric types)
    /// </summary>
    public object? MinValue { get; set; }
    
    /// <summary>
    /// Property maximum value (applicable to numeric types)
    /// </summary>
    public object? MaxValue { get; set; }
    
    /// <summary>
    /// Last updated time
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device action/command definition
/// </summary>
public class DeviceAction
{
    /// <summary>
    /// Action name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Action display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Action description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Action parameter definitions
    /// </summary>
    public Dictionary<string, DeviceActionParameter> Parameters { get; set; } = new();
    
    /// <summary>
    /// Return value type
    /// </summary>
    public Type? ReturnType { get; set; }
}

/// <summary>
/// 设备Action parameter definitions
/// </summary>
public class DeviceActionParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter type
    /// </summary>
    public Type ParameterType { get; set; } = typeof(object);
    
    /// <summary>
    /// Whether required parameter
    /// </summary>
    public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Device action execution result
/// </summary>
public class DeviceActionResult
{
    /// <summary>
    /// Whether operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Operation return value
    /// </summary>
    public object? Result { get; set; }
    
    /// <summary>
    /// Error message (when operation fails)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Operation execution time
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Operation duration (milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Device health status
/// </summary>
public class DeviceHealthStatus
{
    /// <summary>
    /// Whether healthy
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Health status description
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Last check time
    /// </summary>
    public DateTime LastCheckTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Detailed status information
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Device property change event arguments
/// </summary>
public class DevicePropertyChangedEventArgs : EventArgs
{
    /// <summary>
    /// Property name
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Old value
    /// </summary>
    public object? OldValue { get; set; }
    
    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; set; }
    
    /// <summary>
    /// Change time
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device connection status change event arguments
/// </summary>
public class DeviceConnectionStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Old status
    /// </summary>
    public DeviceConnectionStatus OldStatus { get; set; }
    
    /// <summary>
    /// New status
    /// </summary>
    public DeviceConnectionStatus NewStatus { get; set; }
    
    /// <summary>
    /// 状态Change time
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Change reason description
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Device connection information - serializable summary of device connection state
/// </summary>
[GenerateSerializer]
public class DeviceConnectionInfo
{
    /// <summary>
    /// Device unique identifier
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
    /// Device connection status
    /// </summary>
    [Id(3)] public DeviceConnectionStatus Status { get; set; }
    
    /// <summary>
    /// Whether device is currently connected
    /// </summary>
    [Id(4)] public bool IsConnected { get; set; }
    
    /// <summary>
    /// Last status update time
    /// </summary>
    [Id(5)] public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
}
