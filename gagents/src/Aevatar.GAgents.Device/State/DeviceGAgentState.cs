using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Device.Abstractions;

namespace Aevatar.GAgents.Device.State;

/// <summary>
/// 设备GAgent状态类
/// </summary>
[GenerateSerializer]
public class DeviceGAgentState : AIGAgentStateBase
{
    /// <summary>
    /// 设备连接配置
    /// </summary>
    [Id(0)] public DeviceConnectionConfig? ConnectionConfig { get; set; }
    
    /// <summary>
    /// 设备ID
    /// </summary>
    [Id(1)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备名称
    /// </summary>
    [Id(2)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备类型
    /// </summary>
    [Id(3)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    [Id(4)] public bool IsConnected { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    [Id(5)] public DeviceConnectionStatus ConnectionStatus { get; set; } = DeviceConnectionStatus.Disconnected;
    
    /// <summary>
    /// 连接时间
    /// </summary>
    [Id(6)] public DateTime? ConnectedAt { get; set; }
    
    /// <summary>
    /// 最后状态更新时间
    /// </summary>
    [Id(7)] public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 是否启用监控
    /// </summary>
    [Id(8)] public bool MonitoringEnabled { get; set; }
    
    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    [Id(9)] public int MonitoringIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// 监控状态变化时间
    /// </summary>
    [Id(10)] public DateTime? MonitoringStateChangedAt { get; set; }
    
    /// <summary>
    /// 最后错误消息
    /// </summary>
    [Id(11)] public string? LastErrorMessage { get; set; }
    
    /// <summary>
    /// 属性变化历史记录
    /// </summary>
    [Id(12)] public List<DevicePropertyChangeRecord> PropertyChangeHistory { get; set; } = new();
    
    /// <summary>
    /// 最后属性变化时间
    /// </summary>
    [Id(13)] public DateTime? LastPropertyChangeAt { get; set; }
    
    /// <summary>
    /// 设备统计信息
    /// </summary>
    [Id(14)] public DeviceStatistics Statistics { get; set; } = new();
}

/// <summary>
/// 设备连接配置
/// </summary>
[GenerateSerializer]
public class DeviceConnectionConfig
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
    /// 设备类型
    /// </summary>
    [Id(2)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接字符串或配置信息
    /// </summary>
    [Id(3)] public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    [Id(4)] public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 重连尝试次数
    /// </summary>
    [Id(5)] public int RetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// 重连间隔（秒）
    /// </summary>
    [Id(6)] public int RetryIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// 扩展配置属性
    /// </summary>
    [Id(7)] public Dictionary<string, string> ExtendedProperties { get; set; } = new();
}

/// <summary>
/// 设备属性变化记录
/// </summary>
[GenerateSerializer]
public class DevicePropertyChangeRecord
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(0)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// 旧值
    /// </summary>
    [Id(1)] public object? OldValue { get; set; }
    
    /// <summary>
    /// 新值
    /// </summary>
    [Id(2)] public object? NewValue { get; set; }
    
    /// <summary>
    /// 变化时间
    /// </summary>
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 设备统计信息
/// </summary>
[GenerateSerializer]
public class DeviceStatistics
{
    /// <summary>
    /// 总连接次数
    /// </summary>
    [Id(0)] public long TotalConnections { get; set; }
    
    /// <summary>
    /// 连接失败次数
    /// </summary>
    [Id(1)] public long ConnectionFailures { get; set; }
    
    /// <summary>
    /// 属性读取次数
    /// </summary>
    [Id(2)] public long PropertyReads { get; set; }
    
    /// <summary>
    /// 属性写入次数
    /// </summary>
    [Id(3)] public long PropertyWrites { get; set; }
    
    /// <summary>
    /// 操作执行次数
    /// </summary>
    [Id(4)] public long ActionExecutions { get; set; }
    
    /// <summary>
    /// 错误发生次数
    /// </summary>
    [Id(5)] public long ErrorCount { get; set; }
    
    /// <summary>
    /// 最后重置时间
    /// </summary>
    [Id(6)] public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 设备状态摘要
/// </summary>
[GenerateSerializer]
public class DeviceStatusSummary
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
    /// 设备类型
    /// </summary>
    [Id(2)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    [Id(3)] public bool IsConnected { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    [Id(4)] public DeviceConnectionStatus Status { get; set; }
    
    /// <summary>
    /// 是否健康
    /// </summary>
    [Id(5)] public bool IsHealthy { get; set; } = true;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    [Id(6)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 当前属性值
    /// </summary>
    [Id(7)] public Dictionary<string, object?> Properties { get; set; } = new();
    
    /// <summary>
    /// 健康状态详情
    /// </summary>
    [Id(8)] public Dictionary<string, object> HealthDetails { get; set; } = new();
    
    /// <summary>
    /// 错误消息
    /// </summary>
    [Id(9)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// 设备GAgent状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class DeviceGAgentStateLogEvent : StateLogEventBase<DeviceGAgentStateLogEvent>
{
}

/// <summary>
/// 设备连接初始化日志事件
/// </summary>
[GenerateSerializer]
public class DeviceConnectionInitializedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// 连接配置
    /// </summary>
    [Id(0)] public DeviceConnectionConfig Config { get; set; } = new();
    
    /// <summary>
    /// 设备ID
    /// </summary>
    [Id(1)] public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备名称
    /// </summary>
    [Id(2)] public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备类型
    /// </summary>
    [Id(3)] public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接时间
    /// </summary>
    [Id(4)] public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 设备监控状态变化日志事件
/// </summary>
[GenerateSerializer]
public class DeviceMonitoringStateChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// 是否启用监控
    /// </summary>
    [Id(0)] public bool Enabled { get; set; }
    
    /// <summary>
    /// 变化时间
    /// </summary>
    [Id(1)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 设备连接状态变化日志事件
/// </summary>
[GenerateSerializer]
public class DeviceConnectionStatusChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// 旧状态
    /// </summary>
    [Id(0)] public DeviceConnectionStatus OldStatus { get; set; }
    
    /// <summary>
    /// 新状态
    /// </summary>
    [Id(1)] public DeviceConnectionStatus NewStatus { get; set; }
    
    /// <summary>
    /// 变化时间
    /// </summary>
    [Id(2)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 变化原因
    /// </summary>
    [Id(3)] public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 设备属性变化日志事件
/// </summary>
[GenerateSerializer]
public class DevicePropertyChangedLogEvent : DeviceGAgentStateLogEvent
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(0)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// 旧值
    /// </summary>
    [Id(1)] public object? OldValue { get; set; }
    
    /// <summary>
    /// 新值
    /// </summary>
    [Id(2)] public object? NewValue { get; set; }
    
    /// <summary>
    /// 变化时间
    /// </summary>
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
