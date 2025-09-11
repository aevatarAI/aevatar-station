using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.State;

namespace Aevatar.GAgents.Device.Events;

/// <summary>
/// Device property read result event
/// </summary>
[GenerateSerializer]
[Description("Result of device property read operation")]
public class DevicePropertyReadResultEvent : DeviceEventBase
{
    /// <summary>
    /// Property name
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Read value
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// Whether read was successful
    /// </summary>
    [Id(5)] public bool Success { get; set; } = true;
    
    /// <summary>
    /// Error message (if read failed)
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// Device property write result event
/// </summary>
[GenerateSerializer]
[Description("Result of device property write operation")]
public class DevicePropertyWriteResultEvent : DeviceEventBase
{
    /// <summary>
    /// Property name
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Written value
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// Whether write was successful
    /// </summary>
    [Id(5)] public bool Success { get; set; }
    
    /// <summary>
    /// Error message (if write failed)
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// Device action execution result event
/// </summary>
[GenerateSerializer]
[Description("Result of device action execution")]
public class DeviceActionExecutionResultEvent : DeviceEventBase
{
    /// <summary>
    /// Action name
    /// </summary>
    [Id(3)] public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether execution was successful
    /// </summary>
    [Id(4)] public bool Success { get; set; }
    
    /// <summary>
    /// Execution result
    /// </summary>
    [Id(5)] public object? Result { get; set; }
    
    /// <summary>
    /// Error message (if execution failed)
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Execution duration (milliseconds)
    /// </summary>
    [Id(7)] public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Device status response event
/// </summary>
[GenerateSerializer]
[Description("Response to device status query")]
public class DeviceStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Status summary
    /// </summary>
    [Id(3)] public DeviceStatusSummary StatusSummary { get; set; } = new();
}

/// <summary>
/// Device connection operation result event
/// </summary>
[GenerateSerializer]
[Description("Result of device connect or disconnect operation")]
public class DeviceConnectionResultEvent : DeviceEventBase
{
    /// <summary>
    /// Whether action is succeeded
    /// </summary>
    [Id(3)] public bool Success { get; set; }
    
    /// <summary>
    /// Operation type (Connect/Disconnect/Reconnect)
    /// </summary>
    [Id(4)] public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message (if operation failed)
    /// </summary>
    [Id(5)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// Device health status response event
/// </summary>
[GenerateSerializer]
[Description("Response to device health check")]
public class DeviceHealthStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Is healthy
    /// </summary>
    [Id(3)] public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Status description
    /// </summary>
    [Id(4)] public string StatusDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Check time
    /// </summary>
    [Id(5)] public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Detailed information
    /// </summary>
    [Id(6)] public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Device supported actions list response event
/// </summary>
[GenerateSerializer]
[Description("Response to device supported actions list query")]
public class DeviceSupportedActionsResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Supported actions list
    /// </summary>
    [Id(3)] public List<DeviceActionInfo> SupportedActions { get; set; } = new();
}

/// <summary>
/// Device properties definition list response event
/// </summary>
[GenerateSerializer]
[Description("Response to device properties definition list query")]
public class DevicePropertiesResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Properties definition list
    /// </summary>
    [Id(3)] public List<DevicePropertyInfo> Properties { get; set; } = new();
}

/// <summary>
/// Batch property read result event
/// </summary>
[GenerateSerializer]
[Description("Multiple result of device property read operation")]
public class MultipleDevicePropertiesReadResultEvent : DeviceEventBase
{
    /// <summary>
    /// Property values dictionary
    /// </summary>
    [Id(3)] public Dictionary<string, object?> PropertyValues { get; set; } = new();
    
    /// <summary>
    /// Failed properties list
    /// </summary>
    [Id(4)] public List<string> FailedProperties { get; set; } = new();
    
    /// <summary>
    /// Error messages dictionary (property name -> error message)
    /// </summary>
    [Id(5)] public Dictionary<string, string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Batch property write result event
/// </summary>
[GenerateSerializer]
[Description("Multiple result of device property write operation")]
public class MultipleDevicePropertiesWriteResultEvent : DeviceEventBase
{
    /// <summary>
    /// Successfully written properties list
    /// </summary>
    [Id(3)] public List<string> SuccessfulProperties { get; set; } = new();
    
    /// <summary>
    /// Failed to write properties list
    /// </summary>
    [Id(4)] public List<string> FailedProperties { get; set; } = new();
    
    /// <summary>
    /// Error messages dictionary (property name -> error message)
    /// </summary>
    [Id(5)] public Dictionary<string, string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Device action information
/// </summary>
[GenerateSerializer]
public class DeviceActionInfo
{
    /// <summary>
    /// Action name
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Action description
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter information list
    /// </summary>
    [Id(3)] public List<DeviceActionParameterInfo> Parameters { get; set; } = new();
    
    /// <summary>
    /// Return type name
    /// </summary>
    [Id(4)] public string? ReturnTypeName { get; set; }
}

/// <summary>
/// Device action parameter information
/// </summary>
[GenerateSerializer]
public class DeviceActionParameterInfo
{
    /// <summary>
    /// Parameter name
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter description
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter type name
    /// </summary>
    [Id(3)] public string ParameterTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether required
    /// </summary>
    [Id(4)] public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// Default value
    /// </summary>
    [Id(5)] public object? DefaultValue { get; set; }
}

/// <summary>
/// Device property information
/// </summary>
[GenerateSerializer]
public class DevicePropertyInfo
{
    /// <summary>
    /// Property name
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Property description
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Property type name
    /// </summary>
    [Id(3)] public string PropertyTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Current value
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// Is readable
    /// </summary>
    [Id(5)] public bool IsReadable { get; set; } = true;
    
    /// <summary>
    /// Is writable
    /// </summary>
    [Id(6)] public bool IsWritable { get; set; } = false;
    
    /// <summary>
    /// Unit
    /// </summary>
    [Id(7)] public string? Unit { get; set; }
    
    /// <summary>
    /// Minimum value
    /// </summary>
    [Id(8)] public object? MinValue { get; set; }
    
    /// <summary>
    /// Maximum value
    /// </summary>
    [Id(9)] public object? MaxValue { get; set; }
    
    /// <summary>
    /// Last updated time
    /// </summary>
    [Id(10)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
