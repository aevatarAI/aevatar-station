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
/// 智能灯泡GAgent接口
/// </summary>
public interface ISmartLightGAgent : IDeviceGAgent<VirtualDeviceConnection>
{
    /// <summary>
    /// 开灯
    /// </summary>
    /// <returns>操作是否成功</returns>
    Task<bool> TurnOnAsync();
    
    /// <summary>
    /// 关灯
    /// </summary>
    /// <returns>操作是否成功</returns>
    Task<bool> TurnOffAsync();
    
    /// <summary>
    /// 设置亮度
    /// </summary>
    /// <param name="brightness">亮度百分比 (0-100)</param>
    /// <returns>操作是否成功</returns>
    Task<bool> SetBrightnessAsync(int brightness);
    
    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="color">颜色值 (RGB十六进制，如 #FF0000)</param>
    /// <returns>操作是否成功</returns>
    Task<bool> SetColorAsync(string color);
    
    /// <summary>
    /// 获取当前灯泡状态
    /// </summary>
    /// <returns>灯泡状态信息</returns>
    Task<SmartLightStatus> GetLightStatusAsync();
    
    /// <summary>
    /// 切换灯泡开关状态
    /// </summary>
    /// <returns>操作是否成功</returns>
    Task<bool> ToggleAsync();
}

/// <summary>
/// 智能灯泡状态信息
/// </summary>
[GenerateSerializer]
public class SmartLightStatus
{
    /// <summary>
    /// 是否开启
    /// </summary>
    [Id(0)] public bool IsOn { get; set; }
    
    /// <summary>
    /// 亮度百分比 (0-100)
    /// </summary>
    [Id(1)] public int Brightness { get; set; }
    
    /// <summary>
    /// 颜色值 (RGB十六进制)
    /// </summary>
    [Id(2)] public string Color { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// 功耗 (瓦特)
    /// </summary>
    [Id(3)] public double PowerConsumption { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    [Id(4)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 智能灯泡GAgent实现
/// </summary>
[Description("智能灯泡设备代理，提供开关、亮度调节、颜色设置等功能，支持通过AI助手进行智能控制")]
[GAgent("smart-light", "device")]
public class SmartLightGAgent : DeviceGAgentBase<VirtualDeviceConnection>, ISmartLightGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        var deviceName = DeviceConnection?.DeviceName ?? "智能灯泡";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        return $"智能灯泡设备代理 ({deviceName}) - 当前状态: {GetStatusDescription(status)}。" +
               $"支持开关控制、亮度调节(0-100%)、颜色设置(RGB)等功能，可通过语音或AI助手进行智能控制。";
    }

    protected override async Task<VirtualDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
        
        return new VirtualDeviceConnection(
            config.DeviceId,
            config.DeviceName,
            "SmartLight", // 固定为智能灯泡类型
            logger
        );
    }

    public async Task<bool> TurnOnAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法开灯");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("TurnOn");
            
            Logger.LogInformation("智能灯泡开灯操作: {Success}, 结果: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开灯操作失败");
            return false;
        }
    }

    public async Task<bool> TurnOffAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法关灯");
                return false;
            }

            var result = await DeviceConnection.ExecuteActionAsync("TurnOff");
            
            Logger.LogInformation("智能灯泡关灯操作: {Success}, 结果: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "关灯操作失败");
            return false;
        }
    }

    public async Task<bool> SetBrightnessAsync(int brightness)
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法设置亮度");
                return false;
            }

            if (brightness < 0 || brightness > 100)
            {
                Logger.LogWarning("亮度值超出范围: {Brightness}, 有效范围: 0-100", brightness);
                return false;
            }

            var parameters = new Dictionary<string, object>
            {
                ["brightness"] = brightness
            };

            var result = await DeviceConnection.ExecuteActionAsync("SetBrightness", parameters);
            
            Logger.LogInformation("智能灯泡设置亮度操作: {Success}, 亮度: {Brightness}%, 结果: {Result}", 
                result.IsSuccess, brightness, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "设置亮度操作失败: {Brightness}", brightness);
            return false;
        }
    }

    public async Task<bool> SetColorAsync(string color)
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法设置颜色");
                return false;
            }

            if (string.IsNullOrWhiteSpace(color))
            {
                Logger.LogWarning("颜色值不能为空");
                return false;
            }

            // 验证颜色格式（简单验证）
            if (!color.StartsWith("#") || color.Length != 7)
            {
                Logger.LogWarning("颜色格式无效: {Color}, 应为RGB十六进制格式如 #FF0000", color);
                return false;
            }

            var success = await DeviceConnection.WritePropertyAsync("Color", color);
            
            Logger.LogInformation("智能灯泡设置颜色操作: {Success}, 颜色: {Color}", success, color);
            
            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "设置颜色操作失败: {Color}", color);
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

            // 读取设备属性
            var power = await DeviceConnection.ReadPropertyAsync("Power");
            var brightness = await DeviceConnection.ReadPropertyAsync("Brightness");
            var color = await DeviceConnection.ReadPropertyAsync("Color");

            var isOn = power is bool b && b;
            var brightnessValue = brightness is int i ? i : 0;
            var colorValue = color?.ToString() ?? "#FFFFFF";

            // 计算功耗（简单估算）
            var powerConsumption = isOn ? (brightnessValue / 100.0 * 10.0) : 0.0; // 最大10瓦

            var status = new SmartLightStatus
            {
                IsOn = isOn,
                Brightness = brightnessValue,
                Color = colorValue,
                PowerConsumption = Math.Round(powerConsumption, 2),
                LastUpdated = DateTime.UtcNow
            };

            Logger.LogDebug("获取智能灯泡状态: 开关={IsOn}, 亮度={Brightness}%, 颜色={Color}, 功耗={Power}W", 
                status.IsOn, status.Brightness, status.Color, status.PowerConsumption);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取灯泡状态失败");
            
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
                Logger.LogInformation("当前灯泡开启，执行关灯操作");
                return await TurnOffAsync();
            }
            else
            {
                Logger.LogInformation("当前灯泡关闭，执行开灯操作");
                return await TurnOnAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "切换灯泡状态失败");
            return false;
        }
    }

    private static string GetStatusDescription(DeviceConnectionStatus status)
    {
        return status switch
        {
            DeviceConnectionStatus.Connected => "已连接",
            DeviceConnectionStatus.Connecting => "连接中",
            DeviceConnectionStatus.Disconnected => "未连接",
            DeviceConnectionStatus.Error => "连接错误",
            DeviceConnectionStatus.Reconnecting => "重连中",
            _ => "未知状态"
        };
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        Logger.LogInformation("智能灯泡GAgent已激活: {GrainId}", this.GetGrainId());
        
        // 如果有连接配置且设备未连接，尝试自动连接
        if (State.ConnectionConfig != null && !State.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000, cancellationToken); // 延迟2秒后连接
                try
                {
                    await InitializeDeviceConnectionAsync(State.ConnectionConfig);
                    
                    // 启用设备监控
                    await SetDeviceMonitoringAsync(true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "自动连接智能灯泡失败");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("智能灯泡GAgent正在停用: {GrainId}, 原因: {Reason}", this.GetGrainId(), reason);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
