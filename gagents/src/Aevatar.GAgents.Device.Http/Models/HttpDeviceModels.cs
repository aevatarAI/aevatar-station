using Orleans;

namespace Aevatar.GAgents.Device.Http.Models;

/// <summary>
/// Smart Light specific status
/// </summary>
[GenerateSerializer]
public class SmartLightStatus
{
    [Id(0)] public bool Power { get; set; }
    [Id(1)] public int Brightness { get; set; }
    [Id(2)] public string Color { get; set; } = "#FFFFFF";
    [Id(3)] public int ColorTemperature { get; set; }
    [Id(4)] public double EnergyConsumption { get; set; }
    [Id(5)] public double OperatingHours { get; set; }
}

/// <summary>
/// Temperature Sensor specific status
/// </summary>
[GenerateSerializer]
public class TemperatureSensorStatus
{
    [Id(0)] public double Temperature { get; set; }
    [Id(1)] public double Humidity { get; set; }
    [Id(2)] public double MinTemperature { get; set; }
    [Id(3)] public double MaxTemperature { get; set; }
    [Id(4)] public int BatteryLevel { get; set; }
    [Id(5)] public string SensorStatus { get; set; } = "normal";
}

/// <summary>
/// Smart Switch specific status
/// </summary>
[GenerateSerializer]
public class SmartSwitchStatus
{
    [Id(0)] public bool Power { get; set; }
    [Id(1)] public double CurrentLoad { get; set; }
    [Id(2)] public double Voltage { get; set; }
    [Id(3)] public double PowerConsumption { get; set; }
    [Id(4)] public double DailyUsage { get; set; }
    [Id(5)] public double Temperature { get; set; }
}
