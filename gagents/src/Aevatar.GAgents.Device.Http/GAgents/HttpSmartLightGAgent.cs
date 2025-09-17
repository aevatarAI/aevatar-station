using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.GAgents;
using Aevatar.GAgents.Device.Http.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aevatar.GAgents.Device.State;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Device.Http.GAgents;

/// <summary>
/// Virtual Smart Light GAgent interface - connects to real API
/// </summary>
public interface IHttpSmartLightGAgent : IDeviceGAgent<HttpDeviceConnection>
{
    /// <summary>
    /// Turn on light via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOnAsync();
    
    /// <summary>
    /// Turn off light via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOffAsync();
    
    /// <summary>
    /// Set brightness via API
    /// </summary>
    /// <param name="brightness">Brightness percentage (0-100)</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetBrightnessAsync(int brightness);
    
    /// <summary>
    /// Set color via API
    /// </summary>
    /// <param name="color">Color value (RGB hex, e.g. #FF0000)</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetColorAsync(string color);
    
    /// <summary>
    /// Set color temperature via API
    /// </summary>
    /// <param name="temperature">Color temperature in Kelvin</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetColorTemperatureAsync(int temperature);
    
    /// <summary>
    /// Get current light status from API
    /// </summary>
    /// <returns>Smart light status information</returns>
    Task<SmartLightStatus> GetLightStatusAsync();
    
    /// <summary>
    /// Toggle light on/off via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> ToggleAsync();
}

