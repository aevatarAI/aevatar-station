using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Connections;
using Aevatar.GAgents.Device.GAgents;
using Aevatar.GAgents.Device.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Device.Examples;

/// <summary>
/// Temperature sensor GAgent interface
/// </summary>
public interface ITemperatureSensorGAgent : IDeviceGAgent<VirtualDeviceConnection>
{
    /// <summary>
    /// Get current temperature
    /// </summary>
    /// <returns>Temperature value (Celsius)</returns>
    Task<double> GetTemperatureAsync();
    
    /// <summary>
    /// Get current humidity
    /// </summary>
    /// <returns>Humidity percentage</returns>
    Task<double> GetHumidityAsync();
    
    /// <summary>
    /// Get temperature and humidity reading
    /// </summary>
    /// <returns>Temperature and humidity data</returns>
    Task<TemperatureHumidityReading> GetReadingAsync();
    
    /// <summary>
    /// Calibrate sensor
    /// </summary>
    /// <returns>Whether calibration was successful</returns>
    Task<bool> CalibrateAsync();
    
    /// <summary>
    /// Get historical readings
    /// </summary>
    /// <param name="hours">Data from past how many hours</param>
    /// <returns>List of historical readings</returns>
    Task<List<TemperatureHumidityReading>> GetHistoryAsync(int hours = 24);
}

/// <summary>
/// Temperature and humidity reading
/// </summary>
[GenerateSerializer]
public class TemperatureHumidityReading
{
    /// <summary>
    /// Temperature (Celsius)
    /// </summary>
    [Id(0)] public double Temperature { get; set; }
    
    /// <summary>
    /// Humidity percentage
    /// </summary>
    [Id(1)] public double Humidity { get; set; }
    
    /// <summary>
    /// Reading time
    /// </summary>
    [Id(2)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Sensor status
    /// </summary>
    [Id(3)] public string Status { get; set; } = "Normal";
    
    /// <summary>
    /// Temperature level description
    /// </summary>
    public string TemperatureLevel
    {
        get
        {
            return Temperature switch
            {
                < 0 => "Freezing",
                < 10 => "Cold",
                < 20 => "Cool",
                < 25 => "Comfortable",
                < 30 => "Warm",
                < 35 => "Hot",
                _ => "Very Hot"
            };
        }
    }
    
    /// <summary>
    /// Humidity level description
    /// </summary>
    public string HumidityLevel
    {
        get
        {
            return Humidity switch
            {
                < 30 => "Dry",
                < 40 => "Slightly Dry",
                < 60 => "Moderate",
                < 70 => "Slightly Humid",
                _ => "Humid"
            };
        }
    }
    
    /// <summary>
    /// Comfort level rating
    /// </summary>
    public string ComfortLevel
    {
        get
        {
            if (Temperature >= 20 && Temperature <= 26 && Humidity >= 40 && Humidity <= 60)
                return "Comfortable";
            else if (Temperature >= 18 && Temperature <= 28 && Humidity >= 30 && Humidity <= 70)
                return "Fairly Comfortable";
            else
                return "Uncomfortable";
        }
    }
}

/// <summary>
/// Temperature sensor GAgent implementation
/// </summary>
[Description("Temperature sensor device agent providing temperature and humidity monitoring, supports historical data query and sensor calibration, can perform environmental monitoring through AI assistant")]
[GAgent("temperature-sensor", "device")]
public class TemperatureSensorGAgent : DeviceGAgentBase<VirtualDeviceConnection>, ITemperatureSensorGAgent
{
    private readonly List<TemperatureHumidityReading> _readings = new();
    private readonly object _readingsLock = new();

