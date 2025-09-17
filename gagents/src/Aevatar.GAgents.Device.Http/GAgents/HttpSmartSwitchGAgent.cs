using System.ComponentModel;
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
/// Virtual Smart Switch GAgent interface - connects to real API
/// </summary>
public interface IHttpSmartSwitchGAgent : IDeviceGAgent<HttpDeviceConnection>
{
    /// <summary>
    /// Turn on switch via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOnAsync();
    
    /// <summary>
    /// Turn off switch via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOffAsync();
    
    /// <summary>
    /// Toggle switch on/off via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> ToggleAsync();
    
    /// <summary>
    /// Get current load from API
    /// </summary>
    /// <returns>Current load in amperes</returns>
    Task<double> GetCurrentLoadAsync();
    
    /// <summary>
    /// Get voltage from API
    /// </summary>
    /// <returns>Voltage in volts</returns>
    Task<double> GetVoltageAsync();
    
    /// <summary>
    /// Get power consumption from API
    /// </summary>
    /// <returns>Power consumption in watts</returns>
    Task<double> GetPowerConsumptionAsync();
    
    /// <summary>
    /// Get daily usage from API
    /// </summary>
    /// <returns>Daily usage in kWh</returns>
    Task<double> GetDailyUsageAsync();
    
    /// <summary>
    /// Get switch temperature from API
    /// </summary>
    /// <returns>Switch temperature in Celsius</returns>
    Task<double> GetSwitchTemperatureAsync();
    
    /// <summary>
    /// Get complete switch status from API
    /// </summary>
    /// <returns>Smart switch status information</returns>
    Task<SmartSwitchStatus> GetSwitchStatusAsync();
    
    /// <summary>
    /// Reset daily usage counter via API
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> ResetDailyUsageAsync();
}

