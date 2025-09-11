using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Abstractions;

namespace Aevatar.GAgents.Device.Events;

/// <summary>
/// Device event base class
/// </summary>
[GenerateSerializer]
public abstract class DeviceEventBase : EventBase
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
    /// Event timestamp
    /// </summary>
    [Id(2)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device property read event
/// </summary>
[GenerateSerializer]
[Description("Read device property value")]
public class ReadDevicePropertyEvent : DeviceEventBase
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
}

/// <summary>
/// Device property write event
/// </summary>
[GenerateSerializer]
[Description("Write device property value")]
public class WriteDevicePropertyEvent : DeviceEventBase
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Value to write (JSON serialized)
    /// </summary>
    [Id(4)] public string ValueJson { get; set; } = string.Empty;
}

/// <summary>
/// Execute device action event
/// </summary>
[GenerateSerializer]
[Description("Execute device action or command")]
public class ExecuteDeviceActionEvent : DeviceEventBase
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Id(3)] public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Action parameters (JSON serialized)
    /// </summary>
    [Id(4)] public string ParametersJson { get; set; } = "{}";
}

/// <summary>
/// Get device status event
/// </summary>
[GenerateSerializer]
[Description("Get device current status information")]
public class GetDeviceStatusEvent : DeviceEventBase
{
}

/// <summary>
/// Get device health status event
/// </summary>
[GenerateSerializer]
[Description("Get device health check status")]
public class GetDeviceHealthEvent : DeviceEventBase
{
}

/// <summary>
/// Connect device event
/// </summary>
[GenerateSerializer]
[Description("Connect to device")]
public class ConnectDeviceEvent : DeviceEventBase
{
}

/// <summary>
/// Disconnect device event
/// </summary>
[GenerateSerializer]
[Description("Disconnect device")]
public class DisconnectDeviceEvent : DeviceEventBase
{
}

/// <summary>
/// 重新Connect device event
/// </summary>
[GenerateSerializer]
[Description("Reconnect device")]
public class ReconnectDeviceEvent : DeviceEventBase
{
}

/// <summary>
/// Device property change notification event
/// </summary>
[GenerateSerializer]
[Description("Notification of device property value change")]
public class DevicePropertyChangedNotificationEvent : DeviceEventBase
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Old value (JSON serialized)
    /// </summary>
    [Id(4)] public string OldValueJson { get; set; } = string.Empty;
    
    /// <summary>
    /// New value (JSON serialized)
    /// </summary>
    [Id(5)] public string NewValueJson { get; set; } = string.Empty;
}

/// <summary>
/// Device connection status change notification event
/// </summary>
[GenerateSerializer]
[Description("Notification of device connection status change")]
public class DeviceConnectionStatusChangedNotificationEvent : DeviceEventBase
{
    /// <summary>
    /// 旧状态
    /// </summary>
    [Id(3)] public DeviceConnectionStatus OldStatus { get; set; }
    
    /// <summary>
    /// 新状态
    /// </summary>
    [Id(4)] public DeviceConnectionStatus NewStatus { get; set; }
    
    /// <summary>
    /// Change reason
    /// </summary>
    [Id(5)] public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Batch read device properties event
/// </summary>
[GenerateSerializer]
[Description("Batch read multiple device property values")]
public class ReadMultipleDevicePropertiesEvent : DeviceEventBase
{
    /// <summary>
    /// Property name list
    /// </summary>
    [Id(3)] public List<string> PropertyNames { get; set; } = new();
}

/// <summary>
/// Batch write device properties event
/// </summary>
[GenerateSerializer]
[Description("Batch write multiple device property values")]
public class WriteMultipleDevicePropertiesEvent : DeviceEventBase
{
    /// <summary>
    /// Property values dictionary (JSON serialized)
    /// </summary>
    [Id(3)] public string PropertyValuesJson { get; set; } = "{}";
}

/// <summary>
/// Get device supported actions list event
/// </summary>
[GenerateSerializer]
[Description("Get all supported actions list of the device")]
public class GetDeviceSupportedActionsEvent : DeviceEventBase
{
}

/// <summary>
/// Get device properties definition list event
/// </summary>
[GenerateSerializer]
[Description("Get all property definitions of the device")]
public class GetDevicePropertiesEvent : DeviceEventBase
{
}

/// <summary>
/// Device error event
/// </summary>
[GenerateSerializer]
[Description("Device error occurred")]
public class DeviceErrorEvent : DeviceEventBase
{
    /// <summary>
    /// Error code
    /// </summary>
    [Id(3)] public string ErrorCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    [Id(4)] public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Error details (JSON serialized)
    /// </summary>
    [Id(5)] public string ErrorDetailsJson { get; set; } = "{}";
}

/// <summary>
/// Device warning event
/// </summary>
[GenerateSerializer]
[Description("Device warning occurred")]
public class DeviceWarningEvent : DeviceEventBase
{
    /// <summary>
    /// Warning code
    /// </summary>
    [Id(3)] public string WarningCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Warning message
    /// </summary>
    [Id(4)] public string WarningMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Warning details (JSON serialized)
    /// </summary>
    [Id(5)] public string WarningDetailsJson { get; set; } = "{}";
}
