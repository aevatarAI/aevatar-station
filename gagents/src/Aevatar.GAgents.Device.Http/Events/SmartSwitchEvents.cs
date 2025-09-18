using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.Http.Models;
using Orleans;

namespace Aevatar.GAgents.Device.Http.Events;

/// <summary>
/// Turn on smart switch event
/// </summary>
[GenerateSerializer]
[Description("Turn on the smart switch to enable power flow")]
public class TurnOnSwitchEvent : DeviceEventBase
{
}

/// <summary>
/// Turn off smart switch event
/// </summary>
[GenerateSerializer]
[Description("Turn off the smart switch to disable power flow")]
public class TurnOffSwitchEvent : DeviceEventBase
{
}

/// <summary>
/// Toggle smart switch event
/// </summary>
[GenerateSerializer]
[Description("Toggle smart switch on/off state")]
public class ToggleSwitchEvent : DeviceEventBase
{
}

/// <summary>
/// Get current load event
/// </summary>
[GenerateSerializer]
[Description("Get current electrical load from smart switch")]
public class GetCurrentLoadEvent : DeviceEventBase
{
}

/// <summary>
/// Get voltage event
/// </summary>
[GenerateSerializer]
[Description("Get voltage reading from smart switch")]
public class GetVoltageEvent : DeviceEventBase
{
}

/// <summary>
/// Get power consumption event
/// </summary>
[GenerateSerializer]
[Description("Get real-time power consumption from smart switch")]
public class GetPowerConsumptionEvent : DeviceEventBase
{
}

/// <summary>
/// Get daily usage event
/// </summary>
[GenerateSerializer]
[Description("Get daily energy usage from smart switch")]
public class GetDailyUsageEvent : DeviceEventBase
{
}

/// <summary>
/// Get switch temperature event
/// </summary>
[GenerateSerializer]
[Description("Get internal temperature of smart switch")]
public class GetSwitchTemperatureEvent : DeviceEventBase
{
}

/// <summary>
/// Get switch status event
/// </summary>
[GenerateSerializer]
[Description("Get complete smart switch status including all electrical measurements")]
public class GetSwitchStatusEvent : DeviceEventBase
{
}

/// <summary>
/// Reset daily usage event
/// </summary>
[GenerateSerializer]
[Description("Reset daily energy usage counter to zero")]
public class ResetDailyUsageEvent : DeviceEventBase
{
}

/// <summary>
/// Switch operation result event
/// </summary>
[GenerateSerializer]
[Description("Result of smart switch operation")]
public class SwitchOperationResultEvent : DeviceEventBase
{
    /// <summary>
    /// Operation performed
    /// </summary>
    [Id(3)] public string Operation { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether operation was successful
    /// </summary>
    [Id(4)] public bool Success { get; set; }
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    [Id(5)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// Current load response event
/// </summary>
[GenerateSerializer]
[Description("Current electrical load response")]
public class CurrentLoadResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Current load in amperes
    /// </summary>
    [Id(3)] public double CurrentLoad { get; set; }
}

/// <summary>
/// Voltage response event
/// </summary>
[GenerateSerializer]
[Description("Voltage reading response")]
public class VoltageResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Voltage in volts
    /// </summary>
    [Id(3)] public double Voltage { get; set; }
}

/// <summary>
/// Power consumption response event
/// </summary>
[GenerateSerializer]
[Description("Power consumption response")]
public class PowerConsumptionResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Power consumption in watts
    /// </summary>
    [Id(3)] public double PowerConsumption { get; set; }
}

/// <summary>
/// Daily usage response event
/// </summary>
[GenerateSerializer]
[Description("Daily energy usage response")]
public class DailyUsageResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Daily usage in kWh
    /// </summary>
    [Id(3)] public double DailyUsage { get; set; }
}

/// <summary>
/// Switch temperature response event
/// </summary>
[GenerateSerializer]
[Description("Switch internal temperature response")]
public class SwitchTemperatureResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Switch temperature in Celsius
    /// </summary>
    [Id(3)] public double Temperature { get; set; }
}

/// <summary>
/// Switch status response event
/// </summary>
[GenerateSerializer]
[Description("Complete smart switch status response")]
public class SwitchStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Switch status information
    /// </summary>
    [Id(3)] public SmartSwitchStatus SwitchStatus { get; set; } = new();
}