/// <summary>
/// Virtual Smart Switch GAgent - connects to Virtual Device Hub API
/// </summary>
[Description("Virtual smart switch device agent connecting to Virtual Device Hub API, providing on/off control, power monitoring, and usage tracking through real API calls")]
[GAgent("http-smart-switch", "device")]
public class HttpSmartSwitchGAgent : DeviceGAgentBase<HttpDeviceConnection>, IHttpSmartSwitchGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        var deviceId = DeviceConnection?.DeviceId ?? State.DeviceId ?? "unknown";
        var deviceName = DeviceConnection?.DeviceName ?? State.DeviceName ?? "HTTP Smart Switch";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        
        var description = $"HTTP Smart Switch Controller - Device ID: {deviceId}, Name: {deviceName}\n";
        description += $"Connection Status: {GetStatusDescription(status)}\n\n";
        
        description += "CONTROL THIS SWITCH BY SENDING EVENTS:\n\n";
        
        description += "1. TURN SWITCH ON/OFF:\n";
        description += "   • TurnOnSwitchEvent: Turn on the switch\n";
        description += "   • TurnOffSwitchEvent: Turn off the switch\n";
        description += "   • ToggleSwitchEvent: Toggle switch on/off state\n\n";
        
        description += "2. RESET DAILY USAGE:\n";
        description += "   • ResetDailyUsageEvent: Reset daily energy counter to zero\n\n";
        
        description += "3. MONITOR ELECTRICAL PARAMETERS:\n";
        description += "   • GetCurrentLoadEvent: Get current electrical load\n";
        description += "   • GetVoltageEvent: Get voltage reading\n";
        description += "   • GetPowerConsumptionEvent: Get real-time power consumption\n";
        description += "   • GetDailyUsageEvent: Get daily energy usage\n";
        description += "   • GetSwitchTemperatureEvent: Get switch internal temperature\n\n";
        
        description += "4. GET COMPLETE STATUS:\n";
        description += "   • GetSwitchStatusEvent: Get all switch measurements\n";
        description += "   • GetDeviceStatusEvent: Get general device status\n\n";
        
        if (DeviceConnection?.Status == DeviceConnectionStatus.Connected)
        {
            try
            {
                var switchStatus = await GetSwitchStatusAsync();
                description += "CURRENT STATUS:\n";
                description += $"• Power State: {(switchStatus.Power ? "ON (Energized)" : "OFF (De-energized)")}\n";
                description += $"• Current Load: {switchStatus.CurrentLoad:F2} Amperes\n";
                description += $"• Voltage: {switchStatus.Voltage:F1} Volts\n";
                description += $"• Power Consumption: {switchStatus.PowerConsumption:F1} Watts\n";
                description += $"• Daily Usage: {switchStatus.DailyUsage:F2} kWh\n";
                description += $"• Switch Temperature: {switchStatus.Temperature:F1}°C\n";
                
                // Add safety warnings if needed
                if (switchStatus.Temperature > 60)
                    description += $"⚠️ WARNING: Switch temperature is high ({switchStatus.Temperature:F1}°C)\n";
                if (switchStatus.CurrentLoad > 15)
                    description += $"⚠️ WARNING: High current load ({switchStatus.CurrentLoad:F1}A)\n";
                if (switchStatus.Voltage < 200 || switchStatus.Voltage > 250)
                    description += $"⚠️ WARNING: Voltage out of normal range ({switchStatus.Voltage:F1}V)\n";
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
        
        description += "\nThis switch connects to Virtual Device Hub API. Control it by sending the appropriate events listed above.";
        description += "\n⚠️ SAFETY NOTE: This device controls electrical power flow - use with caution.";
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

        return factory.CreateConnection(config.DeviceId, "smart-switch", options);
    }

    public async Task<bool> TurnOnAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot turn on switch");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("turn_on");
            
            Logger.LogInformation("Virtual smart switch turn on operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Turn on switch operation failed");
            return false;
        }
    }

    public async Task<bool> TurnOffAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot turn off switch");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("turn_off");
            
            Logger.LogInformation("Virtual smart switch turn off operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Turn off switch operation failed");
            return false;
        }
    }

    public async Task<bool> ToggleAsync()
    {
        try
        {
            var status = await GetSwitchStatusAsync();
            
            if (status.Power)
            {
                Logger.LogInformation("Current switch is on, executing turn off operation via API");
                return await TurnOffAsync();
            }
            else
            {
                Logger.LogInformation("Current switch is off, executing turn on operation via API");
                return await TurnOnAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to toggle switch status via API");
            return false;
        }
    }

    public async Task<double> GetCurrentLoadAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read current load");
                return 0.0;
            }

            var load = await DeviceConnection.ReadPropertyAsync("CurrentLoad");
            
            if (load is double loadValue)
            {
                Logger.LogDebug("Read current load from API: {Load:F2}A", loadValue);
                return loadValue;
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read current load from API");
            return 0.0;
        }
    }

    public async Task<double> GetVoltageAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read voltage");
                return 0.0;
            }

            var voltage = await DeviceConnection.ReadPropertyAsync("Voltage");
            
            if (voltage is double voltageValue)
            {
                Logger.LogDebug("Read voltage from API: {Voltage:F1}V", voltageValue);
                return voltageValue;
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read voltage from API");
            return 0.0;
        }
    }

    public async Task<double> GetPowerConsumptionAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read power consumption");
                return 0.0;
            }

            var power = await DeviceConnection.ReadPropertyAsync("PowerConsumption");
            
            if (power is double powerValue)
            {
                Logger.LogDebug("Read power consumption from API: {Power:F1}W", powerValue);
                return powerValue;
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read power consumption from API");
            return 0.0;
        }
    }

    public async Task<double> GetDailyUsageAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read daily usage");
                return 0.0;
            }

            var usage = await DeviceConnection.ReadPropertyAsync("DailyUsage");
            
            if (usage is double usageValue)
            {
                Logger.LogDebug("Read daily usage from API: {Usage:F2}kWh", usageValue);
                return usageValue;
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read daily usage from API");
            return 0.0;
        }
    }

    public async Task<double> GetSwitchTemperatureAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot read switch temperature");
                return 0.0;
            }

            var temp = await DeviceConnection.ReadPropertyAsync("Temperature");
            
            if (temp is double tempValue)
            {
                Logger.LogDebug("Read switch temperature from API: {Temperature:F1}°C", tempValue);
                return tempValue;
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read switch temperature from API");
            return 0.0;
        }
    }

    public async Task<SmartSwitchStatus> GetSwitchStatusAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                return new SmartSwitchStatus
                {
                    Power = false,
                    CurrentLoad = 0,
                    Voltage = 0,
                    PowerConsumption = 0,
                    DailyUsage = 0,
                    Temperature = 0
                };
            }

            var power = await DeviceConnection.ReadPropertyAsync("Power");
            var currentLoad = await GetCurrentLoadAsync();
            var voltage = await GetVoltageAsync();
            var powerConsumption = await GetPowerConsumptionAsync();
            var dailyUsage = await GetDailyUsageAsync();
            var temperature = await GetSwitchTemperatureAsync();

            var status = new SmartSwitchStatus
            {
                Power = power is bool b && b,
                CurrentLoad = currentLoad,
                Voltage = voltage,
                PowerConsumption = powerConsumption,
                DailyUsage = dailyUsage,
                Temperature = temperature
            };

            Logger.LogInformation("Get virtual switch status from API: Power={Power}, Load={Load:F1}A, Voltage={Voltage:F1}V, Consumption={Consumption:F1}W",
                status.Power, status.CurrentLoad, status.Voltage, status.PowerConsumption);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get switch status from API");
            
            return new SmartSwitchStatus
            {
                Power = false,
                CurrentLoad = 0,
                Voltage = 0,
                PowerConsumption = 0,
                DailyUsage = 0,
                Temperature = 0
            };
        }
    }

    public async Task<bool> ResetDailyUsageAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("Device not connected, cannot reset daily usage");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("reset_daily_usage");
            
            Logger.LogInformation("Virtual smart switch reset daily usage operation: {Success}, result: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reset daily usage operation failed");
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
        
        Logger.LogInformation("Virtual smart switch GAgent activated: {GrainId}", this.GetGrainId());
        
        // If there's connection config and device is not connected, try auto-connect
        if (State.ConnectionConfig != null && !State.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1500, cancellationToken); // Delay 1.5 seconds before connecting
                try
                {
                    await InitializeDeviceConnectionAsync(State.ConnectionConfig);
                    
                    // Enable device monitoring
                    await SetDeviceMonitoringAsync(true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to auto-connect virtual smart switch to API");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Virtual smart switch GAgent is deactivating: {GrainId}, reason: {Reason}", this.GetGrainId(), reason);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    #region Smart Switch Specific Event Handlers

    /// <summary>
    /// Handle turn on switch event
    /// </summary>
    [EventHandler]
    public async Task HandleTurnOnSwitchAsync(TurnOnSwitchEvent @event)
    {
        Logger.LogInformation("Handling turn on switch event: {DeviceId}", @event.DeviceId);
        
        var success = await TurnOnAsync();
        
        await PublishAsync(new SwitchOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "TurnOn",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle turn off switch event
    /// </summary>
    [EventHandler]
    public async Task HandleTurnOffSwitchAsync(TurnOffSwitchEvent @event)
    {
        Logger.LogInformation("Handling turn off switch event: {DeviceId}", @event.DeviceId);
        
        var success = await TurnOffAsync();
        
        await PublishAsync(new SwitchOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "TurnOff",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle toggle switch event
    /// </summary>
    [EventHandler]
    public async Task HandleToggleSwitchAsync(ToggleSwitchEvent @event)
    {
        Logger.LogInformation("Handling toggle switch event: {DeviceId}", @event.DeviceId);
        
        var success = await ToggleAsync();
        
        await PublishAsync(new SwitchOperationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            Operation = "Toggle",
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle get current load event
    /// </summary>
    [EventHandler]
    public async Task HandleGetCurrentLoadAsync(GetCurrentLoadEvent @event)
    {
        Logger.LogInformation("Handling get current load event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var currentLoad = await GetCurrentLoadAsync();
            
            await PublishAsync(new CurrentLoadResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                CurrentLoad = currentLoad,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get current load: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_CURRENT_LOAD_FAILED",
                ErrorMessage = $"Failed to get current load: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get voltage event
    /// </summary>
    [EventHandler]
    public async Task HandleGetVoltageAsync(GetVoltageEvent @event)
    {
        Logger.LogInformation("Handling get voltage event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var voltage = await GetVoltageAsync();
            
            await PublishAsync(new VoltageResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Voltage = voltage,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get voltage: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_VOLTAGE_FAILED",
                ErrorMessage = $"Failed to get voltage: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get power consumption event
    /// </summary>
    [EventHandler]
    public async Task HandleGetPowerConsumptionAsync(GetPowerConsumptionEvent @event)
    {
        Logger.LogInformation("Handling get power consumption event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var powerConsumption = await GetPowerConsumptionAsync();
            
            await PublishAsync(new PowerConsumptionResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                PowerConsumption = powerConsumption,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get power consumption: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_POWER_CONSUMPTION_FAILED",
                ErrorMessage = $"Failed to get power consumption: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle get daily usage event
    /// </summary>
    [EventHandler]
    public async Task HandleGetDailyUsageAsync(GetDailyUsageEvent @event)
    {
        Logger.LogInformation("Handling get daily usage event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var dailyUsage = await GetDailyUsageAsync();
            
            await PublishAsync(new DailyUsageResponseEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                DailyUsage = dailyUsage,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get daily usage: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "GET_DAILY_USAGE_FAILED",
                ErrorMessage = $"Failed to get daily usage: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }


    /// <summary>
    /// Handle reset daily usage event
    /// </summary>
    [EventHandler]
    public async Task HandleResetDailyUsageAsync(ResetDailyUsageEvent @event)
    {
        Logger.LogInformation("Handling reset daily usage event: {DeviceId}", @event.DeviceId);
        
        try
        {
            var success = await ResetDailyUsageAsync();
            
            await PublishAsync(new SwitchOperationResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Operation = "ResetDailyUsage",
                Success = success,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reset daily usage: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new SwitchOperationResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Operation = "ResetDailyUsage",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion
}
