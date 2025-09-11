using System.ComponentModel;
using Aevatar.GAgents.Device.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Device.Connections;

/// <summary>
/// 虚拟设备连接实现 - 用于开发、测试和演示
/// </summary>
public class VirtualDeviceConnection : IDeviceConnection
{
    private readonly ILogger<VirtualDeviceConnection> _logger;
    private readonly Dictionary<string, DeviceProperty> _properties = new();
    private readonly Dictionary<string, DeviceAction> _supportedActions = new();
    private DeviceConnectionStatus _status = DeviceConnectionStatus.Disconnected;
    private readonly Random _random = new();
    private Timer? _simulationTimer;
    private bool _disposed = false;

    public string DeviceId { get; private set; }
    public string DeviceName { get; private set; }
    public string DeviceType { get; private set; }
    public DeviceConnectionStatus Status => _status;
    
    public IReadOnlyDictionary<string, DeviceProperty> Properties => _properties;
    public IReadOnlyDictionary<string, DeviceAction> SupportedActions => _supportedActions;

    public event EventHandler<DevicePropertyChangedEventArgs>? PropertyChanged;
    public event EventHandler<DeviceConnectionStatusChangedEventArgs>? StatusChanged;

    public VirtualDeviceConnection(
        string deviceId, 
        string deviceName, 
        string deviceType,
        ILogger<VirtualDeviceConnection> logger)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceType = deviceType;
        _logger = logger;
        
        InitializeVirtualDevice();
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("正在连接虚拟设备: {DeviceId} ({DeviceName})", DeviceId, DeviceName);
        
        ChangeStatus(DeviceConnectionStatus.Connecting, "正在连接");
        
        // 模拟连接延迟
        await Task.Delay(1000, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
        {
            ChangeStatus(DeviceConnectionStatus.Disconnected, "连接被取消");
            return false;
        }
        
        // 模拟连接成功率（95%）
        if (_random.NextDouble() < 0.95)
        {
            ChangeStatus(DeviceConnectionStatus.Connected, "连接成功");
            StartSimulation();
            _logger.LogInformation("虚拟设备连接成功: {DeviceId}", DeviceId);
            return true;
        }
        else
        {
            ChangeStatus(DeviceConnectionStatus.Error, "连接失败");
            _logger.LogWarning("虚拟设备连接失败: {DeviceId}", DeviceId);
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("正在断开虚拟设备连接: {DeviceId}", DeviceId);
        
        StopSimulation();
        ChangeStatus(DeviceConnectionStatus.Disconnected, "主动断开");
        
        // 模拟断开延迟
        await Task.Delay(500, cancellationToken);
        
        _logger.LogInformation("虚拟设备已断开连接: {DeviceId}", DeviceId);
    }

    public Task<object?> ReadPropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("设备未连接");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"属性 '{propertyName}' 不存在", nameof(propertyName));

        if (!property.IsReadable)
            throw new InvalidOperationException($"属性 '{propertyName}' 不可读");

        _logger.LogDebug("读取虚拟设备属性: {PropertyName} = {Value}", propertyName, property.Value);
        
