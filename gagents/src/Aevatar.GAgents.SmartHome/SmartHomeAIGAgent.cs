using System.ComponentModel;
using System.Text.Json;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Core;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.GAgents;
using Aevatar.GAgents.Device.Http.Events;
using Aevatar.GAgents.Device.Http.GAgents;
using Aevatar.GAgents.Device.Http.Models;
using Aevatar.GAgents.Device.State;
using Aevatar.GAgents.Device.Http;
using Aevatar.GAgents.SmartHome.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.SmartHome;

/// <summary>
/// Smart Home AI GAgent interface
/// </summary>
public interface ISmartHomeAIGAgent : IStateGAgent<SmartHomeAIGAgentState>, IAIGAgent
{
    /// <summary>
    /// Register a smart home device
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="deviceName">Device name</param>
    /// <param name="deviceType">Device type (smart-light, smart-switch, temperature-sensor)</param>
    /// <returns>Whether registration was successful</returns>
    Task<bool> RegisterDeviceAsync(string deviceId, string deviceName, string deviceType);

    /// <summary>
    /// Unregister a smart home device
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Whether unregistration was successful</returns>
    Task<bool> UnregisterDeviceAsync(string deviceId);

    /// <summary>
    /// Get all registered devices
    /// </summary>
    /// <returns>List of registered devices</returns>
    Task<List<SmartHomeDeviceInfo>> GetRegisteredDevicesAsync();

    /// <summary>
    /// Execute natural language command
    /// </summary>
    /// <param name="command">Natural language command</param>
    /// <returns>Execution result</returns>
    Task<SmartHomeCommandResult> ExecuteNaturalLanguageCommandAsync(string command);

    /// <summary>
    /// Get home status overview
    /// </summary>
    /// <returns>Home status overview</returns>
    Task<SmartHomeStatusOverview> GetHomeStatusOverviewAsync();
}

/// <summary>
/// Smart Home AI GAgent State
/// </summary>
[GenerateSerializer]
public class SmartHomeAIGAgentState : AIGAgentStateBase
{
    /// <summary>
    /// Registered smart home devices
    /// </summary>
    [Id(0)] public Dictionary<string, SmartHomeDeviceInfo> RegisteredDevices { get; set; } = new();

    /// <summary>
    /// Command execution history
    /// </summary>
    [Id(1)] public List<SmartHomeCommandRecord> CommandHistory { get; set; } = new();

    /// <summary>
    /// Device connection configurations
    /// </summary>
    [Id(2)] public Dictionary<string, DeviceConnectionConfig> DeviceConfigs { get; set; } = new();
}

/// <summary>
/// Smart Home AI GAgent State Log Event
/// </summary>
[GenerateSerializer]
public class SmartHomeAIGAgentStateLogEvent : StateLogEventBase<SmartHomeAIGAgentStateLogEvent>
{
}

/// <summary>
/// Device registered log event
/// </summary>
[GenerateSerializer]
public class DeviceRegisteredLogEvent : SmartHomeAIGAgentStateLogEvent
{
    [Id(0)] public SmartHomeDeviceInfo Device { get; set; } = new();
    [Id(1)] public DeviceConnectionConfig? Config { get; set; }
}

/// <summary>
/// Device unregistered log event
/// </summary>
[GenerateSerializer]
public class DeviceUnregisteredLogEvent : SmartHomeAIGAgentStateLogEvent
{
    [Id(0)] public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// Command executed log event
/// </summary>
[GenerateSerializer]
public class CommandExecutedLogEvent : SmartHomeAIGAgentStateLogEvent
{
    [Id(0)] public SmartHomeCommandRecord CommandRecord { get; set; } = new();
}

/// <summary>
/// Smart Home AI GAgent Configuration
/// </summary>
[GenerateSerializer]
public class SmartHomeAIGAgentConfiguration : AIGAgentConfigurationBase
{
    /// <summary>
    /// Device Hub API base URL
    /// </summary>
    [Id(0)] public string DeviceHubApiUrl { get; set; } = "http://localhost:9001";

    /// <summary>
    /// Device Hub API key
    /// </summary>
    [Id(1)] public string? DeviceHubApiKey { get; set; }

