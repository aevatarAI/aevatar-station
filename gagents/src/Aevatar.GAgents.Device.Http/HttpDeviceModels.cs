using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Device.Http;

/// <summary>
/// Base API response
/// </summary>
[GenerateSerializer]
public class ApiResponse
{
    [Id(0)][JsonPropertyName("success")] public bool Success { get; set; }
    [Id(1)][JsonPropertyName("message")] public string? Message { get; set; }
    [Id(2)][JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Device information from API
/// </summary>
[GenerateSerializer]
public class DeviceInfo
{
    [Id(0)][JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [Id(1)][JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [Id(2)][JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [Id(3)][JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = string.Empty;
    [Id(4)][JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [Id(5)][JsonPropertyName("description")] public string? Description { get; set; }
    [Id(6)][JsonPropertyName("isOnline")] public bool IsOnline { get; set; }
    [Id(7)][JsonPropertyName("status")] public Dictionary<string, object>? Status { get; set; }
    [Id(8)][JsonPropertyName("supportedCommands")] public List<string> SupportedCommands { get; set; } = new();
    [Id(9)][JsonPropertyName("lastUpdated")] public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Device list response
/// </summary>
[GenerateSerializer]
public class DeviceListResponse : ApiResponse
{
    [Id(0)][JsonPropertyName("data")] public List<DeviceInfo> Data { get; set; } = new();
}

/// <summary>
/// Device response
/// </summary>
[GenerateSerializer]
public class DeviceResponse : ApiResponse
{
    [Id(0)][JsonPropertyName("data")] public DeviceInfo? Data { get; set; }
}

/// <summary>
/// Status response
/// </summary>
[GenerateSerializer]
public class StatusResponse : ApiResponse
{
    [Id(0)][JsonPropertyName("data")] public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Device overview statistics
/// </summary>
[GenerateSerializer]
public class DeviceOverview
{
    [Id(0)][JsonPropertyName("totalDevices")] public int TotalDevices { get; set; }
    [Id(1)][JsonPropertyName("onlineDevices")] public int OnlineDevices { get; set; }
    [Id(2)][JsonPropertyName("offlineDevices")] public int OfflineDevices { get; set; }
    [Id(3)][JsonPropertyName("devicesByType")] public Dictionary<string, int> DevicesByType { get; set; } = new();
    [Id(4)][JsonPropertyName("lastUpdated")] public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Overview response
/// </summary>
[GenerateSerializer]
public class OverviewResponse : ApiResponse
{
    [Id(0)][JsonPropertyName("data")] public DeviceOverview? Data { get; set; }
}

/// <summary>
/// Device command request
/// </summary>
[GenerateSerializer]
public class DeviceCommandRequest
{
    [Id(0)][JsonPropertyName("command")] public Dictionary<string, object> Command { get; set; } = new();
}

/// <summary>
/// Error response
/// </summary>
[GenerateSerializer]
public class ErrorResponse : ApiResponse
{
    [Id(0)][JsonPropertyName("error")] public string? Error { get; set; }
    [Id(1)][JsonPropertyName("details")] public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Smart Light specific status
/// </summary>
[GenerateSerializer]
public class SmartLightStatus
{
    [Id(0)][JsonPropertyName("power")] public bool Power { get; set; }
    [Id(1)][JsonPropertyName("brightness")] public int Brightness { get; set; }
    [Id(2)][JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    [Id(3)][JsonPropertyName("color_temperature")] public int ColorTemperature { get; set; }
    [Id(4)][JsonPropertyName("energy_consumption")] public double EnergyConsumption { get; set; }
    [Id(5)][JsonPropertyName("operating_hours")] public double OperatingHours { get; set; }
}

/// <summary>
/// Temperature Sensor specific status
/// </summary>
[GenerateSerializer]
public class TemperatureSensorStatus
{
    [Id(0)][JsonPropertyName("temperature")] public double Temperature { get; set; }
    [Id(1)][JsonPropertyName("humidity")] public double Humidity { get; set; }
    [Id(2)][JsonPropertyName("min_temperature")] public double MinTemperature { get; set; }
    [Id(3)][JsonPropertyName("max_temperature")] public double MaxTemperature { get; set; }
    [Id(4)][JsonPropertyName("battery_level")] public int BatteryLevel { get; set; }
    [Id(5)][JsonPropertyName("sensor_status")] public string SensorStatus { get; set; } = "normal";
}

/// <summary>
/// Smart Switch specific status
/// </summary>
[GenerateSerializer]
public class SmartSwitchStatus
{
    [Id(0)][JsonPropertyName("power")] public bool Power { get; set; }
    [Id(1)][JsonPropertyName("current_load")] public double CurrentLoad { get; set; }
    [Id(2)][JsonPropertyName("voltage")] public double Voltage { get; set; }
    [Id(3)][JsonPropertyName("power_consumption")] public double PowerConsumption { get; set; }
    [Id(4)][JsonPropertyName("daily_usage")] public double DailyUsage { get; set; }
    [Id(5)][JsonPropertyName("temperature")] public double Temperature { get; set; }
}
