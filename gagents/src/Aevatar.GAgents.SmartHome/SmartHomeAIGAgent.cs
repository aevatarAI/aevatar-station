using System.ComponentModel;
using System.Text.Json;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using AIChatMessage = Aevatar.GAgents.AI.Common.ChatMessage;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Core;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.GroupChat;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChatMessage = GroupChat.GAgent.Feature.Common.ChatMessage;
using GroupChatResponse = GroupChat.GAgent.Feature.Common.ChatResponse;
using GroupChat.GAgent.GEvent;
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
using Microsoft.SemanticKernel;
using Orleans;
using Orleans.Streams;

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
public class SmartHomeAIGAgentState : GroupMemberState
{
    /// <summary>
    /// Registered smart home devices
    /// </summary>
    [Id(1)] public Dictionary<string, SmartHomeDeviceInfo> RegisteredDevices { get; set; } = new();

    /// <summary>
    /// Command execution history
    /// </summary>
    [Id(2)] public List<SmartHomeCommandRecord> CommandHistory { get; set; } = new();

    /// <summary>
    /// Device connection configurations
    /// </summary>
    [Id(3)] public Dictionary<string, DeviceConnectionConfig> DeviceConfigs { get; set; } = new();

    /// <summary>
    /// Pre-created device GAgent instances mapped by device ID
    /// </summary>
    [Id(4)] public Dictionary<string, Guid> DeviceGAgentInstances { get; set; } = new();
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
/// Devices initialized log event
/// </summary>
[GenerateSerializer]
public class DevicesInitializedLogEvent : SmartHomeAIGAgentStateLogEvent
{
    [Id(0)] public Dictionary<string, Guid> DeviceInstances { get; set; } = new();
    [Id(1)] public Dictionary<string, DeviceConnectionConfig> DeviceConfigs { get; set; } = new();
    [Id(2)] public Dictionary<string, SmartHomeDeviceInfo> RegisteredDevices { get; set; } = new();
}

/// <summary>
/// Smart Home AI GAgent Configuration
/// </summary>
[GenerateSerializer]
public class SmartHomeAIGAgentConfiguration : GroupMemberConfigDto
{
    /// <summary>
    /// Device Hub API base URL
    /// </summary>
    [Id(1)] public string DeviceHubApiUrl { get; set; } = "http://localhost:9001";

    /// <summary>
    /// Device Hub API key
    /// </summary>
    [Id(2)] public string? DeviceHubApiKey { get; set; }

    /// <summary>
    /// Pre-configured devices (hardcoded for demo)
    /// </summary>
    [Id(3)] public List<SmartHomeDeviceInfo> PreConfiguredDevices { get; set; } = new()
    {
        new() { DeviceId = "light001", Name = "客厅主灯", DeviceType = "smart-light", RegisteredAt = DateTime.UtcNow, IsOnline = false },
        new() { DeviceId = "light002", Name = "卧室台灯", DeviceType = "smart-light", RegisteredAt = DateTime.UtcNow, IsOnline = false },
        new() { DeviceId = "temp001", Name = "客厅温度传感器", DeviceType = "temperature-sensor", RegisteredAt = DateTime.UtcNow, IsOnline = false },
        new() { DeviceId = "temp002", Name = "卧室温度传感器", DeviceType = "temperature-sensor", RegisteredAt = DateTime.UtcNow, IsOnline = false },
        new() { DeviceId = "switch001", Name = "客厅总开关", DeviceType = "smart-switch", RegisteredAt = DateTime.UtcNow, IsOnline = false },
        new() { DeviceId = "switch002", Name = "厨房插座", DeviceType = "smart-switch", RegisteredAt = DateTime.UtcNow, IsOnline = false }
    };

    /// <summary>
    /// LLM Configuration for AI functionality
    /// </summary>
    [Id(4)] public LLMConfigDto LLMConfig { get; set; } = new() { SystemLLM = "OpenAI" };

