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
/// 温度传感器GAgent接口
/// </summary>
public interface ITemperatureSensorGAgent : IDeviceGAgent<VirtualDeviceConnection>
{
    /// <summary>
    /// 获取当前温度
    /// </summary>
    /// <returns>温度值（摄氏度）</returns>
    Task<double> GetTemperatureAsync();
    
    /// <summary>
    /// 获取当前湿度
    /// </summary>
    /// <returns>湿度百分比</returns>
    Task<double> GetHumidityAsync();
    
    /// <summary>
    /// 获取温湿度读数
    /// </summary>
    /// <returns>温湿度数据</returns>
    Task<TemperatureHumidityReading> GetReadingAsync();
    
    /// <summary>
    /// 校准传感器
    /// </summary>
    /// <returns>校准是否成功</returns>
    Task<bool> CalibrateAsync();
    
    /// <summary>
    /// 获取历史读数
    /// </summary>
    /// <param name="hours">过去多少小时的数据</param>
    /// <returns>历史读数列表</returns>
    Task<List<TemperatureHumidityReading>> GetHistoryAsync(int hours = 24);
}

/// <summary>
/// 温湿度读数
/// </summary>
[GenerateSerializer]
public class TemperatureHumidityReading
{
    /// <summary>
    /// 温度（摄氏度）
    /// </summary>
    [Id(0)] public double Temperature { get; set; }
    
    /// <summary>
    /// 湿度百分比
    /// </summary>
    [Id(1)] public double Humidity { get; set; }
    
    /// <summary>
    /// 读取时间
    /// </summary>
    [Id(2)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 传感器状态
    /// </summary>
    [Id(3)] public string Status { get; set; } = "Normal";
    
    /// <summary>
    /// 温度等级描述
    /// </summary>
    public string TemperatureLevel
    {
        get
        {
            return Temperature switch
            {
                < 0 => "严寒",
                < 10 => "寒冷",
                < 20 => "凉爽",
                < 25 => "舒适",
                < 30 => "温暖",
                < 35 => "炎热",
                _ => "酷热"
            };
        }
    }
    
    /// <summary>
    /// 湿度等级描述
    /// </summary>
    public string HumidityLevel
    {
        get
        {
            return Humidity switch
            {
                < 30 => "干燥",
                < 40 => "较干",
                < 60 => "适中",
                < 70 => "较湿",
                _ => "潮湿"
            };
        }
    }
    
    /// <summary>
    /// 舒适度评级
    /// </summary>
    public string ComfortLevel
    {
        get
        {
            if (Temperature >= 20 && Temperature <= 26 && Humidity >= 40 && Humidity <= 60)
                return "舒适";
            else if (Temperature >= 18 && Temperature <= 28 && Humidity >= 30 && Humidity <= 70)
                return "较舒适";
            else
                return "不舒适";
        }
    }
}

/// <summary>
/// 温度传感器GAgent实现
/// </summary>
[Description("温度传感器设备代理，提供温度和湿度监测功能，支持历史数据查询和传感器校准，可通过AI助手进行环境监控")]
[GAgent("temperature-sensor", "device")]
public class TemperatureSensorGAgent : DeviceGAgentBase<VirtualDeviceConnection>, ITemperatureSensorGAgent
{
    private readonly List<TemperatureHumidityReading> _readings = new();
    private readonly object _readingsLock = new();

    public override async Task<string> GetDescriptionAsync()
    {
        var deviceName = DeviceConnection?.DeviceName ?? "温度传感器";
        var status = DeviceConnection?.Status ?? DeviceConnectionStatus.Disconnected;
        
        var description = $"温度传感器设备代理 ({deviceName}) - 当前状态: {GetStatusDescription(status)}。";
        
        if (DeviceConnection?.Status == DeviceConnectionStatus.Connected)
        {
            try
            {
                var reading = await GetReadingAsync();
                description += $" 当前环境: 温度 {reading.Temperature:F1}°C ({reading.TemperatureLevel})，" +
                              $"湿度 {reading.Humidity:F1}% ({reading.HumidityLevel})，" +
                              $"舒适度: {reading.ComfortLevel}";
            }
            catch
            {
                description += " 无法获取当前读数";
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
            "TemperatureSensor", // 固定为温度传感器类型
            logger
        );
        
        // 订阅属性变化事件来记录历史数据
        connection.PropertyChanged += OnSensorPropertyChanged;
        
        return connection;
    }

    public async Task<double> GetTemperatureAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法读取温度");
                return double.NaN;
            }

            var temperature = await DeviceConnection.ReadPropertyAsync("Temperature");
            
            if (temperature is double temp)
            {
                Logger.LogDebug("读取温度: {Temperature:F1}°C", temp);
                return temp;
            }
            
