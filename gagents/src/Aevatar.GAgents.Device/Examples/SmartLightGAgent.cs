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
/// Smart light GAgent interface
/// </summary>
public interface ISmartLightGAgent : IDeviceGAgent<VirtualDeviceConnection>
{
    /// <summary>
    /// Turn on light
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOnAsync();
    
    /// <summary>
    /// Turn off light
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> TurnOffAsync();
    
    /// <summary>
    /// Set brightness
    /// </summary>
    /// <param name="brightness">Brightness percentage (0-100)</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetBrightnessAsync(int brightness);
    
    /// <summary>
    /// Set color
    /// </summary>
    /// <param name="color">Color value (RGB hex, e.g. #FF0000)</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetColorAsync(string color);
    
    /// <summary>
    /// Get current light bulb status
    /// </summary>
    /// <returns>Light bulb status information</returns>
    Task<SmartLightStatus> GetLightStatusAsync();
    
    /// <summary>
    /// Toggle light bulb on/off status
    /// </summary>
    /// <returns>Whether operation was successful</returns>
    Task<bool> ToggleAsync();
}

/// <summary>
/// Smart light status information
/// </summary>
[GenerateSerializer]
public class SmartLightStatus
{
    /// <summary>
    /// Whether turned on
    /// </summary>
    [Id(0)] public bool IsOn { get; set; }
    
    /// <summary>
    /// Brightness percentage (0-100)
    /// </summary>
    [Id(1)] public int Brightness { get; set; }
    
    /// <summary>
    /// Color value (RGB hex)
    /// </summary>
    [Id(2)] public string Color { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// Power consumption (watts)
    /// </summary>
    [Id(3)] public double PowerConsumption { get; set; }
    
    /// <summary>
    /// Last updated time
    /// </summary>
    [Id(4)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Smart light GAgent implementation
/// </summary>
[Description("Smart light device agent providing on/off, brightness adjustment, color setting and other functions, supports intelligent control through AI assistant")]
[GAgent("smart-light", "device")]
public class SmartLightGAgent : DeviceGAgentBase<VirtualDeviceConnection>, ISmartLightGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        var deviceName = DeviceConnection?.DeviceName ?? "Smart Light";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        return $"Smart light device agent ({deviceName}) - Current status: {GetStatusDescription(status)}. " +
               $"Supports on/off control, brightness adjustment (0-100%), color setting (RGB) and other functions, " +
               $"can be intelligently controlled through voice or AI assistant.";
    }

    protected override async Task<VirtualDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
        
        return new VirtualDeviceConnection(
            config.DeviceId,
            config.DeviceName,
            "SmartLight", // Fixed as smart light type
            logger,
            config.ExtendedProperties // Pass extended properties for initial configuration
        );
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

            var result = await DeviceConnection.ExecuteActionAsync("TurnOn");
            
            Logger.LogInformation("Smart light turn on operation: {Success}, result: {Result}", 
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

            var result = await DeviceConnection.ExecuteActionAsync("TurnOff");
            
            Logger.LogInformation("Smart light turn off operation: {Success}, result: {Result}", 
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

            var result = await DeviceConnection.ExecuteActionAsync("SetBrightness", parameters);
            
            Logger.LogInformation("Smart light set brightness operation: {Success}, brightness: {Brightness}%, result: {Result}", 
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

            // Validate color format (simple validation)
            if (!color.StartsWith("#") || color.Length != 7)
            {
                Logger.LogWarning("Invalid color format: {Color}, should be RGB hex format like #FF0000", color);
                return false;
            }

            var success = await DeviceConnection.WritePropertyAsync("Color", color);
            
            Logger.LogInformation("Smart light set color operation: {Success}, color: {Color}", success, color);
            
            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Set color operation failed: {Color}", color);
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
                    IsOn = false,
                    Brightness = 0,
                    Color = "#FFFFFF",
                    PowerConsumption = 0,
                    LastUpdated = DateTime.UtcNow
                };
            }

            // Read device properties
            var power = await DeviceConnection.ReadPropertyAsync("Power");
            var brightness = await DeviceConnection.ReadPropertyAsync("Brightness");
            var color = await DeviceConnection.ReadPropertyAsync("Color");

            var isOn = power is bool b && b;
            var brightnessValue = brightness is int i ? i : 0;
            var colorValue = color?.ToString() ?? "#FFFFFF";

            // Calculate power consumption (simple estimation)
            var powerConsumption = isOn ? (brightnessValue / 100.0 * 10.0) : 0.0; // Max 10 watts

            var status = new SmartLightStatus
            {
                IsOn = isOn,
                Brightness = brightnessValue,
                Color = colorValue,
                PowerConsumption = Math.Round(powerConsumption, 2),
                LastUpdated = DateTime.UtcNow
            };

            Logger.LogDebug("Get smart light status: on={IsOn}, brightness={Brightness}%, color={Color}, power={Power}W", 
                status.IsOn, status.Brightness, status.Color, status.PowerConsumption);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get light status");
            
            return new SmartLightStatus
            {
                IsOn = false,
                Brightness = 0,
                Color = "#FFFFFF",
                PowerConsumption = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> ToggleAsync()
    {
        try
        {
            var status = await GetLightStatusAsync();
            
            if (status.IsOn)
            {
                Logger.LogInformation("Current light is on, executing turn off operation");
                return await TurnOffAsync();
            }
            else
            {
                Logger.LogInformation("Current light is off, executing turn on operation");
                return await TurnOnAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to toggle light status");
            return false;
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
        
        Logger.LogInformation("Smart light GAgent activated: {GrainId}", this.GetGrainId());
        
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
                    Logger.LogWarning(ex, "Failed to auto-connect smart light");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Smart light GAgent is deactivating: {GrainId}, reason: {Reason}", this.GetGrainId(), reason);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