    /// <summary>
    /// Pre-configured devices
    /// </summary>
    [Id(2)] public List<SmartHomeDeviceInfo> PreConfiguredDevices { get; set; } = new();

    /// <summary>
    /// LLM Configuration for AI functionality
    /// </summary>
    [Id(3)] public LLMConfigDto LLMConfig { get; set; } = new() { SystemLLM = "OpenAI" };
}

/// <summary>
/// Smart Home AI GAgent - AI assistant for smart home device control
/// </summary>
[Description("AI assistant for smart home device control with natural language understanding. Manages smart lights, switches, and sensors through HTTP API calls.")]
[GAgent("smart-home-ai", "smarthome")]
public class SmartHomeAIGAgent : AIGAgentBase<SmartHomeAIGAgentState, SmartHomeAIGAgentStateLogEvent, EventBase, SmartHomeAIGAgentConfiguration>, ISmartHomeAIGAgent
{
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

    public override async Task<string> GetDescriptionAsync()
    {
        var deviceCount = State.RegisteredDevices.Count;
        var recentCommands = State.CommandHistory.TakeLast(5).Count();

        var description = $"Smart Home AI Assistant - Managing {deviceCount} devices\n\n";
        
        description += "🏠 SMART HOME CONTROL CAPABILITIES:\n\n";
        
        description += "1. NATURAL LANGUAGE COMMANDS:\n";
        description += "   • \"Turn on the living room light\"\n";
        description += "   • \"Set bedroom lamp to 50% brightness\"\n";
        description += "   • \"Make the kitchen light red\"\n";
        description += "   • \"Check temperature in living room\"\n";
        description += "   • \"Turn off all lights\"\n\n";
        
        description += "2. DEVICE MANAGEMENT:\n";
        description += "   • Register/unregister smart devices\n";
        description += "   • Monitor device status and health\n";
        description += "   • Automatic device discovery\n\n";
        
        description += "3. SUPPORTED DEVICE TYPES:\n";
        description += "   • Smart Lights: On/off, brightness, color, temperature\n";
        description += "   • Smart Switches: Power control, energy monitoring\n";
        description += "   • Temperature Sensors: Temperature, humidity, battery\n\n";

        if (deviceCount > 0)
        {
            description += "📱 REGISTERED DEVICES:\n";
            foreach (var device in State.RegisteredDevices.Values)
            {
                var statusIcon = device.IsOnline ? "🟢" : "🔴";
                description += $"   {statusIcon} {device.Name} ({device.DeviceId}) - {device.DeviceType}\n";
            }
            description += "\n";
        }

        if (recentCommands > 0)
        {
            description += "📝 RECENT COMMANDS:\n";
            foreach (var cmd in State.CommandHistory.TakeLast(3))
            {
                var resultIcon = cmd.Success ? "✅" : "❌";
                description += $"   {resultIcon} \"{cmd.Command}\" - {cmd.Timestamp:HH:mm:ss}\n";
            }
            description += "\n";
        }

        description += "💡 USAGE EXAMPLES:\n";
        description += "   • Chat: \"Turn on the living room main light\"\n";
        description += "   • Chat: \"What's the temperature in the bedroom?\"\n";
        description += "   • Chat: \"Set all lights to warm white\"\n";
        description += "   • Event: SendSmartHomeCommandEvent with natural language\n\n";

        description += "🔧 CONFIGURATION:\n";
        description += $"   • Device Hub API: {State.DeviceConfigs.FirstOrDefault().Value?.ExtendedProperties?.GetValueOrDefault("BaseUrl", "Not configured")}\n";
        description += $"   • AI Model: {State.SystemLLM ?? "Not initialized"}\n";

        return description;
    }