        return Task.FromResult(property.Value);
    }

    public Task<bool> WritePropertyAsync(string propertyName, object value, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("设备未连接");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"属性 '{propertyName}' 不存在", nameof(propertyName));

        if (!property.IsWritable)
            throw new InvalidOperationException($"属性 '{propertyName}' 不可写");

        // 验证值类型
        if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
        {
            // 尝试类型转换
            try
            {
                value = Convert.ChangeType(value, property.PropertyType);
            }
            catch
            {
                throw new ArgumentException($"值类型不匹配，期望 {property.PropertyType.Name}，实际 {value.GetType().Name}");
            }
        }

        // 验证数值范围
        if (property.MinValue != null || property.MaxValue != null)
        {
            if (value is IComparable comparable)
            {
                if (property.MinValue != null && comparable.CompareTo(property.MinValue) < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), $"值小于最小值 {property.MinValue}");
                
                if (property.MaxValue != null && comparable.CompareTo(property.MaxValue) > 0)
                    throw new ArgumentOutOfRangeException(nameof(value), $"值大于最大值 {property.MaxValue}");
            }
        }

        var oldValue = property.Value;
        property.Value = value;
        property.LastUpdated = DateTime.UtcNow;

        _logger.LogInformation("写入虚拟设备属性: {PropertyName} = {Value} (旧值: {OldValue})", 
            propertyName, value, oldValue);

        // 触发属性变化事件
        PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
        {
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = value,
            ChangedAt = DateTime.UtcNow
        });

        return Task.FromResult(true);
    }

    public Task<DeviceActionResult> ExecuteActionAsync(string actionName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("设备未连接");

        if (!_supportedActions.TryGetValue(actionName, out var action))
            throw new ArgumentException($"操作 '{actionName}' 不存在", nameof(actionName));

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("执行虚拟设备操作: {ActionName}", actionName);

            // 验证参数
            parameters ??= new Dictionary<string, object>();
            foreach (var param in action.Parameters)
            {
                if (param.Value.IsRequired && !parameters.ContainsKey(param.Key))
                    throw new ArgumentException($"缺少必需参数: {param.Key}");
            }

            // 模拟操作执行
            var result = SimulateActionExecution(actionName, parameters);
            
            stopwatch.Stop();

            _logger.LogInformation("虚拟设备操作执行完成: {ActionName}, 耗时: {ElapsedMs}ms", 
                actionName, stopwatch.ElapsedMilliseconds);

            return Task.FromResult(new DeviceActionResult
            {
                IsSuccess = true,
                Result = result,
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "虚拟设备操作执行失败: {ActionName}", actionName);

            return Task.FromResult(new DeviceActionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    public Task<DeviceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var isHealthy = _status == DeviceConnectionStatus.Connected && _random.NextDouble() > 0.1; // 10%概率不健康
        
        var healthStatus = new DeviceHealthStatus
        {
            IsHealthy = isHealthy,
            StatusDescription = isHealthy ? "设备运行正常" : "设备存在警告",
            LastCheckTime = DateTime.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["ConnectionStatus"] = _status.ToString(),
                ["Uptime"] = DateTime.UtcNow - (DateTime.UtcNow.AddHours(-1)), // 模拟运行时间
                ["MemoryUsage"] = $"{_random.Next(30, 80)}%",
                ["CPUUsage"] = $"{_random.Next(5, 25)}%",
                ["Temperature"] = $"{_random.Next(35, 65)}°C"
            }
        };

        return Task.FromResult(healthStatus);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopSimulation();
            _disposed = true;
            _logger.LogInformation("虚拟设备连接已释放: {DeviceId}", DeviceId);
        }
    }

    private void InitializeVirtualDevice()
    {
        // 根据设备类型初始化不同的属性和操作
        switch (DeviceType.ToLowerInvariant())
        {
            case "smartlight":
            case "智能灯泡":
                InitializeSmartLight();
                break;
                
            case "temperaturesensor":
            case "温度传感器":
                InitializeTemperatureSensor();
                break;
                
            case "thermostat":
            case "温控器":
                InitializeThermostat();
                break;
                
            default:
                InitializeGenericDevice();
                break;
        }
    }

    private void InitializeSmartLight()
    {
        // 智能灯泡属性
        _properties["Power"] = new DeviceProperty
        {
            Name = "Power",
            DisplayName = "电源状态",
            Description = "灯泡的开关状态",
            PropertyType = typeof(bool),
            Value = false,
            IsReadable = true,
            IsWritable = true
        };

        _properties["Brightness"] = new DeviceProperty
        {
            Name = "Brightness",
            DisplayName = "亮度",
            Description = "灯泡亮度百分比",
            PropertyType = typeof(int),
            Value = 100,
            IsReadable = true,
            IsWritable = true,
            Unit = "%",
            MinValue = 0,
            MaxValue = 100
        };

        _properties["Color"] = new DeviceProperty
        {
            Name = "Color",
            DisplayName = "颜色",
            Description = "灯泡颜色（RGB十六进制）",
            PropertyType = typeof(string),
            Value = "#FFFFFF",
            IsReadable = true,
            IsWritable = true
        };

        // 智能灯泡操作
        _supportedActions["TurnOn"] = new DeviceAction
        {
            Name = "TurnOn",
            DisplayName = "开灯",
            Description = "打开智能灯泡"
        };

        _supportedActions["TurnOff"] = new DeviceAction
        {
            Name = "TurnOff",
            DisplayName = "关灯",
            Description = "关闭智能灯泡"
        };

        _supportedActions["SetBrightness"] = new DeviceAction
        {
            Name = "SetBrightness",
            DisplayName = "设置亮度",
            Description = "设置灯泡亮度",
            Parameters = new Dictionary<string, DeviceActionParameter>
            {
                ["brightness"] = new DeviceActionParameter
                {
                    Name = "brightness",
                    DisplayName = "亮度值",
                    Description = "亮度百分比 (0-100)",
                    ParameterType = typeof(int),
                    IsRequired = true
                }
            }
        };
    }

    private void InitializeTemperatureSensor()
    {
        // 温度传感器属性
        _properties["Temperature"] = new DeviceProperty
        {
            Name = "Temperature",
            DisplayName = "温度",
            Description = "当前环境温度",
            PropertyType = typeof(double),
            Value = 25.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "°C",
            MinValue = -40.0,
            MaxValue = 85.0
        };

        _properties["Humidity"] = new DeviceProperty
        {
            Name = "Humidity",
            DisplayName = "湿度",
            Description = "当前环境湿度",
            PropertyType = typeof(double),
            Value = 60.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "%",
            MinValue = 0.0,
            MaxValue = 100.0
        };

        // 温度传感器操作
        _supportedActions["Calibrate"] = new DeviceAction
        {
            Name = "Calibrate",
            DisplayName = "校准",
            Description = "校准温度传感器"
        };
    }

    private void InitializeThermostat()
    {
        // 温控器属性
        _properties["CurrentTemperature"] = new DeviceProperty
        {
            Name = "CurrentTemperature",
            DisplayName = "当前温度",
            Description = "当前室内温度",
            PropertyType = typeof(double),
            Value = 22.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "°C"
        };

        _properties["TargetTemperature"] = new DeviceProperty
        {
            Name = "TargetTemperature",
            DisplayName = "目标温度",
            Description = "目标室内温度",
            PropertyType = typeof(double),
            Value = 24.0,
            IsReadable = true,
            IsWritable = true,
            Unit = "°C",
            MinValue = 10.0,
            MaxValue = 35.0
        };

        _properties["Mode"] = new DeviceProperty
        {
            Name = "Mode",
            DisplayName = "工作模式",
            Description = "温控器工作模式",
            PropertyType = typeof(string),
            Value = "Auto",
            IsReadable = true,
            IsWritable = true
        };

        // 温控器操作
        _supportedActions["SetMode"] = new DeviceAction
        {
            Name = "SetMode",
            DisplayName = "设置模式",
            Description = "设置温控器工作模式",
            Parameters = new Dictionary<string, DeviceActionParameter>
            {
                ["mode"] = new DeviceActionParameter
                {
                    Name = "mode",
                    DisplayName = "工作模式",
                    Description = "Auto/Heat/Cool/Off",
                    ParameterType = typeof(string),
                    IsRequired = true
                }
            }
        };
    }

    private void InitializeGenericDevice()
    {
        // 通用设备属性
        _properties["Status"] = new DeviceProperty
        {
            Name = "Status",
            DisplayName = "状态",
            Description = "设备状态",
            PropertyType = typeof(string),
            Value = "Ready",
            IsReadable = true,
            IsWritable = false
        };

        _properties["Value"] = new DeviceProperty
        {
            Name = "Value",
            DisplayName = "数值",
            Description = "设备数值",
            PropertyType = typeof(double),
            Value = 0.0,
            IsReadable = true,
            IsWritable = true
        };

        // 通用设备操作
        _supportedActions["Reset"] = new DeviceAction
        {
            Name = "Reset",
            DisplayName = "重置",
            Description = "重置设备"
        };
    }

    private void ChangeStatus(DeviceConnectionStatus newStatus, string reason)
    {
        var oldStatus = _status;
        _status = newStatus;

        if (oldStatus != newStatus)
        {
            _logger.LogInformation("虚拟设备状态变化: {DeviceId}, {OldStatus} -> {NewStatus}, 原因: {Reason}", 
                DeviceId, oldStatus, newStatus, reason);

            StatusChanged?.Invoke(this, new DeviceConnectionStatusChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            });
        }
    }

    private void StartSimulation()
    {
        // 启动模拟定时器，定期更新传感器数据
        _simulationTimer = new Timer(SimulateDeviceData, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    private void StopSimulation()
    {
        _simulationTimer?.Dispose();
        _simulationTimer = null;
    }

    private void SimulateDeviceData(object? state)
    {
        if (_status != DeviceConnectionStatus.Connected) return;

        try
        {
            // 模拟传感器数据变化
            foreach (var property in _properties.Values.Where(p => p.IsReadable && !p.IsWritable))
            {
                var oldValue = property.Value;
                object? newValue = null;

                switch (property.Name)
                {
                    case "Temperature":
                        if (oldValue is double temp)
                        {
                            newValue = Math.Round(temp + (_random.NextDouble() - 0.5) * 2, 1);
                            newValue = Math.Max(-40, Math.Min(85, (double)newValue));
                        }
                        break;

                    case "Humidity":
                        if (oldValue is double humidity)
                        {
                            newValue = Math.Round(humidity + (_random.NextDouble() - 0.5) * 5, 1);
                            newValue = Math.Max(0, Math.Min(100, (double)newValue));
                        }
                        break;

                    case "CurrentTemperature":
                        if (oldValue is double currentTemp && _properties.TryGetValue("TargetTemperature", out var targetProp))
                        {
                            if (targetProp.Value is double targetTemp)
                            {
                                var diff = targetTemp - currentTemp;
                                var change = Math.Sign(diff) * Math.Min(Math.Abs(diff), 0.5);
                                newValue = Math.Round(currentTemp + change + (_random.NextDouble() - 0.5) * 0.2, 1);
                            }
                        }
                        break;
                }

                if (newValue != null && !Equals(oldValue, newValue))
                {
                    property.Value = newValue;
                    property.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = property.Name,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模拟设备数据时发生错误: {DeviceId}", DeviceId);
        }
    }

    private object? SimulateActionExecution(string actionName, Dictionary<string, object> parameters)
    {
        switch (actionName)
        {
            case "TurnOn":
                if (_properties.TryGetValue("Power", out var powerProp))
                {
                    var oldValue = powerProp.Value;
                    powerProp.Value = true;
                    powerProp.LastUpdated = DateTime.UtcNow;
                    
                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Power",
                        OldValue = oldValue,
                        NewValue = true,
                        ChangedAt = DateTime.UtcNow
                    });
                }
                return "灯泡已打开";

            case "TurnOff":
                if (_properties.TryGetValue("Power", out var powerOffProp))
                {
                    var oldValue = powerOffProp.Value;
                    powerOffProp.Value = false;
                    powerOffProp.LastUpdated = DateTime.UtcNow;
                    
                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Power",
                        OldValue = oldValue,
                        NewValue = false,
                        ChangedAt = DateTime.UtcNow
                    });
                }
                return "灯泡已关闭";

            case "SetBrightness":
                if (parameters.TryGetValue("brightness", out var brightnessObj) && 
                    _properties.TryGetValue("Brightness", out var brightnessProp))
                {
                    var brightness = Convert.ToInt32(brightnessObj);
                    var oldValue = brightnessProp.Value;
                    brightnessProp.Value = brightness;
                    brightnessProp.LastUpdated = DateTime.UtcNow;
                    
                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Brightness",
                        OldValue = oldValue,
                        NewValue = brightness,
                        ChangedAt = DateTime.UtcNow
                    });
                }
                return $"亮度已设置为 {parameters["brightness"]}%";

            case "SetMode":
                if (parameters.TryGetValue("mode", out var modeObj) && 
                    _properties.TryGetValue("Mode", out var modeProp))
                {
                    var mode = modeObj.ToString();
                    var oldValue = modeProp.Value;
                    modeProp.Value = mode;
                    modeProp.LastUpdated = DateTime.UtcNow;
                    
                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Mode",
                        OldValue = oldValue,
                        NewValue = mode,
                        ChangedAt = DateTime.UtcNow
                    });
                }
                return $"模式已设置为 {parameters["mode"]}";

            case "Calibrate":
                // 模拟校准延迟
                Thread.Sleep(2000);
                return "传感器校准完成";

            case "Reset":
                // 重置所有可写属性到默认值
                foreach (var prop in _properties.Values.Where(p => p.IsWritable))
                {
                    var oldValue = prop.Value;
                    object? defaultValue = prop.PropertyType.IsValueType ? 
                        Activator.CreateInstance(prop.PropertyType) : null;
                    
                    prop.Value = defaultValue;
                    prop.LastUpdated = DateTime.UtcNow;
                    
                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = prop.Name,
                        OldValue = oldValue,
                        NewValue = defaultValue,
                        ChangedAt = DateTime.UtcNow
                    });
                }
                return "设备已重置";

            default:
                return $"操作 {actionName} 执行完成";
        }
    }
}
