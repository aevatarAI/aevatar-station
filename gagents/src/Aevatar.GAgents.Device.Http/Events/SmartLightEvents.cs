using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.Http.Models;
using Orleans;

namespace Aevatar.GAgents.Device.Http.Events;

/// <summary>
/// Turn on smart light event
/// </summary>
[GenerateSerializer]
[Description("Turn on the smart light")]
public class TurnOnLightEvent : DeviceEventBase
{
}

/// <summary>
/// Turn off smart light event
/// </summary>
[GenerateSerializer]
[Description("Turn off the smart light")]
public class TurnOffLightEvent : DeviceEventBase
{
}

/// <summary>
/// Set light brightness event
/// </summary>
[GenerateSerializer]
[Description("Set smart light brightness level")]
public class SetLightBrightnessEvent : DeviceEventBase
{
    /// <summary>
    /// Brightness percentage (0-100)
    /// </summary>
    [Id(3)]
    [Description("Brightness percentage from 0 to 100 (0=off, 100=maximum brightness)")]
    public int Brightness { get; set; }
}

/// <summary>
/// Set light color event
/// </summary>
[GenerateSerializer]
[Description("Set smart light color")]
public class SetLightColorEvent : DeviceEventBase
{
    /// <summary>
    /// Color in RGB hex format
    /// </summary>
    [Id(3)]
    [Description("Color in RGB hex format (e.g., #FF0000 for red, #00FF00 for green, #0000FF for blue)")]
    public string Color { get; set; } = "#FFFFFF";
}

/// <summary>
/// Set light color temperature event
/// </summary>
[GenerateSerializer]
[Description("Set smart light color temperature")]
public class SetLightColorTemperatureEvent : DeviceEventBase
{
    /// <summary>
    /// Color temperature in Kelvin
    /// </summary>
    [Id(3)]
    [Description("Color temperature in Kelvin (1000-10000K, where 2700K=warm white, 4000K=neutral white, 6500K=cool white)")]
    public int Temperature { get; set; }
}

/// <summary>
/// Toggle light on/off event
/// </summary>
[GenerateSerializer]
[Description("Toggle smart light on/off state")]
public class ToggleLightEvent : DeviceEventBase
{
}

/// <summary>
/// Get light status event
/// </summary>
[GenerateSerializer]
[Description("Get current smart light status including power, brightness, color, and energy consumption")]
public class GetLightStatusEvent : DeviceEventBase
{
}

/// <summary>
/// Light operation result event
/// </summary>
[GenerateSerializer]
[Description("Result of smart light operation")]
public class LightOperationResultEvent : DeviceEventBase
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
/// Light status response event
/// </summary>
[GenerateSerializer]
[Description("Smart light status response")]
public class LightStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Light status information
    /// </summary>
    [Id(3)] public SmartLightStatus LightStatus { get; set; } = new();
}