    protected override async Task PerformConfigAsync(SmartHomeAIGAgentConfiguration configuration)
    {
        // Initialize AI capabilities
        var initDto = new InitializeDto
        {
            LLMConfig = configuration.LLMConfig,
            Instructions = GetSmartHomeInstructions(),
            StreamingModeEnabled = false,
            StreamingConfig = new StreamingConfig()
        };

        await InitializeAsync(initDto);

        // Register pre-configured devices
        foreach (var device in configuration.PreConfiguredDevices)
        {
            var config = new DeviceConnectionConfig
            {
                DeviceId = device.DeviceId,
                DeviceName = device.Name,
                DeviceType = device.DeviceType,
                ExtendedProperties = new Dictionary<string, string>
                {
                    ["BaseUrl"] = configuration.DeviceHubApiUrl
                }
            };

            if (!string.IsNullOrEmpty(configuration.DeviceHubApiKey))
            {
                config.ExtendedProperties["ApiKey"] = configuration.DeviceHubApiKey;
            }

            RaiseEvent(new DeviceRegisteredLogEvent
            {
                Device = device,
                Config = config
            });
        }

        await ConfirmEvents();
    }

    protected override void AIGAgentTransitionState(SmartHomeAIGAgentState state, StateLogEventBase<SmartHomeAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case DeviceRegisteredLogEvent deviceRegistered:
                state.RegisteredDevices[deviceRegistered.Device.DeviceId] = deviceRegistered.Device;
                if (deviceRegistered.Config != null)
                {
                    state.DeviceConfigs[deviceRegistered.Device.DeviceId] = deviceRegistered.Config;
                }
                break;

            case DeviceUnregisteredLogEvent deviceUnregistered:
                state.RegisteredDevices.Remove(deviceUnregistered.DeviceId);
                state.DeviceConfigs.Remove(deviceUnregistered.DeviceId);
                break;

            case CommandExecutedLogEvent commandExecuted:
                state.CommandHistory.Add(commandExecuted.CommandRecord);
                // Keep only last 100 commands
                if (state.CommandHistory.Count > 100)
                {
                    state.CommandHistory.RemoveRange(0, state.CommandHistory.Count - 100);
                }
                break;
        }
    }