            Logger.LogWarning("温度数据类型错误: {Type}", temperature?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "读取温度失败");
            return double.NaN;
        }
    }

    public async Task<double> GetHumidityAsync()
    {
        try
        {
            if (DeviceConnection == null)
            {
                Logger.LogWarning("设备未连接，无法读取湿度");
                return double.NaN;
            }

            var humidity = await DeviceConnection.ReadPropertyAsync("Humidity");
            
            if (humidity is double hum)
            {
                Logger.LogDebug("读取湿度: {Humidity:F1}%", hum);
                return hum;
            }
            
            Logger.LogWarning("湿度数据类型错误: {Type}", humidity?.GetType().Name ?? "null");
            return double.NaN;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "读取湿度失败");
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
            
            Logger.LogInformation("获取传感器读数: 温度={Temperature:F1}°C ({TempLevel}), 湿度={Humidity:F1}% ({HumLevel}), 舒适度={Comfort}",
                reading.Temperature, reading.TemperatureLevel, reading.Humidity, reading.HumidityLevel, reading.ComfortLevel);
            
            return reading;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取传感器读数失败");
            
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
                Logger.LogWarning("设备未连接，无法校准");
                return false;
            }

            Logger.LogInformation("开始校准温度传感器...");
            
            var result = await DeviceConnection.ExecuteActionAsync("Calibrate");
            
            Logger.LogInformation("传感器校准操作: {Success}, 结果: {Result}", 
                result.IsSuccess, result.Result);
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "校准传感器失败");
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
                .Take(1000) // 最多返回1000条记录
                .ToList();
            
            Logger.LogDebug("获取历史读数: {Count}条记录，时间范围: {Hours}小时", history.Count, hours);
            
            return Task.FromResult(history);
        }
    }

    private void OnSensorPropertyChanged(object? sender, DevicePropertyChangedEventArgs e)
    {
        // 当温度或湿度变化时，记录到历史数据
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
                        
                        // 保持历史记录不超过10000条
                        if (_readings.Count > 10000)
                        {
                            _readings.RemoveRange(0, _readings.Count - 10000);
                        }
                    }
                    
                    // 检查是否需要发出警告
                    await CheckEnvironmentAlertsAsync(reading);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "记录传感器读数时发生错误");
                }
            });
        }
    }

    private async Task CheckEnvironmentAlertsAsync(TemperatureHumidityReading reading)
    {
        try
        {
            var alerts = new List<string>();
            
            // 温度警告
            if (reading.Temperature < 5)
                alerts.Add($"温度过低: {reading.Temperature:F1}°C，可能结冰");
            else if (reading.Temperature > 35)
                alerts.Add($"温度过高: {reading.Temperature:F1}°C，注意防暑");
            
            // 湿度警告
            if (reading.Humidity < 20)
                alerts.Add($"湿度过低: {reading.Humidity:F1}%，空气干燥");
            else if (reading.Humidity > 80)
                alerts.Add($"湿度过高: {reading.Humidity:F1}%，可能霉变");
            
            // 舒适度警告
            if (reading.ComfortLevel == "不舒适")
                alerts.Add("环境舒适度不佳，建议调节温湿度");
            
            // 发布警告事件
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
                
                Logger.LogWarning("环境警告: {Alert}", alert);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "检查环境警告时发生错误");
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
        
        Logger.LogInformation("温度传感器GAgent已激活: {GrainId}", this.GetGrainId());
        
        // 如果有连接配置且设备未连接，尝试自动连接
        if (State.ConnectionConfig != null && !State.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000, cancellationToken); // 延迟1秒后连接
                try
                {
                    await InitializeDeviceConnectionAsync(State.ConnectionConfig);
                    
                    // 启用设备监控
                    await SetDeviceMonitoringAsync(true);
                    
                    // 记录初始读数
                    var initialReading = await GetReadingAsync();
                    lock (_readingsLock)
                    {
                        _readings.Add(initialReading);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "自动连接温度传感器失败");
                }
            }, cancellationToken);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("温度传感器GAgent正在停用: {GrainId}, 原因: {Reason}", this.GetGrainId(), reason);
        
        // 取消属性变化事件订阅
        if (DeviceConnection != null)
        {
            DeviceConnection.PropertyChanged -= OnSensorPropertyChanged;
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}

/// <summary>
/// 环境警告事件
/// </summary>
[GenerateSerializer]
[Description("环境监测警告事件")]
public class EnvironmentAlertEvent : EventBase
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [Id(0)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备名称
    /// </summary>
    [Id(1)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 警告级别
    /// </summary>
    [Id(2)] public string AlertLevel { get; set; } = "Info";
    
    /// <summary>
    /// 警告消息
    /// </summary>
    [Id(3)] public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前温度
    /// </summary>
    [Id(4)] public double Temperature { get; set; }
    
    /// <summary>
    /// 当前湿度
    /// </summary>
    [Id(5)] public double Humidity { get; set; }
    
    /// <summary>
    /// 事件时间
    /// </summary>
    [Id(6)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
