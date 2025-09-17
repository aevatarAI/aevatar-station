using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.GAgents;
using Aevatar.GAgents.Device.Http.Events;
using Aevatar.GAgents.Device.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.GAgents.Device.Http.GAgents;

/// <summary>
/// Virtual Temperature Sensor GAgent interface - connects to real API
/// </summary>
public interface IHttpTemperatureSensorGAgent : IDeviceGAgent<HttpDeviceConnection>
{
    /// <summary>
    /// Get current temperature from API
    /// </summary>
    /// <returns>Temperature value (Celsius)</returns>
    Task<double> GetTemperatureAsync();
    
    /// <summary>
    /// Get current humidity from API
    /// </summary>
    /// <returns>Humidity percentage</returns>
    Task<double> GetHumidityAsync();
    
    /// <summary>
    /// Get battery level from API
    /// </summary>
    /// <returns>Battery level percentage</returns>
    Task<int> GetBatteryLevelAsync();
    
    /// <summary>
    /// Get complete sensor reading from API
    /// </summary>
    /// <returns>Temperature sensor status information</returns>
    Task<TemperatureSensorStatus> GetSensorStatusAsync();
    
    /// <summary>
    /// Calibrate sensor via API
    /// </summary>
    /// <returns>Whether calibration was successful</returns>
    Task<bool> CalibrateAsync();
    
    /// <summary>
    /// Get temperature range (min/max) from API
    /// </summary>
    /// <returns>Temperature range information</returns>
    Task<(double min, double max)> GetTemperatureRangeAsync();
}