    public async Task<bool> RegisterDeviceAsync(string deviceId, string deviceName, string deviceType)
    {
        try
        {
            if (State.RegisteredDevices.ContainsKey(deviceId))
            {
                Logger.LogWarning("Device {DeviceId} is already registered", deviceId);
                return false;
            }

            var deviceInfo = new SmartHomeDeviceInfo
            {
                DeviceId = deviceId,
                Name = deviceName,
                DeviceType = deviceType,
                RegisteredAt = DateTime.UtcNow,
                IsOnline = false
            };

            var config = new DeviceConnectionConfig
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceType = deviceType,
                ExtendedProperties = new Dictionary<string, string>
                {
                    ["BaseUrl"] = State.DeviceConfigs.FirstOrDefault().Value?.ExtendedProperties?.GetValueOrDefault("BaseUrl", "http://localhost:9001") ?? "http://localhost:9001"
                }
            };

            // Try to connect to the device to verify it exists
            var deviceAgent = await CreateDeviceAgentAsync(deviceType, Guid.NewGuid());
            if (deviceAgent != null)
            {
                // Test connection
                await deviceAgent.InitializeDeviceConnectionAsync(config);
                var status = await deviceAgent.GetDeviceStatusSummaryAsync();
                deviceInfo.IsOnline = status?.IsConnected ?? false;
            }

            RaiseEvent(new DeviceRegisteredLogEvent
            {
                Device = deviceInfo,
                Config = config
            });

            await ConfirmEvents();

            Logger.LogInformation("Successfully registered device {DeviceId} ({DeviceName})", deviceId, deviceName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register device {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceAsync(string deviceId)
    {
        try
        {
            if (!State.RegisteredDevices.ContainsKey(deviceId))
            {
                Logger.LogWarning("Device {DeviceId} is not registered", deviceId);
                return false;
            }

            RaiseEvent(new DeviceUnregisteredLogEvent
            {
                DeviceId = deviceId
            });

            await ConfirmEvents();

            Logger.LogInformation("Successfully unregistered device {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unregister device {DeviceId}", deviceId);
            return false;
        }
    }

    public Task<List<SmartHomeDeviceInfo>> GetRegisteredDevicesAsync()
    {
        return Task.FromResult(State.RegisteredDevices.Values.ToList());
    }

    public async Task<SmartHomeCommandResult> ExecuteNaturalLanguageCommandAsync(string command)
    {
        try
        {
            Logger.LogInformation("Executing natural language command: {Command}", command);

            var commandRecord = new SmartHomeCommandRecord
            {
                Command = command,
                Timestamp = DateTime.UtcNow,
                Success = false
            };

            // Use AI to understand and execute the command
            var chatResponse = await ChatWithHistory($"Execute this smart home command: {command}");

            var responseContent = chatResponse?.LastOrDefault()?.Content ?? "Failed to process command";
            var result = new SmartHomeCommandResult
            {
                Command = command,
                Success = chatResponse?.Any() == true,
                Response = responseContent,
                ExecutedAt = DateTime.UtcNow
            };

            commandRecord.Success = result.Success;
            commandRecord.Response = result.Response;

            RaiseEvent(new CommandExecutedLogEvent
            {
                CommandRecord = commandRecord
            });

            await ConfirmEvents();

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute natural language command: {Command}", command);
            
            var errorResult = new SmartHomeCommandResult
            {
                Command = command,
                Success = false,
                Response = $"Error executing command: {ex.Message}",
                ExecutedAt = DateTime.UtcNow
            };

            var errorRecord = new SmartHomeCommandRecord
            {
                Command = command,
                Timestamp = DateTime.UtcNow,
                Success = false,
                Response = errorResult.Response
            };

            RaiseEvent(new CommandExecutedLogEvent
            {
                CommandRecord = errorRecord
            });

            await ConfirmEvents();

            return errorResult;
        }
    }

    public async Task<SmartHomeStatusOverview> GetHomeStatusOverviewAsync()
    {
        var overview = new SmartHomeStatusOverview
        {
            TotalDevices = State.RegisteredDevices.Count,
            OnlineDevices = 0,
            OfflineDevices = 0,
            DevicesByType = new Dictionary<string, int>(),
            LastUpdated = DateTime.UtcNow
        };

        // Count devices by type and status
        foreach (var device in State.RegisteredDevices.Values)
        {
            if (device.IsOnline)
                overview.OnlineDevices++;
            else
                overview.OfflineDevices++;

            if (overview.DevicesByType.ContainsKey(device.DeviceType))
                overview.DevicesByType[device.DeviceType]++;
            else
                overview.DevicesByType[device.DeviceType] = 1;
        }

        // Try to get real-time status for online devices
        var deviceStatuses = new List<SmartHomeDeviceStatus>();
        foreach (var device in State.RegisteredDevices.Values.Where(d => d.IsOnline))
        {
            try
            {
                var deviceAgent = await CreateDeviceAgentAsync(device.DeviceType, Guid.NewGuid());
                if (deviceAgent != null && State.DeviceConfigs.TryGetValue(device.DeviceId, out var config))
                {
                    await deviceAgent.InitializeDeviceConnectionAsync(config);
                    var status = await deviceAgent.GetDeviceStatusSummaryAsync();
                    
                    deviceStatuses.Add(new SmartHomeDeviceStatus
                    {
                        DeviceId = device.DeviceId,
                        DeviceName = device.Name,
                        DeviceType = device.DeviceType,
                        IsOnline = status?.IsConnected ?? false,
                        LastUpdated = DateTime.UtcNow,
                        Properties = new Dictionary<string, object>()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get status for device {DeviceId}", device.DeviceId);
            }
        }

        overview.DeviceStatuses = deviceStatuses;
        return overview;
    }

    private async Task<IDeviceGAgent<HttpDeviceConnection>?> CreateDeviceAgentAsync(string deviceType, Guid agentId)
    {
        try
        {
            return deviceType.ToLower() switch
            {
                "smart-light" => await GAgentFactory.GetGAgentAsync<IHttpSmartLightGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                "smart-switch" => await GAgentFactory.GetGAgentAsync<IHttpSmartSwitchGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                "temperature-sensor" => await GAgentFactory.GetGAgentAsync<IHttpTemperatureSensorGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                _ => null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create device agent for type {DeviceType}", deviceType);
            return null;
        }
    }

    private static string GetSmartHomeInstructions()
    {
        return @"You are a Smart Home AI Assistant that helps users control their smart home devices through natural language commands.

AVAILABLE DEVICES AND CAPABILITIES:
- Smart Lights: Turn on/off, adjust brightness (0-100%), change color (RGB hex), set color temperature (1000-10000K)
- Smart Switches: Turn on/off, monitor power consumption, check electrical parameters
- Temperature Sensors: Read temperature, humidity, battery level, calibrate

COMMAND INTERPRETATION GUIDELINES:
1. Parse user intent from natural language
2. Identify target devices by name or location
3. Extract specific parameters (brightness, color, etc.)
4. Execute appropriate device commands
5. Provide clear feedback on results

EXAMPLE COMMAND PATTERNS:
- ""Turn on [device name]"" → TurnOnLightEvent or TurnOnSwitchEvent  
- ""Set [device] to [X]% brightness"" → SetLightBrightnessEvent
- ""Make [device] [color]"" → SetLightColorEvent
- ""Check temperature in [location]"" → GetTemperatureEvent
- ""Turn off all lights"" → Multiple TurnOffLightEvents

RESPONSE FORMAT:
- Acknowledge the command
- Describe what actions were taken
- Report any errors or issues
- Provide current device status if relevant

Always be helpful, clear, and provide specific feedback about device operations.";
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleSmartHomeCommandAsync(SendSmartHomeCommandEvent @event)
    {
        Logger.LogInformation("Handling smart home command event: {Command}", @event.Command);
        
        var result = await ExecuteNaturalLanguageCommandAsync(@event.Command);
        
        await PublishAsync(new SmartHomeCommandResultEvent
        {
            Command = @event.Command,
            Success = result.Success,
            Response = result.Response,
            ExecutedAt = result.ExecutedAt,
            Timestamp = DateTime.UtcNow
        });
    }

    [EventHandler]
    public async Task HandleRegisterDeviceAsync(RegisterSmartHomeDeviceEvent @event)
    {
        Logger.LogInformation("Handling register device event: {DeviceId}", @event.DeviceId);
        
        var success = await RegisterDeviceAsync(@event.DeviceId, @event.DeviceName, @event.DeviceType);
        
        await PublishAsync(new SmartHomeDeviceRegistrationResultEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            DeviceType = @event.DeviceType,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    [EventHandler]
    public async Task HandleUnregisterDeviceAsync(UnregisterSmartHomeDeviceEvent @event)
    {
        Logger.LogInformation("Handling unregister device event: {DeviceId}", @event.DeviceId);
        
        var success = await UnregisterDeviceAsync(@event.DeviceId);
        
        await PublishAsync(new SmartHomeDeviceUnregistrationResultEvent
        {
            DeviceId = @event.DeviceId,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    [EventHandler]
    public async Task HandleGetHomeStatusAsync(GetSmartHomeStatusEvent @event)
    {
        Logger.LogInformation("Handling get home status event");
        
        try
        {
            var overview = await GetHomeStatusOverviewAsync();
            
            await PublishAsync(new SmartHomeStatusResponseEvent
            {
                StatusOverview = overview,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get home status");
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = "smart-home-ai",
                DeviceName = "Smart Home AI",
                ErrorCode = "GET_STATUS_FAILED",
                ErrorMessage = $"Failed to get home status: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        Logger.LogInformation("Smart Home AI GAgent activated: {GrainId}", this.GetGrainId());
        
        // Update device online status periodically
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                    await UpdateDeviceStatusesAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error updating device statuses");
                }
            }
        }, cancellationToken);
    }

    private async Task UpdateDeviceStatusesAsync()
    {
        foreach (var device in State.RegisteredDevices.Values.ToList())
        {
            try
            {
                    if (State.DeviceConfigs.TryGetValue(device.DeviceId, out var config))
                    {
                        var deviceAgent = await CreateDeviceAgentAsync(device.DeviceType, Guid.NewGuid());
                        if (deviceAgent != null)
                        {
                            await deviceAgent.InitializeDeviceConnectionAsync(config);
                            var status = await deviceAgent.GetDeviceStatusSummaryAsync();
                        
                        var updatedDevice = device with { IsOnline = status?.IsConnected ?? false };
                        
                        if (updatedDevice.IsOnline != device.IsOnline)
                        {
                            RaiseEvent(new DeviceRegisteredLogEvent
                            {
                                Device = updatedDevice,
                                Config = config
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Failed to update status for device {DeviceId}", device.DeviceId);
            }
        }

        await ConfirmEvents();
    }
}