    public override async Task<string> GetDescriptionAsync()
    {
        var deviceName = DeviceConnection?.DeviceName ?? "Temperature Sensor";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        
        var description = $"Temperature sensor device agent ({deviceName}) - Current status: {GetStatusDescription(status)}.";
        
        if (DeviceConnection?.Status == DeviceConnectionStatus.Connected)
        {
            try
            {
                var reading = await GetReadingAsync();
                description += $" Current environment: Temperature {reading.Temperature:F1}°C ({reading.TemperatureLevel}), " +
                              $"Humidity {reading.Humidity:F1}% ({reading.HumidityLevel}), " +
                              $"Comfort: {reading.ComfortLevel}";
            }
            catch
            {
                description += " Unable to get current readings";
            }
        }
        
        return description;
    }

    protected override async Task<VirtualDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
        
        var connection = new VirtualDeviceConnection(
            config.DeviceId,
            config.DeviceName,
            "TemperatureSensor", // Fixed as temperature sensor type
            logger
        );
        
        // Subscribe to property change events to record historical data
        connection.PropertyChanged += OnSensorPropertyChanged;
        
        return connection;
    }

    public async Task<double> GetTemperatureAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read temperature");
                return double.NaN;
            }

            var temperature = await DeviceConnection.ReadPropertyAsync("Temperature");
            
            if (temperature is double temp)
            {
                Logger.LogDebug("Read temperature: {Temperature:F1}°C", temp);
                return temp;
            }
            
            Logger.LogWarning("Temperature data type error: {Type}", temperature?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read temperature");
            return double.NaN;
        }
    }

    public async Task<double> GetHumidityAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read humidity");
                return double.NaN;
            }

            var humidity = await DeviceConnection.ReadPropertyAsync("Humidity");
            
            if (humidity is double hum)
            {
                Logger.LogDebug("Read humidity: {Humidity:F1}%", hum);
                return hum;
            }
            
            Logger.LogWarning("Humidity data type error: {Type}", humidity?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read humidity");
            return double.NaN;
        }
    }

    public async Task<TemperatureHumidityReading> GetReadingAsync()
    {
        try
        {
            var temperature = await GetTemperatureAsync();
            var humidity = await GetHumidityAsync();
            
            var reading = new TemperatureHumidityReading
            {
                Temperature = temperature,
                Humidity = humidity,
                Timestamp = DateTime.UtcNow,
                Status = (double.IsNaN(temperature) || double.IsNaN(humidity)) ? "Error" : "Normal"
            };
            
            Logger.LogInformation("Get sensor reading: Temperature={Temperature:F1}°C ({TempLevel}), Humidity={Humidity:F1}% ({HumLevel}), Comfort={Comfort}",
                reading.Temperature, reading.TemperatureLevel, reading.Humidity, reading.HumidityLevel, reading.ComfortLevel);
            
            return reading;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get sensor reading");
            
            return new TemperatureHumidityReading
            {
                Temperature = double.NaN,
                Humidity = double.NaN,
                Timestamp = DateTime.UtcNow,
                Status = "Error"
            };
        }
    }

    public async Task<bool> CalibrateAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot calibrate");
                return false;
            }

            Logger.LogInformation("Starting temperature sensor calibration...");
            
            var result = await DeviceConnection.ExecuteActionAsync("Calibrate");
            
            Logger.LogInformation("Sensor calibration operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to calibrate sensor");
            return false;
        }
    }

    public Task<List<TemperatureHumidityReading>> GetHistoryAsync(int hours = 24)
    {
        lock (_readingsLock)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);
            var history = _readings
                .Where(r => r.Timestamp >= cutoffTime)
                .OrderByDescending(r => r.Timestamp)
                .Take(1000) // Return at most 1000 records
                .ToList();
            
            Logger.LogDebug("Get historical readings: {Count} records, time range: {Hours} hours", history.Count, hours);
            
            return Task.FromResult(history);
        }
    }

    private void OnSensorPropertyChanged(object? sender, DevicePropertyChangedEventArgs e)
    {
        // When temperature or humidity changes, record to historical data
        if (e.PropertyName == "Temperature" || e.PropertyName == "Humidity")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var reading = await GetReadingAsync();
                    
                    lock (_readingsLock)
                    {
                        _readings.Add(reading);
                        
                        // Keep historical records under 10000
                        if (_readings.Count > 10000)
                        {
                            _readings.RemoveRange(0, _readings.Count - 10000);
                        }
                    }
                    
                    // Check if environment alerts are needed
                    await CheckEnvironmentAlertsAsync(reading);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred while recording sensor reading");
                }
            });
        }
    }

    private async Task CheckEnvironmentAlertsAsync(TemperatureHumidityReading reading)
    {
        try
        {
            var alerts = new List<string>();
            
            // Temperature warnings
            if (reading.Temperature < 5)
                alerts.Add($"Temperature too low: {reading.Temperature:F1}°C, may freeze");
            else if (reading.Temperature > 35)
                alerts.Add($"Temperature too high: {reading.Temperature:F1}°C, pay attention to heat protection");
            
            // Humidity warnings
            if (reading.Humidity < 20)
                alerts.Add($"Humidity too low: {reading.Humidity:F1}%, air is dry");
            else if (reading.Humidity > 80)
                alerts.Add($"Humidity too high: {reading.Humidity:F1}%, may cause mold");
            
            // Comfort warnings
            if (reading.ComfortLevel == "Uncomfortable")
                alerts.Add("Environmental comfort is poor, recommend adjusting temperature and humidity");
            
            // Publish warning events
            foreach (var alert in alerts)
            {
                await PublishAsync(new EnvironmentAlertEvent
                {
                    DeviceId = DeviceConnection?.DeviceId ?? State.DeviceId,
                    DeviceName = DeviceConnection?.DeviceName ?? State.DeviceName,
                    AlertLevel = "Warning",
                    Message = alert,
                    Temperature = reading.Temperature,
                    Humidity = reading.Humidity,
                    Timestamp = DateTime.UtcNow
                });
                
                Logger.LogWarning("Environment warning: {Alert}", alert);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while checking environment alerts");
        }
    }

    private static string GetStatusDescription(DeviceConnectionStatus status)
    {
        return status switch
        {
            DeviceConnectionStatus.Connected => "Connected",
            DeviceConnectionStatus.Connecting => "Connecting",
            DeviceConnectionStatus.Disconnected => "Disconnected",
            DeviceConnectionStatus.Error => "Connection Error",
            DeviceConnectionStatus.Reconnecting => "Reconnecting",
            _ => "Unknown Status"
        };
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        Logger.LogInformation("Temperature sensor GAgent activated: {GrainId}", this.GetGrainId());
        
        // If there's connection config and device is not connected, try auto-connect
        if (State.ConnectionConfig != null && !State.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000, cancellationToken); // Delay 1 second before connecting
                try
                {
                    await InitializeDeviceConnectionAsync(State.ConnectionConfig);
                    
                    // Enable device monitoring
                    await SetDeviceMonitoringAsync(true);
                    
                    // Record initial reading
                    var initialReading = await GetReadingAsync();
                    lock (_readingsLock)
                    {
                        _readings.Add(initialReading);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to auto-connect temperature sensor");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Temperature sensor GAgent is deactivating: {GrainId}, reason: {Reason}", this.GetGrainId(), reason);
        
        // Unsubscribe from property change events
        if (DeviceConnection != null)
        {
            DeviceConnection.PropertyChanged -= OnSensorPropertyChanged;
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}

/// <summary>
/// Environment alert event
/// </summary>
[GenerateSerializer]
[Description("Environmental monitoring alert event")]
public class EnvironmentAlertEvent : EventBase
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
    /// Alert level
    /// </summary>
    [Id(2)] public string AlertLevel { get; set; } = "Info";
    
    /// <summary>
    /// Alert message
    /// </summary>
    [Id(3)] public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Current temperature
    /// </summary>
    [Id(4)] public double Temperature { get; set; }
    
    /// <summary>
    /// Current humidity
    /// </summary>
    [Id(5)] public double Humidity { get; set; }
    
    /// <summary>
    /// Event time
    /// </summary>
    [Id(6)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