/// <summary>
/// Virtual Smart Light GAgent - connects to Virtual Device Hub API
/// </summary>
[Description("Virtual smart light device agent connecting to Virtual Device Hub API, providing on/off, brightness adjustment, color setting and other functions through real API calls")]
[GAgent("http-smart-light", "device")]
public class HttpSmartLightGAgent : DeviceGAgentBase<HttpDeviceConnection>, IHttpSmartLightGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        var deviceId = DeviceConnection?.DeviceId ?? State.DeviceId ?? "unknown";
        var deviceName = DeviceConnection?.DeviceName ?? State.DeviceName ?? "HTTP Smart Light";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        
        var description = $"HTTP Smart Light Controller - Device ID: {deviceId}, Name: {deviceName}\n";
        description += $"Connection Status: {GetStatusDescription(status)}\n\n";
        
        description += "CONTROL THIS DEVICE BY SENDING EVENTS:\n\n";
        
        description += "1. TURN LIGHT ON/OFF:\n";
        description += "   • TurnOnLightEvent: Turn on the light\n";
        description += "   • TurnOffLightEvent: Turn off the light\n";
        description += "   • ToggleLightEvent: Toggle light on/off state\n\n";
        
        description += "2. ADJUST BRIGHTNESS:\n";
        description += "   • SetLightBrightnessEvent: Brightness (0-100%)\n\n";
        
        description += "3. CHANGE COLOR:\n";
        description += "   • SetLightColorEvent: Color (RGB hex format like #FF0000)\n\n";
        
        description += "4. SET COLOR TEMPERATURE:\n";
        description += "   • SetLightColorTemperatureEvent: Temperature (1000-10000K)\n\n";
        
        description += "5. GET STATUS:\n";
        description += "   • GetLightStatusEvent: Get complete light status\n";
        description += "   • GetDeviceStatusEvent: Get general device status\n\n";
        
        if (DeviceConnection?.Status == DeviceConnectionStatus.Connected)
        {
            try
            {
                var lightStatus = await GetLightStatusAsync();
                description += "CURRENT STATUS:\n";
                description += $"• Power: {(lightStatus.Power ? "ON" : "OFF")}\n";
                description += $"• Brightness: {lightStatus.Brightness}%\n";
                description += $"• Color: {lightStatus.Color}\n";
                description += $"• Color Temperature: {lightStatus.ColorTemperature}K\n";
                description += $"• Energy Consumption: {lightStatus.EnergyConsumption:F1}W\n";
                description += $"• Operating Hours: {lightStatus.OperatingHours:F1}h\n";
            }
            catch
            {
                description += "CURRENT STATUS: Unable to retrieve status from API\n";
            }
        }
        else
        {
            description += "CURRENT STATUS: Device not connected - send ConnectDeviceEvent first\n";
        }
        
        description += "\nThis device connects to Virtual Device Hub API. Control it by sending the appropriate events listed above.";
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

        return factory.CreateConnection(config.DeviceId, "smart-light", options);
    }

    public async Task<bool> TurnOnAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot turn on light");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("turn_on");
            
            Logger.LogInformation("Virtual smart light turn on operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Turn on light operation failed");
            return false;
        }
    }

    public async Task<bool> TurnOffAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot turn off light");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("turn_off");
            
            Logger.LogInformation("Virtual smart light turn off operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Turn off light operation failed");
            return false;
        }
    }

    public async Task<bool> SetBrightnessAsync(int brightness)
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot set brightness");
                return false;
            }

            if (brightness < 0 || brightness > 100)
            {
                Logger.LogWarning("Brightness value out of range: {Brightness}, valid range: 0-100", brightness);
                return false;
            }

            var parameters = new Dictionary<string, object>
            {
                ["brightness"] = brightness
            };

            var result = await DeviceConnection.ExecuteActionAsync("set_brightness", parameters);
            
            Logger.LogInformation("Virtual smart light set brightness operation: {Success}, brightness: {Brightness}%, result: {Result}", 
                result.IsSuccess, brightness, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Set brightness operation failed: {Brightness}", brightness);
            return false;
        }
    }

    public async Task<bool> SetColorAsync(string color)
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot set color");
                return false;
            }

            if (string.IsNullOrWhiteSpace(color))
            {
                Logger.LogWarning("Color value cannot be empty");
                return false;
            }

            var parameters = new Dictionary<string, object>
            {
                ["color"] = color
            };

            var result = await DeviceConnection.ExecuteActionAsync("set_color", parameters);
            
            Logger.LogInformation("Virtual smart light set color operation: {Success}, color: {Color}, result: {Result}", 
                result.IsSuccess, color, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Set color operation failed: {Color}", color);
            return false;
        }
    }

    public async Task<bool> SetColorTemperatureAsync(int temperature)
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot set color temperature");
                return false;
            }

            if (temperature < 1000 || temperature > 10000)
            {
                Logger.LogWarning("Color temperature out of range: {Temperature}, valid range: 1000-10000K", temperature);
                return false;
            }

            var parameters = new Dictionary<string, object>
            {
                ["color_temperature"] = temperature
            };

            var result = await DeviceConnection.ExecuteActionAsync("set_color_temperature", parameters);
            
            Logger.LogInformation("Virtual smart light set color temperature operation: {Success}, temperature: {Temperature}K, result: {Result}", 
                result.IsSuccess, temperature, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Set color temperature operation failed: {Temperature}", temperature);
            return false;
        }
    }

    public async Task<SmartLightStatus> GetLightStatusAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                return new SmartLightStatus
                {
                    Power = false,
                    Brightness = 0,
                    Color = "#FFFFFF",
                    ColorTemperature = 4000,
                    EnergyConsumption = 0,
                    OperatingHours = 0
                };
            }

            // Read current properties from API
            var power = await DeviceConnection.ReadPropertyAsync("Power");
            var brightness = await DeviceConnection.ReadPropertyAsync("Brightness");
            var color = await DeviceConnection.ReadPropertyAsync("Color");
            var colorTemp = await DeviceConnection.ReadPropertyAsync("ColorTemperature");
            var energy = await DeviceConnection.ReadPropertyAsync("EnergyConsumption");
            var hours = await DeviceConnection.ReadPropertyAsync("OperatingHours");

            var status = new SmartLightStatus
            {
                Power = power is bool b && b,
                Brightness = brightness is int i ? i : 0,
                Color = color?.ToString() ?? "#FFFFFF",
                ColorTemperature = colorTemp is int ct ? ct : 4000,
                EnergyConsumption = energy is double e ? e : 0.0,
                OperatingHours = hours is double h ? h : 0.0
            };

            Logger.LogDebug("Get virtual smart light status from API: power={Power}, brightness={Brightness}%, color={Color}, temp={ColorTemp}K",
                status.Power, status.Brightness, status.Color, status.ColorTemperature);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get light status from API");
            
            return new SmartLightStatus
            {
                Power = false,
                Brightness = 0,
                Color = "#FFFFFF",
                ColorTemperature = 4000,
                EnergyConsumption = 0,
                OperatingHours = 0
            };
        }
    }

    public async Task<bool> ToggleAsync()
    {
        try
        {
            var status = await GetLightStatusAsync();
            
            if (status.Power)
            {
                Logger.LogInformation("Current light is on, executing turn off operation via API");
                return await TurnOffAsync();
            }
            else
            {
                Logger.LogInformation("Current light is off, executing turn on operation via API");
                return await TurnOnAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to toggle light status via API");
            return false;
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

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        
        Logger.LogInformation("Virtual smart light GAgent activated: {GrainId}", this.GetGrainId());
        
        // If there's connection config and device is not connected, try auto-connect
        if (State.ConnectionConfig != null && !State.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000, cancellationToken); // Delay 2 seconds before connecting
                try
                {
                    await InitializeDeviceConnectionAsync(State.ConnectionConfig);
                    
                    // Enable device monitoring
                    await SetDeviceMonitoringAsync(true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to auto-connect virtual smart light to API");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Virtual smart light GAgent is deactivating: {GrainId}, reason: {Reason}", this.GetGrainId(), reason);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    #region Smart Light Specific Event Handlers

    /// <summary>
    /// Handle smart light turn on event
    /// </summary>
    [EventHandler]
    public async Task HandleTurnOnLightAsync(TurnOnLightEvent @event)
    {
        Logger.LogInformation("Handling turn on light event: {DeviceId}", @event.DeviceId);
        
        var success = await TurnOnAsync();
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "TurnOn",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle smart light turn off event
    /// </summary>
    [EventHandler]
    public async Task HandleTurnOffLightAsync(TurnOffLightEvent @event)
    {
        Logger.LogInformation("Handling turn off light event: {DeviceId}", @event.DeviceId);
        
        var success = await TurnOffAsync();
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "TurnOff",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle set brightness event
    /// </summary>
    [EventHandler]
    public async Task HandleSetBrightnessAsync(SetLightBrightnessEvent @event)
    {
        Logger.LogInformation("Handling set brightness event: {DeviceId}, brightness: {Brightness}%", 
            @event.DeviceId, @event.Brightness);
        
        var success = await SetBrightnessAsync(@event.Brightness);
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = $"SetBrightness({@event.Brightness}%)",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle set color event
    /// </summary>
    [EventHandler]
    public async Task HandleSetColorAsync(SetLightColorEvent @event)
    {
        Logger.LogInformation("Handling set color event: {DeviceId}, color: {Color}", 
            @event.DeviceId, @event.Color);
        
        var success = await SetColorAsync(@event.Color);
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = $"SetColor({@event.Color})",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle set color temperature event
    /// </summary>
    [EventHandler]
    public async Task HandleSetColorTemperatureAsync(SetLightColorTemperatureEvent @event)
    {
        Logger.LogInformation("Handling set color temperature event: {DeviceId}, temperature: {Temperature}K", 
            @event.DeviceId, @event.Temperature);
        
        var success = await SetColorTemperatureAsync(@event.Temperature);
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = $"SetColorTemperature({@event.Temperature}K)",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle toggle light event
    /// </summary>
    [EventHandler]
    public async Task HandleToggleLightAsync(ToggleLightEvent @event)
    {
        Logger.LogInformation("Handling toggle light event: {DeviceId}", @event.DeviceId);
        
        var success = await ToggleAsync();
        
        await PublishAsync(new LightOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "Toggle",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle get light status event
    /// </summary>
    [EventHandler]
    public async Task HandleGetLightStatusAsync(GetLightStatusEvent @event)
    {
        Logger.LogInformation("Handling get light status event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var status = await GetLightStatusAsync();
            
            await PublishAsync(new LightStatusResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                LightStatus = status,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get light status: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_STATUS_FAILED",
                ErrorMessage = $"Failed to get light status: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion
}