/// <summary>
/// Virtual Temperature Sensor GAgent - connects to Virtual Device Hub API
/// </summary>
[Description("Virtual temperature sensor device agent connecting to Virtual Device Hub API, providing temperature, humidity, and battery monitoring through real API calls")]
[GAgent("http-temperature-sensor", "device")]
public class HttpTemperatureSensorGAgent : DeviceGAgentBase<HttpDeviceConnection>, IHttpTemperatureSensorGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        var deviceId = DeviceConnection?.DeviceId ?? State.DeviceId ?? "unknown";
        var deviceName = DeviceConnection?.DeviceName ?? State.DeviceName ?? "HTTP Temperature Sensor";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        
        var description = $"HTTP Temperature Sensor Monitor - Device ID: {deviceId}, Name: {deviceName}\n";
        description += $"Connection Status: {GetStatusDescription(status)}\n\n";
        
        description += "MONITOR THIS SENSOR BY SENDING EVENTS:\n\n";
        
        description += "1. READ INDIVIDUAL VALUES:\n";
        description += "   • GetTemperatureEvent: Get current temperature reading\n";
        description += "   • GetHumidityEvent: Get current humidity reading\n";
        description += "   • GetBatteryLevelEvent: Get battery level percentage\n\n";
        
        description += "2. GET TEMPERATURE RANGE:\n";
        description += "   • GetTemperatureRangeEvent: Get min/max temperature readings\n\n";
        
        description += "3. CALIBRATE SENSOR:\n";
        description += "   • CalibrateSensorEvent: Recalibrate sensor for accurate readings\n\n";
        
        description += "4. GET COMPLETE STATUS:\n";
        description += "   • GetSensorStatusEvent: Get all sensor readings and status\n";
        description += "   • GetDeviceStatusEvent: Get general device status\n\n";
        
        if (DeviceConnection?.Status == DeviceConnectionStatus.Connected)
        {
            try
            {
                var sensorStatus = await GetSensorStatusAsync();
                description += "CURRENT READINGS:\n";
                description += $"• Temperature: {sensorStatus.Temperature:F1}°C\n";
                description += $"• Humidity: {sensorStatus.Humidity:F1}%\n";
                description += $"• Battery Level: {sensorStatus.BatteryLevel}%\n";
                description += $"• Temperature Range: {sensorStatus.MinTemperature:F1}°C - {sensorStatus.MaxTemperature:F1}°C\n";
                description += $"• Sensor Status: {sensorStatus.SensorStatus}\n";
                
                // Add comfort level analysis
                var comfort = GetComfortLevel(sensorStatus.Temperature, sensorStatus.Humidity);
                description += $"• Environmental Comfort: {comfort}\n";
            }
            catch
            {
                description += "CURRENT READINGS: Unable to retrieve readings from API\n";
            }
        }
        else
        {
            description += "CURRENT READINGS: Device not connected - send ConnectDeviceEvent first\n";
        }
        
        description += "\nThis sensor connects to Virtual Device Hub API. Monitor it by sending the appropriate events listed above.";
        description += "\nNote: This is a read-only sensor device - it provides monitoring data but cannot be controlled.";
        return description;
    }

    protected override async Task<HttpDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config)
    {
        var factory = ServiceProvider.GetRequiredService<IVirtualDeviceApiConnectionFactory>();
        var options = ServiceProvider.GetService<IOptions<HttpDeviceHubOptions>>()?.Value;
        
        // Override options with config if provided
        if (config.ExtendedProperties.TryGetValue("BaseUrl", out var baseUrl))
        {
            options = options ?? new HttpDeviceHubOptions();
            options.BaseUrl = baseUrl;
        }
        
        if (config.ExtendedProperties.TryGetValue("ApiKey", out var apiKey))
        {
            options = options ?? new HttpDeviceHubOptions();
            options.ApiKey = apiKey;
        }

        return factory.CreateConnection(config.DeviceId, "temperature-sensor", options);
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
                Logger.LogDebug("Read temperature from API: {Temperature:F1}°C", temp);
                return temp;
            }
            
            Logger.LogWarning("Temperature data type error from API: {Type}", temperature?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read temperature from API");
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
                Logger.LogDebug("Read humidity from API: {Humidity:F1}%", hum);
                return hum;
            }
            
            Logger.LogWarning("Humidity data type error from API: {Type}", humidity?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read humidity from API");
            return double.NaN;
        }
    }

    public async Task<int> GetBatteryLevelAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read battery level");
                return 0;
            }

            var battery = await DeviceConnection.ReadPropertyAsync("BatteryLevel");
            
            if (battery is int level)
            {
                Logger.LogDebug("Read battery level from API: {BatteryLevel}%", level);
                return level;
            }
            
            Logger.LogWarning("Battery level data type error from API: {Type}", battery?.GetType().Name ?? "null");
            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read battery level from API");
            return 0;
        }
    }

    public async Task<TemperatureSensorStatus> GetSensorStatusAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                return new TemperatureSensorStatus
                {
                    Temperature = double.NaN,
                    Humidity = double.NaN,
                    MinTemperature = 0,
                    MaxTemperature = 0,
                    BatteryLevel = 0,
                    SensorStatus = "disconnected"
                };
            }

            var temperature = await GetTemperatureAsync();
            var humidity = await GetHumidityAsync();
            var batteryLevel = await GetBatteryLevelAsync();
            var (minTemp, maxTemp) = await GetTemperatureRangeAsync();

            var status = new TemperatureSensorStatus
            {
                Temperature = temperature,
                Humidity = humidity,
                MinTemperature = minTemp,
                MaxTemperature = maxTemp,
                BatteryLevel = batteryLevel,
                SensorStatus = (double.IsNaN(temperature) || double.IsNaN(humidity)) ? "error" : "normal"
            };

            Logger.LogInformation("Get virtual sensor status from API: Temperature={Temperature:F1}°C, Humidity={Humidity:F1}%, Battery={Battery}%, Status={Status}",
                status.Temperature, status.Humidity, status.BatteryLevel, status.SensorStatus);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get sensor status from API");
            
            return new TemperatureSensorStatus
            {
                Temperature = double.NaN,
                Humidity = double.NaN,
                MinTemperature = 0,
                MaxTemperature = 0,
                BatteryLevel = 0,
                SensorStatus = "error"
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

            Logger.LogInformation("Starting virtual temperature sensor calibration via API...");
            
            var result = await DeviceConnection.ExecuteActionAsync("calibrate");
            
            Logger.LogInformation("Virtual sensor calibration operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to calibrate sensor via API");
            return false;
        }
    }

    public async Task<(double min, double max)> GetTemperatureRangeAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read temperature range");
                return (0, 0);
            }

            var minTemp = await DeviceConnection.ReadPropertyAsync("MinTemperature");
            var maxTemp = await DeviceConnection.ReadPropertyAsync("MaxTemperature");
            
            var min = minTemp is double minValue ? minValue : 0.0;
            var max = maxTemp is double maxValue ? maxValue : 0.0;
            
            Logger.LogDebug("Read temperature range from API: {Min:F1}°C - {Max:F1}°C", min, max);
            return (min, max);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read temperature range from API");
            return (0, 0);
        }
    }

    private static string GetStatusDescription(DeviceConnectionStatus status)
    {
        return status switch
        {
            DeviceConnectionStatus.Connected => "Connected to API",
            DeviceConnectionStatus.Connecting => "Connecting to API",
            DeviceConnectionStatus.Disconnected => "Disconnected from API",
            DeviceConnectionStatus.Error => "API Connection Error",
            DeviceConnectionStatus.Reconnecting => "Reconnecting to API",
            _ => "Unknown Status"
        };
    }

    private static string GetComfortLevel(double temperature, double humidity)
    {
        if (temperature >= 20 && temperature <= 26 && humidity >= 40 && humidity <= 60)
            return "Comfortable";
        else if (temperature >= 18 && temperature <= 28 && humidity >= 30 && humidity <= 70)
            return "Fairly Comfortable";
        else
            return "Uncomfortable";
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        
        Logger.LogInformation("Virtual temperature sensor GAgent activated: {GrainId}", this.GetGrainId());
        
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
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to auto-connect virtual temperature sensor to API");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Virtual temperature sensor GAgent is deactivating: {GrainId}, reason: {Reason}", this.GetGrainId(), reason);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    #region Temperature Sensor Specific Event Handlers

    /// <summary>
    /// Handle get temperature event
    /// </summary>
    [EventHandler]
    public async Task HandleGetTemperatureAsync(GetTemperatureEvent @event)
    {
        Logger.LogInformation("Handling get temperature event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var temperature = await GetTemperatureAsync();
            
            await PublishAsync(new TemperatureResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Temperature = temperature,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get temperature: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_TEMPERATURE_FAILED",
                ErrorMessage = $"Failed to get temperature: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get humidity event
    /// </summary>
    [EventHandler]
    public async Task HandleGetHumidityAsync(GetHumidityEvent @event)
    {
        Logger.LogInformation("Handling get humidity event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var humidity = await GetHumidityAsync();
            
            await PublishAsync(new HumidityResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Humidity = humidity,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get humidity: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_HUMIDITY_FAILED",
                ErrorMessage = $"Failed to get humidity: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get battery level event
    /// </summary>
    [EventHandler]
    public async Task HandleGetBatteryLevelAsync(GetBatteryLevelEvent @event)
    {
        Logger.LogInformation("Handling get battery level event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var batteryLevel = await GetBatteryLevelAsync();
            
            await PublishAsync(new BatteryLevelResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                BatteryLevel = batteryLevel,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get battery level: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_BATTERY_FAILED",
                ErrorMessage = $"Failed to get battery level: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get sensor status event
    /// </summary>
    [EventHandler]
    public async Task HandleGetSensorStatusAsync(GetSensorStatusEvent @event)
    {
        Logger.LogInformation("Handling get sensor status event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var status = await GetSensorStatusAsync();
            
            await PublishAsync(new SensorStatusResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                SensorStatus = status,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get sensor status: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_SENSOR_STATUS_FAILED",
                ErrorMessage = $"Failed to get sensor status: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get temperature range event
    /// </summary>
    [EventHandler]
    public async Task HandleGetTemperatureRangeAsync(GetTemperatureRangeEvent @event)
    {
        Logger.LogInformation("Handling get temperature range event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var (min, max) = await GetTemperatureRangeAsync();
            
            await PublishAsync(new TemperatureRangeResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                MinTemperature = min,
                MaxTemperature = max,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get temperature range: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_TEMPERATURE_RANGE_FAILED",
                ErrorMessage = $"Failed to get temperature range: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle calibrate sensor event
    /// </summary>
    [EventHandler]
    public async Task HandleCalibrateSensorAsync(CalibrateSensorEvent @event)
    {
        Logger.LogInformation("Handling calibrate sensor event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var success = await CalibrateAsync();
            
            await PublishAsync(new SensorCalibrationResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Success = success,
                Message = success ? "Sensor calibration completed successfully" : "Sensor calibration failed",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to calibrate sensor: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new SensorCalibrationResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Success = false,
                Message = $"Calibration failed: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion
}