    /// <summary>
    /// Create default demo configuration
    /// </summary>
    public static SmartHomeAIGAgentConfiguration CreateDemoConfig(string? systemLLM = "OpenAI")
    {
        return new SmartHomeAIGAgentConfiguration
        {
            MemberName = "Smart Home AI Assistant",
            DeviceHubApiUrl = "http://localhost:9001",
            DeviceHubApiKey = null, // No API key needed for demo
            LLMConfig = new LLMConfigDto { SystemLLM = systemLLM }
            // PreConfiguredDevices are already hardcoded above
        };
    }
}

/// <summary>
/// Smart Home AI GAgent - AI assistant for smart home device control
/// </summary>
[Description("AI assistant for smart home device control with natural language understanding. Manages smart lights, switches, and sensors through HTTP API calls.")]
[GAgent("smart-home-ai", "smart-home")]
public class SmartHomeAIGAgent : GroupMemberGAgentBase<SmartHomeAIGAgentState, SmartHomeAIGAgentStateLogEvent, EventBase, SmartHomeAIGAgentConfiguration>, ISmartHomeAIGAgent
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
        // Pre-create device GAgent instances for each configured device
        var deviceInstances = new Dictionary<string, Guid>();
        var deviceConfigs = new Dictionary<string, DeviceConnectionConfig>();
        var registeredDevices = new Dictionary<string, SmartHomeDeviceInfo>();
        var toolGAgents = new List<GrainId>();

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

            // Create a dedicated GAgent instance for this specific device
            var deviceGAgentId = Guid.NewGuid();
            var deviceAgent = await CreateAndInitializeDeviceAgentAsync(device.DeviceType, deviceGAgentId, config);
            
