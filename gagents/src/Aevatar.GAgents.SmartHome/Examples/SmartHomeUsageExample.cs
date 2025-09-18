using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.SmartHome.Events;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.SmartHome.Examples;

/// <summary>
/// Smart Home AI GAgent Usage Examples
/// This class demonstrates how to use the Smart Home AI GAgent in various scenarios
/// </summary>
public class SmartHomeUsageExample
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<SmartHomeUsageExample> _logger;

    public SmartHomeUsageExample(IGAgentFactory gAgentFactory, ILogger<SmartHomeUsageExample> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Basic setup and device registration
    /// </summary>
    public async Task Example1_BasicSetupAsync()
    {
        _logger.LogInformation("=== Example 1: Basic Setup and Device Registration ===");

        // Step 1: Create configuration
        var config = new SmartHomeAIGAgentConfiguration
        {
            DeviceHubApiUrl = "http://localhost:9001",
            DeviceHubApiKey = "your-api-key-here",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "gpt-4" // Use your configured LLM
            },
            PreConfiguredDevices = new List<SmartHomeDeviceInfo>
            {
                new() { DeviceId = "light001", Name = "客厅主灯", DeviceType = "smart-light" },
                new() { DeviceId = "light002", Name = "卧室台灯", DeviceType = "smart-light" },
                new() { DeviceId = "temp001", Name = "客厅温度传感器", DeviceType = "temperature-sensor" },
                new() { DeviceId = "switch001", Name = "客厅总开关", DeviceType = "smart-switch" }
            }
        };

        // Step 2: Create Smart Home AI GAgent instance
        var smartHomeAI = await _gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(
            Guid.NewGuid(), 
            config
        );

        // Step 3: Verify devices are registered
        var devices = await smartHomeAI.GetRegisteredDevicesAsync();
        _logger.LogInformation($"Registered {devices.Count} devices:");
        foreach (var device in devices)
        {
            _logger.LogInformation($"  - {device.Name} ({device.DeviceId}): {device.DeviceType}");
        }

        // Step 4: Register additional device dynamically
        var success = await smartHomeAI.RegisterDeviceAsync("temp002", "卧室温度传感器", "temperature-sensor");
        _logger.LogInformation($"Dynamic device registration: {(success ? "Success" : "Failed")}");
    }

    /// <summary>
    /// Example 2: Natural language command execution
    /// </summary>
    public async Task Example2_NaturalLanguageCommandsAsync()
    {
        _logger.LogInformation("=== Example 2: Natural Language Commands ===");

        var smartHomeAI = await GetSmartHomeAIInstanceAsync();

        // Various natural language commands
        var commands = new[]
        {
            "打开客厅主灯",
            "把卧室台灯调到50%亮度",
            "将客厅灯设为红色",
            "查看客厅温度",
            "关闭所有灯",
            "打开客厅总开关",
            "检查所有传感器的电池电量"
        };

        foreach (var command in commands)
        {
            _logger.LogInformation($"Executing command: {command}");
            
            var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
            
            _logger.LogInformation($"Result: {(result.Success ? "✅" : "❌")} {result.Response}");
            
            // Add delay between commands
            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Example 3: Event-driven control
    /// </summary>
    public async Task Example3_EventDrivenControlAsync()
    {
        _logger.LogInformation("=== Example 3: Event-Driven Control ===");

        var smartHomeAI = await GetSmartHomeAIInstanceAsync();

        // Example 3.1: Send command via event
        var publishingGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
        await publishingGAgent.PublishEventAsync(new SendSmartHomeCommandEvent
        {
            Command = "把所有灯调成暖白色",
            Context = "用户在客厅准备休息"
        }, smartHomeAI);

        await Task.Delay(2000);

        // Example 3.2: Register device via event
        await publishingGAgent.PublishEventAsync(new RegisterSmartHomeDeviceEvent
        {
            DeviceId = "light003",
            DeviceName = "阳台灯",
            DeviceType = "smart-light"
        });

        await Task.Delay(1000);

        // Example 3.3: Get status via event
        await publishingGAgent.PublishEventAsync(new GetSmartHomeStatusEvent());

        _logger.LogInformation("Event-driven commands sent. Check the logs for responses.");
    }

    /// <summary>
    /// Example 4: Home status monitoring
    /// </summary>
    public async Task Example4_HomeStatusMonitoringAsync()
    {
        _logger.LogInformation("=== Example 4: Home Status Monitoring ===");

        var smartHomeAI = await GetSmartHomeAIInstanceAsync();

        // Get comprehensive home status
        var overview = await smartHomeAI.GetHomeStatusOverviewAsync();

        _logger.LogInformation("=== Home Status Overview ===");
        _logger.LogInformation($"Total Devices: {overview.TotalDevices}");
        _logger.LogInformation($"Online Devices: {overview.OnlineDevices}");
        _logger.LogInformation($"Offline Devices: {overview.OfflineDevices}");

        _logger.LogInformation("Devices by Type:");
        foreach (var deviceType in overview.DevicesByType)
        {
            _logger.LogInformation($"  - {deviceType.Key}: {deviceType.Value}");
        }

        _logger.LogInformation("Individual Device Status:");
        foreach (var deviceStatus in overview.DeviceStatuses)
        {
            var status = deviceStatus.IsOnline ? "🟢 Online" : "🔴 Offline";
            _logger.LogInformation($"  - {deviceStatus.DeviceName}: {status}");
        }

        _logger.LogInformation($"Last Updated: {overview.LastUpdated:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// Example 5: Advanced scenarios and error handling
    /// </summary>
    public async Task Example5_AdvancedScenariosAsync()
    {
        _logger.LogInformation("=== Example 5: Advanced Scenarios ===");

        var smartHomeAI = await GetSmartHomeAIInstanceAsync();

        // Scenario 1: Batch operations
        _logger.LogInformation("Scenario 1: Batch Operations");
        var batchCommands = new[]
        {
            "打开所有灯",
            "把所有灯调到80%亮度",
            "设置客厅为电影模式", // This might require custom logic
            "检查所有设备状态"
        };

        foreach (var command in batchCommands)
        {
            var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
            _logger.LogInformation($"Batch Command '{command}': {(result.Success ? "✅" : "❌")}");
        }

        // Scenario 2: Error handling
        _logger.LogInformation("\nScenario 2: Error Handling");
        var errorCommands = new[]
        {
            "", // Empty command
            "打开不存在的设备", // Non-existent device
            "设置灯的亮度为150%", // Invalid parameter
        };

        foreach (var command in errorCommands)
        {
            var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
            _logger.LogInformation($"Error Test '{command}': {result.Response}");
        }

        // Scenario 3: Device management
        _logger.LogInformation("\nScenario 3: Device Management");
        
        // Add a device
        var addResult = await smartHomeAI.RegisterDeviceAsync("switch002", "厨房插座", "smart-switch");
        _logger.LogInformation($"Add device: {(addResult ? "✅" : "❌")}");

        // List all devices
        var devices = await smartHomeAI.GetRegisteredDevicesAsync();
        _logger.LogInformation($"Total devices after addition: {devices.Count}");

        // Remove a device
        var removeResult = await smartHomeAI.UnregisterDeviceAsync("switch002");
        _logger.LogInformation($"Remove device: {(removeResult ? "✅" : "❌")}");
    }

    /// <summary>
    /// Example 6: Scene control and automation
    /// </summary>
    public async Task Example6_SceneControlAsync()
    {
        _logger.LogInformation("=== Example 6: Scene Control and Automation ===");

        var smartHomeAI = await GetSmartHomeAIInstanceAsync();

        // Define scenes through natural language
        var scenes = new Dictionary<string, string[]>
        {
            ["早安模式"] = new[]
            {
                "打开所有灯",
                "把客厅灯调到100%亮度",
                "将卧室灯调到70%亮度",
                "打开客厅总开关"
            },
            ["睡眠模式"] = new[]
            {
                "关闭所有灯",
                "关闭客厅总开关",
                "只保留卧室台灯，调到10%亮度"
            },
            ["电影模式"] = new[]
            {
                "关闭客厅主灯",
                "把卧室台灯调到30%亮度",
                "将客厅灯设为暖白色"
            }
        };

        foreach (var scene in scenes)
        {
            _logger.LogInformation($"\nExecuting Scene: {scene.Key}");
            
            foreach (var command in scene.Value)
            {
                var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
                _logger.LogInformation($"  Command '{command}': {(result.Success ? "✅" : "❌")}");
                
                // Small delay between commands in a scene
                await Task.Delay(500);
            }
            
            _logger.LogInformation($"Scene '{scene.Key}' completed");
        }
    }

    /// <summary>
    /// Helper method to create a Smart Home AI GAgent instance
    /// </summary>
    private async Task<ISmartHomeAIGAgent> GetSmartHomeAIInstanceAsync()
    {
        var config = new SmartHomeAIGAgentConfiguration
        {
            DeviceHubApiUrl = "http://localhost:9001",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "gpt-4"
            },
            PreConfiguredDevices = new List<SmartHomeDeviceInfo>
            {
                new() { DeviceId = "light001", Name = "客厅主灯", DeviceType = "smart-light" },
                new() { DeviceId = "light002", Name = "卧室台灯", DeviceType = "smart-light" },
                new() { DeviceId = "temp001", Name = "客厅温度传感器", DeviceType = "temperature-sensor" },
                new() { DeviceId = "temp002", Name = "卧室温度传感器", DeviceType = "temperature-sensor" },
                new() { DeviceId = "switch001", Name = "客厅总开关", DeviceType = "smart-switch" },
                new() { DeviceId = "switch002", Name = "厨房插座", DeviceType = "smart-switch" }
            }
        };

        return await _gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(Guid.NewGuid(), config);
    }

    /// <summary>
    /// Run all examples in sequence
    /// </summary>
    public async Task RunAllExamplesAsync()
    {
        _logger.LogInformation("🏠 Starting Smart Home AI GAgent Examples");
        
        try
        {
            await Example1_BasicSetupAsync();
            await Task.Delay(2000);
            
            await Example2_NaturalLanguageCommandsAsync();
            await Task.Delay(2000);
            
            await Example3_EventDrivenControlAsync();
            await Task.Delay(2000);
            
            await Example4_HomeStatusMonitoringAsync();
            await Task.Delay(2000);
            
            await Example5_AdvancedScenariosAsync();
            await Task.Delay(2000);
            
            await Example6_SceneControlAsync();
            
            _logger.LogInformation("✅ All Smart Home AI GAgent examples completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error running Smart Home AI GAgent examples");
        }
    }
}

/// <summary>
/// Configuration helper for different deployment scenarios
/// </summary>
public static class SmartHomeConfigurationHelper
{
    /// <summary>
    /// Create configuration for development environment
    /// </summary>
    public static SmartHomeAIGAgentConfiguration CreateDevelopmentConfig()
    {
        return new SmartHomeAIGAgentConfiguration
        {
            DeviceHubApiUrl = "http://localhost:9001",
            DeviceHubApiKey = null, // No API key for local development
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "gpt-4"
            },
            PreConfiguredDevices = GetDevelopmentDevices()
        };
    }

    /// <summary>
    /// Create configuration for production environment
    /// </summary>
    public static SmartHomeAIGAgentConfiguration CreateProductionConfig(string apiUrl, string apiKey)
    {
        return new SmartHomeAIGAgentConfiguration
        {
            DeviceHubApiUrl = apiUrl,
            DeviceHubApiKey = apiKey,
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "gpt-4"
            },
            PreConfiguredDevices = GetProductionDevices()
        };
    }

    /// <summary>
    /// Get development device list
    /// </summary>
    private static List<SmartHomeDeviceInfo> GetDevelopmentDevices()
    {
        return new List<SmartHomeDeviceInfo>
        {
            new() { DeviceId = "light001", Name = "客厅主灯", DeviceType = "smart-light" },
            new() { DeviceId = "light002", Name = "卧室台灯", DeviceType = "smart-light" },
            new() { DeviceId = "temp001", Name = "客厅温度传感器", DeviceType = "temperature-sensor" },
            new() { DeviceId = "temp002", Name = "卧室温度传感器", DeviceType = "temperature-sensor" },
            new() { DeviceId = "switch001", Name = "客厅总开关", DeviceType = "smart-switch" },
            new() { DeviceId = "switch002", Name = "厨房插座", DeviceType = "smart-switch" }
        };
    }

    /// <summary>
    /// Get production device list (customize based on actual deployment)
    /// </summary>
    private static List<SmartHomeDeviceInfo> GetProductionDevices()
    {
        // In production, you might load this from configuration or database
        return GetDevelopmentDevices();
    }
}
