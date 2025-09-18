using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.Http.Models;
using Orleans;

namespace Aevatar.GAgents.Device.Http.Events;

/// <summary>
/// Get temperature reading event
/// </summary>
[GenerateSerializer]
[Description("Get current temperature reading from sensor")]
public class GetTemperatureEvent : DeviceEventBase
{
}

/// <summary>
/// Get humidity reading event
/// </summary>
[GenerateSerializer]
[Description("Get current humidity reading from sensor")]
public class GetHumidityEvent : DeviceEventBase
{
}

/// <summary>
/// Get battery level event
/// </summary>
[GenerateSerializer]
[Description("Get sensor battery level")]
public class GetBatteryLevelEvent : DeviceEventBase
{
}

/// <summary>
/// Get complete sensor status event
/// </summary>
[GenerateSerializer]
[Description("Get complete temperature sensor status including temperature, humidity, battery, and range")]
public class GetSensorStatusEvent : DeviceEventBase
{
}

/// <summary>
/// Get temperature range event
/// </summary>
[GenerateSerializer]
[Description("Get minimum and maximum temperature readings recorded by sensor")]
public class GetTemperatureRangeEvent : DeviceEventBase
{
}

/// <summary>
/// Calibrate temperature sensor event
/// </summary>
[GenerateSerializer]
[Description("Calibrate the temperature sensor for accurate readings")]
public class CalibrateSensorEvent : DeviceEventBase
{
}

/// <summary>
/// Temperature reading response event
/// </summary>
[GenerateSerializer]
[Description("Temperature reading response")]
public class TemperatureResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Temperature value in Celsius
    /// </summary>
    [Id(3)] public double Temperature { get; set; }
}

/// <summary>
/// Humidity reading response event
/// </summary>
[GenerateSerializer]
[Description("Humidity reading response")]
public class HumidityResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Humidity percentage
    /// </summary>
    [Id(3)] public double Humidity { get; set; }
}

/// <summary>
/// Battery level response event
/// </summary>
[GenerateSerializer]
[Description("Battery level response")]
public class BatteryLevelResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Battery level percentage
    /// </summary>
    [Id(3)] public int BatteryLevel { get; set; }
}

/// <summary>
/// Sensor status response event
/// </summary>
[GenerateSerializer]
[Description("Complete temperature sensor status response")]
public class SensorStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Sensor status information
    /// </summary>
    [Id(3)] public TemperatureSensorStatus SensorStatus { get; set; } = new();
}

/// <summary>
/// Temperature range response event
/// </summary>
[GenerateSerializer]
[Description("Temperature range response")]
public class TemperatureRangeResponseEvent : DeviceEventBase
{
    /// <summary>
    /// Minimum temperature
    /// </summary>
    [Id(3)] public double MinTemperature { get; set; }
    
    /// <summary>
    /// Maximum temperature
    /// </summary>
    [Id(4)] public double MaxTemperature { get; set; }
}

/// <summary>
/// Sensor calibration result event
/// </summary>
[GenerateSerializer]
[Description("Temperature sensor calibration result")]
public class SensorCalibrationResultEvent : DeviceEventBase
{
    /// <summary>
    /// Whether calibration was successful
    /// </summary>
    [Id(3)] public bool Success { get; set; }
    
    /// <summary>
    /// Calibration result message
    /// </summary>
    [Id(4)] public string Message { get; set; } = string.Empty;
}
