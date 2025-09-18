using System;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.SmartHome.Events;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.SmartHome;

/// <summary>
/// Smart Home Demo Helper - 简化demo演示的工具类
/// </summary>
public class SmartHomeDemoHelper
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<SmartHomeDemoHelper> _logger;
    private ISmartHomeAIGAgent? _smartHomeAI;

    public SmartHomeDemoHelper(IGAgentFactory gAgentFactory, ILogger<SmartHomeDemoHelper> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// 初始化智能家居AI助手 (使用hardcoded设备)
    /// </summary>
    /// <param name="systemLLM">使用的LLM系统，默认为OpenAI</param>
    /// <returns>SmartHomeAIGAgent实例</returns>
    public async Task<ISmartHomeAIGAgent> InitializeSmartHomeAIAsync(string systemLLM = "OpenAI")
    {
        _logger.LogInformation("🏠 初始化智能家居AI助手 (Demo模式)");

        // 使用hardcoded配置创建SmartHomeAIGAgent
        var config = SmartHomeAIGAgentConfiguration.CreateDemoConfig(systemLLM);
        
        _smartHomeAI = await _gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(
            Guid.NewGuid(), 
            config
        );

        _logger.LogInformation("✅ 智能家居AI助手初始化完成");
        
        // 显示已注册的设备
        var devices = await _smartHomeAI.GetRegisteredDevicesAsync();
        _logger.LogInformation("📱 已注册设备 ({Count}个):", devices.Count);
        foreach (var device in devices)
        {
            var statusIcon = device.IsOnline ? "🟢" : "🔴";
            _logger.LogInformation("   {Icon} {Name} ({DeviceId}) - {DeviceType}", 
                statusIcon, device.Name, device.DeviceId, device.DeviceType);
        }

        return _smartHomeAI;
    }

    /// <summary>
    /// 执行demo命令
    /// </summary>
    /// <param name="command">自然语言命令</param>
    /// <returns>执行结果</returns>
    public async Task<SmartHomeCommandResult> ExecuteDemoCommandAsync(string command)
    {
        if (_smartHomeAI == null)
        {
            throw new InvalidOperationException("请先调用 InitializeSmartHomeAIAsync() 初始化AI助手");
        }

        _logger.LogInformation("🗣️ 执行命令: {Command}", command);
        
        var result = await _smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
        
        var resultIcon = result.Success ? "✅" : "❌";
        _logger.LogInformation("{Icon} 命令结果: {Response}", resultIcon, result.Response);
        
        return result;
    }

    /// <summary>
    /// 运行预设的demo场景
    /// </summary>
    public async Task RunDemoScenariosAsync()
    {
        if (_smartHomeAI == null)
        {
            throw new InvalidOperationException("请先调用 InitializeSmartHomeAIAsync() 初始化AI助手");
        }

        _logger.LogInformation("🎬 开始运行智能家居Demo场景");

        var demoCommands = new[]
        {
            "打开客厅主灯",
            "把卧室台灯调到50%亮度",
            "将客厅灯设为红色",
            "查看客厅温度",
            "打开客厅总开关",
            "检查所有传感器状态",
            "把所有灯调成暖白色",
            "关闭所有灯"
        };

        foreach (var command in demoCommands)
        {
            await ExecuteDemoCommandAsync(command);
            await Task.Delay(2000); // 每个命令间隔2秒
        }

        _logger.LogInformation("🎉 Demo场景运行完成");
    }

    /// <summary>
    /// 获取家居状态概览
    /// </summary>
    public async Task ShowHomeStatusAsync()
    {
        if (_smartHomeAI == null)
        {
            throw new InvalidOperationException("请先调用 InitializeSmartHomeAIAsync() 初始化AI助手");
        }

        var overview = await _smartHomeAI.GetHomeStatusOverviewAsync();
        
        _logger.LogInformation("🏠 智能家居状态概览:");
        _logger.LogInformation("   📊 总设备: {Total}个", overview.TotalDevices);
        _logger.LogInformation("   🟢 在线: {Online}个", overview.OnlineDevices);
        _logger.LogInformation("   🔴 离线: {Offline}个", overview.OfflineDevices);
        
        _logger.LogInformation("   📋 设备类型统计:");
        foreach (var deviceType in overview.DevicesByType)
        {
            _logger.LogInformation("      - {Type}: {Count}个", deviceType.Key, deviceType.Value);
        }

        _logger.LogInformation("   📱 设备详细状态:");
        foreach (var device in overview.DeviceStatuses)
        {
            var statusIcon = device.IsOnline ? "🟢" : "🔴";
            _logger.LogInformation("      {Icon} {Name}: {Type} - 更新时间: {LastUpdated:HH:mm:ss}", 
                statusIcon, device.DeviceName, device.DeviceType, device.LastUpdated);
        }
    }

    /// <summary>
    /// 运行完整的demo展示
    /// </summary>
    /// <param name="systemLLM">使用的LLM系统</param>
    public async Task RunCompleteDemo(string systemLLM = "OpenAI")
    {
        try
        {
            _logger.LogInformation("🚀 开始智能家居完整Demo展示");

            // 1. 初始化
            await InitializeSmartHomeAIAsync(systemLLM);
            await Task.Delay(1000);

            // 2. 显示初始状态
            await ShowHomeStatusAsync();
            await Task.Delay(2000);

            // 3. 运行demo场景
            await RunDemoScenariosAsync();
            await Task.Delay(1000);

            // 4. 显示最终状态
            _logger.LogInformation("📊 最终状态检查:");
            await ShowHomeStatusAsync();

            _logger.LogInformation("🎉 智能家居Demo展示完成!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Demo运行失败");
            throw;
        }
    }
}

/// <summary>
/// Demo快速启动扩展方法
/// </summary>
public static class SmartHomeDemoExtensions
{
    /// <summary>
    /// 快速创建SmartHomeAIGAgent用于demo
    /// </summary>
    public static async Task<ISmartHomeAIGAgent> CreateSmartHomeDemoAsync(
        this IGAgentFactory gAgentFactory, 
        string systemLLM = "OpenAI")
    {
        var config = SmartHomeAIGAgentConfiguration.CreateDemoConfig(systemLLM);
        return await gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(Guid.NewGuid(), config);
    }

    /// <summary>
    /// 快速执行智能家居命令 (demo用)
    /// </summary>
    public static async Task<string> ExecuteSmartHomeCommandAsync(
        this ISmartHomeAIGAgent smartHomeAI,
        string command)
    {
        var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
        return $"{(result.Success ? "✅" : "❌")} {result.Response}";
    }
}