            if (deviceAgent != null)
            {
                deviceInstances[device.DeviceId] = deviceGAgentId;
                deviceConfigs[device.DeviceId] = config;
                registeredDevices[device.DeviceId] = device with { IsOnline = true };

                // Register this device GAgent for event communication
                await RegisterAsync(deviceAgent);
                
                // Add to tool GAgents list for AI
                toolGAgents.Add(deviceAgent.GetGrainId());
                
                Logger.LogInformation("Pre-created and registered device GAgent: {DeviceId} ({DeviceName}) -> {GAgentId}", 
                    device.DeviceId, device.Name, deviceGAgentId);
            }
            else
            {
                registeredDevices[device.DeviceId] = device with { IsOnline = false };
                Logger.LogWarning("Failed to create device GAgent for: {DeviceId} ({DeviceName})", 
                    device.DeviceId, device.Name);
            }
        }

        // Initialize AI capabilities with device GAgents as tools
        var initDto = new InitializeDto
        {
            LLMConfig = configuration.LLMConfig,
            Instructions = GetSmartHomeInstructions(),
            StreamingModeEnabled = false,
            StreamingConfig = new StreamingConfig(),
            ToolGAgents = toolGAgents // Add device GAgent instances as AI tools
        };

        await InitializeAsync(initDto);

        // Batch update state
        RaiseEvent(new DevicesInitializedLogEvent
        {
            DeviceInstances = deviceInstances,
            DeviceConfigs = deviceConfigs,
            RegisteredDevices = registeredDevices
        });

        await ConfirmEvents();
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

            // Create a dedicated GAgent instance for this specific device
            var deviceGAgentId = Guid.NewGuid();
            var deviceAgent = await CreateAndInitializeDeviceAgentAsync(deviceType, deviceGAgentId, config);
            
            if (deviceAgent != null)
            {
                var status = await deviceAgent.GetDeviceStatusSummaryAsync();
                deviceInfo = deviceInfo with { IsOnline = status?.IsConnected ?? false };

                // Register this device GAgent for event communication
                await RegisterAsync(deviceAgent);

                // Update state with new device instance
                RaiseEvent(new DeviceRegisteredLogEvent
                {
                    Device = deviceInfo,
                    Config = config
                });

                // Also update the device instance mapping
                var deviceInstances = new Dictionary<string, Guid>(State.DeviceGAgentInstances)
                {
                    [deviceId] = deviceGAgentId
                };

                RaiseEvent(new DevicesInitializedLogEvent
                {
                    DeviceInstances = deviceInstances,
                    DeviceConfigs = State.DeviceConfigs,
                    RegisteredDevices = State.RegisteredDevices
                });

                await ConfirmEvents();
                
                Logger.LogInformation("Successfully registered and created device GAgent: {DeviceId} ({DeviceName}) -> {GAgentId}", 
                    deviceId, deviceName, deviceGAgentId);
                return true;
            }
            else
            {
                Logger.LogWarning("Failed to create device GAgent for {DeviceId} ({DeviceName})", deviceId, deviceName);
                return false;
            }
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

            // Use AI to understand and execute the command with tool calling capability
            var context = new AIChatContextDto
            {
                RequestId = Guid.NewGuid(),
                MessageId = Guid.NewGuid().ToString(),
                ChatId = Guid.NewGuid().ToString()
            };

            var chatResponse = await ChatWithHistoryAndToolsAsync(
                $"Execute this smart home command: {command}", 
                null, 
                null, 
                default, 
                context);

            var result = new SmartHomeCommandResult
            {
                Command = command,
                Success = !string.IsNullOrEmpty(chatResponse?.Response),
                Response = chatResponse?.Response ?? "Failed to process command",
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

        // Try to get real-time status for registered devices using existing GAgent instances
        var deviceStatuses = new List<SmartHomeDeviceStatus>();
        foreach (var device in State.RegisteredDevices.Values)
        {
            try
            {
                var deviceAgent = await GetExistingDeviceAgentAsync(device.DeviceId);
                if (deviceAgent != null)
                {
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
                else
                {
                    // Add offline status if GAgent not available
                    deviceStatuses.Add(new SmartHomeDeviceStatus
                    {
                        DeviceId = device.DeviceId,
                        DeviceName = device.Name,
                        DeviceType = device.DeviceType,
                        IsOnline = false,
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

    private async Task<IDeviceGAgent<HttpDeviceConnection>?> CreateAndInitializeDeviceAgentAsync(
        string deviceType, 
        Guid agentId, 
        DeviceConnectionConfig config)
    {
        try
        {
            IDeviceGAgent<HttpDeviceConnection>? deviceAgent = deviceType.ToLower() switch
            {
                "smart-light" => await GAgentFactory.GetGAgentAsync<IHttpSmartLightGAgent>(agentId, config) as IDeviceGAgent<HttpDeviceConnection>,
                "smart-switch" => await GAgentFactory.GetGAgentAsync<IHttpSmartSwitchGAgent>(agentId, config) as IDeviceGAgent<HttpDeviceConnection>,
                "temperature-sensor" => await GAgentFactory.GetGAgentAsync<IHttpTemperatureSensorGAgent>(agentId, config) as IDeviceGAgent<HttpDeviceConnection>,
                _ => null
            };

            if (deviceAgent != null)
            {
                // The device GAgent will automatically initialize its connection using the provided config
                Logger.LogInformation("Created device GAgent {DeviceType} for {DeviceId} with GAgent ID {AgentId}", 
                    deviceType, config.DeviceId, agentId);
            }

            return deviceAgent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create and initialize device agent for type {DeviceType}, device {DeviceId}", 
                deviceType, config.DeviceId);
            return null;
        }
    }

    private async Task<IDeviceGAgent<HttpDeviceConnection>?> GetExistingDeviceAgentAsync(string deviceId)
    {
        try
        {
            if (!State.DeviceGAgentInstances.TryGetValue(deviceId, out var agentId))
            {
                Logger.LogWarning("No GAgent instance found for device {DeviceId}", deviceId);
                return null;
            }

            if (!State.RegisteredDevices.TryGetValue(deviceId, out var deviceInfo))
            {
                Logger.LogWarning("No device info found for device {DeviceId}", deviceId);
                return null;
            }

            return deviceInfo.DeviceType.ToLower() switch
            {
                "smart-light" => await GAgentFactory.GetGAgentAsync<IHttpSmartLightGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                "smart-switch" => await GAgentFactory.GetGAgentAsync<IHttpSmartSwitchGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                "temperature-sensor" => await GAgentFactory.GetGAgentAsync<IHttpTemperatureSensorGAgent>(agentId) as IDeviceGAgent<HttpDeviceConnection>,
                _ => null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get existing device agent for {DeviceId}", deviceId);
            return null;
        }
    }

    private static string GetSmartHomeInstructions()
    {
        return @"You are a Smart Home AI Assistant that controls smart home devices through function calls.

AVAILABLE DEVICES:
- light001: 客厅主灯 (Smart Light)
- light002: 卧室台灯 (Smart Light)  
- temp001: 客厅温度传感器 (Temperature Sensor)
- temp002: 卧室温度传感器 (Temperature Sensor)
- switch001: 客厅总开关 (Smart Switch)
- switch002: 厨房插座 (Smart Switch)

AVAILABLE DEVICE CONTROL FUNCTIONS:
You have access to the following Kernel functions to control smart home devices:

SMART LIGHT FUNCTIONS (from HttpSmartLightGAgent):
- TurnOnLightEvent(DeviceId, DeviceName): Turn on smart lights
- TurnOffLightEvent(DeviceId, DeviceName): Turn off smart lights  
- SetLightBrightnessEvent(DeviceId, DeviceName, Brightness): Set brightness (0-100%)
- SetLightColorEvent(DeviceId, DeviceName, Color): Set color (RGB hex like #FF0000)
- SetLightColorTemperatureEvent(DeviceId, DeviceName, Temperature): Set color temperature (1000-10000K)
- ToggleLightEvent(DeviceId, DeviceName): Toggle light on/off
- GetLightStatusEvent(DeviceId, DeviceName): Get light status

SMART SWITCH FUNCTIONS (from HttpSmartSwitchGAgent):
- TurnOnSwitchEvent(DeviceId, DeviceName): Turn on switches
- TurnOffSwitchEvent(DeviceId, DeviceName): Turn off switches
- ToggleSwitchEvent(DeviceId, DeviceName): Toggle switch on/off
- GetSwitchStatusEvent(DeviceId, DeviceName): Get switch status
- GetCurrentLoadEvent(DeviceId, DeviceName): Get electrical load
- GetPowerConsumptionEvent(DeviceId, DeviceName): Get power consumption
- GetDailyUsageEvent(DeviceId, DeviceName): Get daily energy usage
- ResetDailyUsageEvent(DeviceId, DeviceName): Reset daily usage counter

TEMPERATURE SENSOR FUNCTIONS (from HttpTemperatureSensorGAgent):
- GetTemperatureEvent(DeviceId, DeviceName): Get temperature reading
- GetHumidityEvent(DeviceId, DeviceName): Get humidity reading
- GetSensorStatusEvent(DeviceId, DeviceName): Get complete sensor status
- GetBatteryLevelEvent(DeviceId, DeviceName): Get battery level
- GetTemperatureRangeEvent(DeviceId, DeviceName): Get temperature range
- CalibrateSensorEvent(DeviceId, DeviceName): Calibrate sensor

DEVICE NAME MAPPING:
- ""客厅主灯"" / ""living room light"" → light001
- ""卧室台灯"" / ""bedroom lamp"" → light002
- ""客厅温度"" / ""living room temperature"" → temp001
- ""卧室温度"" / ""bedroom temperature"" → temp002
- ""客厅开关"" / ""living room switch"" → switch001
- ""厨房插座"" / ""kitchen outlet"" → switch002

COMMAND EXECUTION EXAMPLES:
User: ""打开客厅主灯""
→ Call: TurnOnLightEvent(DeviceId=""light001"", DeviceName=""客厅主灯"")

User: ""把卧室台灯调到50%亮度""
→ Call: SetLightBrightnessEvent(DeviceId=""light002"", DeviceName=""卧室台灯"", Brightness=50)

User: ""将客厅灯设为红色""
→ Call: SetLightColorEvent(DeviceId=""light001"", DeviceName=""客厅主灯"", Color=""#FF0000"")

User: ""查看客厅温度""
→ Call: GetTemperatureEvent(DeviceId=""temp001"", DeviceName=""客厅温度传感器"")

User: ""关闭所有灯""
→ Call: TurnOffLightEvent(DeviceId=""light001"", DeviceName=""客厅主灯"")
→ Call: TurnOffLightEvent(DeviceId=""light002"", DeviceName=""卧室台灯"")

User: ""打开客厅总开关""
→ Call: TurnOnSwitchEvent(DeviceId=""switch001"", DeviceName=""客厅总开关"")

FUNCTION CALLING GUIDELINES:
1. Always use the available Event functions (TurnOnLightEvent, SetLightBrightnessEvent, etc.) to control devices
2. Include the correct DeviceId and DeviceName parameters
3. For ""all lights"" commands, call Event functions for both light001 and light002
4. For brightness, use values 0-100
5. For colors, use RGB hex format (#FF0000, #00FF00, #0000FF, etc.)
6. For temperature sensors, use temp001 for 客厅, temp002 for 卧室
7. Always provide helpful feedback about what Event functions were called

RESPONSE FORMAT:
- Acknowledge the command received
- Describe which specific Event functions were called
- Report success/failure status
- Provide current device status if relevant

Remember: You must use the available Event functions (TurnOnLightEvent, SetLightBrightnessEvent, etc.) to perform device control operations.";
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
                var deviceAgent = await GetExistingDeviceAgentAsync(device.DeviceId);
                if (deviceAgent != null)
                {
                    var status = await deviceAgent.GetDeviceStatusSummaryAsync();
                    var updatedDevice = device with { IsOnline = status?.IsConnected ?? false };
                    
                    if (updatedDevice.IsOnline != device.IsOnline)
                    {
                        RaiseEvent(new DeviceRegisteredLogEvent
                        {
                            Device = updatedDevice,
                            Config = State.DeviceConfigs[device.DeviceId]
                        });
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


    #region GroupMemberGAgentBase Required Methods

    protected override async Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // Return interest score based on smart home capabilities
        var onlineDevices = State.RegisteredDevices.Values.Count(d => d.IsOnline);
        // Higher score if more devices are online and available
        return onlineDevices * 10; // Score based on available devices
    }

    protected override async Task<GroupChatResponse> ChatAsync(Guid blackboardId, List<GroupChatMessage>? coordinatorMessages)
    {
        // Handle chat requests from workflow coordinator
        var lastMessage = coordinatorMessages?.LastOrDefault()?.Content ?? "";
        
        if (string.IsNullOrEmpty(lastMessage))
        {
            return new GroupChatResponse
            {
                Content = "Smart Home AI ready. Please provide a command to control devices.",
                Continue = true
            };
        }

        // Execute the command using AI with tool calling
        var result = await ExecuteNaturalLanguageCommandAsync(lastMessage);
        
        return new GroupChatResponse
        {
            Content = result.Response,
            Continue = true
        };
    }

    protected override void GroupMemberTransitionState(SmartHomeAIGAgentState state, StateLogEventBase<SmartHomeAIGAgentStateLogEvent> @event)
    {
        // Handle smart home specific state transitions
        switch (@event)
        {
            case DevicesInitializedLogEvent devicesInitialized:
                state.DeviceGAgentInstances = devicesInitialized.DeviceInstances;
                state.DeviceConfigs = devicesInitialized.DeviceConfigs;
                state.RegisteredDevices = devicesInitialized.RegisteredDevices;
                break;

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
                state.DeviceGAgentInstances.Remove(deviceUnregistered.DeviceId);
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

    #endregion
}
