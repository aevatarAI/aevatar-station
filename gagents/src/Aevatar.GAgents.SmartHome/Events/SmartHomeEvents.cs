using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.SmartHome.Events;

/// <summary>
/// Smart home event base class
/// </summary>
[GenerateSerializer]
public abstract class SmartHomeEventBase : EventBase
{
    /// <summary>
    /// Event timestamp
    /// </summary>
    [Id(0)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Send smart home command event
/// </summary>
[GenerateSerializer]
[Description("Send natural language command to smart home AI assistant")]
public class SendSmartHomeCommandEvent : SmartHomeEventBase
{
    /// <summary>
    /// Natural language command
    /// </summary>
    [Id(1)]
    [Description("Natural language command like 'Turn on the living room light' or 'Set bedroom temperature to 22 degrees'")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Optional user context or location
    /// </summary>
    [Id(2)]
    [Description("Optional context like user location or room preference")]
    public string? Context { get; set; }
}

/// <summary>
/// Smart home command result event
/// </summary>
[GenerateSerializer]
[Description("Result of smart home command execution")]
public class SmartHomeCommandResultEvent : SmartHomeEventBase
{
    /// <summary>
    /// Original command
    /// </summary>
    [Id(1)] public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Whether command was successful
    /// </summary>
    [Id(2)] public bool Success { get; set; }

    /// <summary>
    /// AI response or error message
    /// </summary>
    [Id(3)] public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Command execution timestamp
    /// </summary>
    [Id(4)] public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Register smart home device event
/// </summary>
[GenerateSerializer]
[Description("Register a new smart home device with the AI assistant")]
public class RegisterSmartHomeDeviceEvent : SmartHomeEventBase
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(1)]
    [Description("Unique device identifier (e.g., 'light001', 'temp001', 'switch001')")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    [Id(2)]
    [Description("Human-readable device name (e.g., '客厅主灯', '卧室台灯')")]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device type
    /// </summary>
    [Id(3)]
    [Description("Device type: 'smart-light', 'smart-switch', or 'temperature-sensor'")]
    public string DeviceType { get; set; } = string.Empty;
}

/// <summary>
/// Unregister smart home device event
/// </summary>
[GenerateSerializer]
[Description("Unregister a smart home device from the AI assistant")]
public class UnregisterSmartHomeDeviceEvent : SmartHomeEventBase
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(1)]
    [Description("Device ID to unregister")]
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// Get smart home status event
/// </summary>
[GenerateSerializer]
[Description("Get complete smart home status overview")]
public class GetSmartHomeStatusEvent : SmartHomeEventBase
{
}

/// <summary>
/// Smart home device registration result event
/// </summary>
[GenerateSerializer]
[Description("Result of device registration")]
public class SmartHomeDeviceRegistrationResultEvent : SmartHomeEventBase
{
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
    /// Whether registration was successful
    /// </summary>
    [Id(4)] public bool Success { get; set; }
}

/// <summary>
/// Smart home device unregistration result event
/// </summary>
[GenerateSerializer]
[Description("Result of device unregistration")]
public class SmartHomeDeviceUnregistrationResultEvent : SmartHomeEventBase
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(1)] public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Whether unregistration was successful
    /// </summary>
    [Id(2)] public bool Success { get; set; }
}

/// <summary>
/// Smart home status response event
/// </summary>
[GenerateSerializer]
[Description("Smart home status overview response")]
public class SmartHomeStatusResponseEvent : SmartHomeEventBase
{
    /// <summary>
    /// Status overview
    /// </summary>
    [Id(1)] public SmartHomeStatusOverview StatusOverview { get; set; } = new();
}

/// <summary>
/// Smart home device info
/// </summary>
[GenerateSerializer]
public record SmartHomeDeviceInfo
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Id(0)] public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    [Id(1)] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Device type
    /// </summary>
    [Id(2)] public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Registration timestamp
    /// </summary>
    [Id(3)] public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Whether device is currently online
    /// </summary>
    [Id(4)] public bool IsOnline { get; set; }

    /// <summary>
    /// Last seen timestamp
    /// </summary>
    [Id(5)] public DateTime? LastSeen { get; set; }
}

/// <summary>
/// Smart home command record
/// </summary>
[GenerateSerializer]
public class SmartHomeCommandRecord
{
    /// <summary>
    /// Command text
    /// </summary>
    [Id(0)] public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Command timestamp
    /// </summary>
    [Id(1)] public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether command was successful
    /// </summary>
    [Id(2)] public bool Success { get; set; }

    /// <summary>
    /// Command response
    /// </summary>
    [Id(3)] public string? Response { get; set; }
}

/// <summary>
/// Smart home command result
/// </summary>
[GenerateSerializer]
public class SmartHomeCommandResult
{
    /// <summary>
    /// Original command
    /// </summary>
    [Id(0)] public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Whether command was successful
    /// </summary>
    [Id(1)] public bool Success { get; set; }

    /// <summary>
    /// AI response or result message
    /// </summary>
    [Id(2)] public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Execution timestamp
    /// </summary>
    [Id(3)] public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Smart home status overview
/// </summary>
[GenerateSerializer]
public class SmartHomeStatusOverview
{
    /// <summary>
    /// Total number of devices
    /// </summary>
    [Id(0)] public int TotalDevices { get; set; }

    /// <summary>
    /// Number of online devices
    /// </summary>
    [Id(1)] public int OnlineDevices { get; set; }

    /// <summary>
    /// Number of offline devices
    /// </summary>
    [Id(2)] public int OfflineDevices { get; set; }

    /// <summary>
    /// Device count by type
    /// </summary>
    [Id(3)] public Dictionary<string, int> DevicesByType { get; set; } = new();

    /// <summary>
    /// Individual device statuses
    /// </summary>
    [Id(4)] public List<SmartHomeDeviceStatus> DeviceStatuses { get; set; } = new();

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Id(5)] public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Smart home device status
/// </summary>
[GenerateSerializer]
public class SmartHomeDeviceStatus
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
    /// Whether device is online
    /// </summary>
    [Id(3)] public bool IsOnline { get; set; }

    /// <summary>
    /// Device properties
    /// </summary>
    [Id(4)] public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Id(5)] public DateTime LastUpdated { get; set; }
}
