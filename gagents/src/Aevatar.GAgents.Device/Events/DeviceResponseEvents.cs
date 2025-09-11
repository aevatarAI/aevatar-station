using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.State;

namespace Aevatar.GAgents.Device.Events;

/// <summary>
/// Device property read result event
/// </summary>
[GenerateSerializer]
[Description("Result of device property read operation")]
public class DevicePropertyReadResultEvent : DeviceEventBase
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Read value
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// Whether read was successful
    /// </summary>
    [Id(5)] public bool Success { get; set; } = true;
    
    /// <summary>
    /// Error message (if read failed)
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// 设备属性写入结果事件
/// </summary>
[GenerateSerializer]
[Description("设备属性写入操作的结果")]
public class DevicePropertyWriteResultEvent : DeviceEventBase
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(3)] public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// 写入的值
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// 写入是否成功
    /// </summary>
    [Id(5)] public bool Success { get; set; }
    
    /// <summary>
    /// 错误消息（如果写入失败）
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// 设备操作执行结果事件
/// </summary>
[GenerateSerializer]
[Description("设备操作执行的结果")]
public class DeviceActionExecutionResultEvent : DeviceEventBase
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Id(3)] public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行是否成功
    /// </summary>
    [Id(4)] public bool Success { get; set; }
    
    /// <summary>
    /// 执行结果
    /// </summary>
    [Id(5)] public object? Result { get; set; }
    
    /// <summary>
    /// 错误消息（如果执行失败）
    /// </summary>
    [Id(6)] public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    [Id(7)] public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// 设备状态响应事件
/// </summary>
[GenerateSerializer]
[Description("设备状态查询的响应")]
public class DeviceStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// 状态摘要
    /// </summary>
    [Id(3)] public DeviceStatusSummary StatusSummary { get; set; } = new();
}

/// <summary>
/// 设备连接操作结果事件
/// </summary>
[GenerateSerializer]
[Description("设备连接或断开操作的结果")]
public class DeviceConnectionResultEvent : DeviceEventBase
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    [Id(3)] public bool Success { get; set; }
    
    /// <summary>
    /// 操作类型（Connect/Disconnect/Reconnect）
    /// </summary>
    [Id(4)] public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误消息（如果操作失败）
    /// </summary>
    [Id(5)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// 设备健康状态响应事件
/// </summary>
[GenerateSerializer]
[Description("设备健康检查的响应")]
public class DeviceHealthStatusResponseEvent : DeviceEventBase
{
    /// <summary>
    /// 是否健康
    /// </summary>
    [Id(3)] public bool IsHealthy { get; set; }
    
    /// <summary>
    /// 健康状态描述
    /// </summary>
    [Id(4)] public string StatusDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 检查时间
    /// </summary>
    [Id(5)] public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 详细信息
    /// </summary>
    [Id(6)] public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// 设备支持的操作列表响应事件
/// </summary>
[GenerateSerializer]
[Description("设备支持的操作列表查询响应")]
public class DeviceSupportedActionsResponseEvent : DeviceEventBase
{
    /// <summary>
    /// 支持的操作列表
    /// </summary>
    [Id(3)] public List<DeviceActionInfo> SupportedActions { get; set; } = new();
}

/// <summary>
/// 设备属性定义列表响应事件
/// </summary>
[GenerateSerializer]
[Description("设备属性定义列表查询响应")]
public class DevicePropertiesResponseEvent : DeviceEventBase
{
    /// <summary>
    /// 属性定义列表
    /// </summary>
    [Id(3)] public List<DevicePropertyInfo> Properties { get; set; } = new();
}

/// <summary>
/// 批量属性读取结果事件
/// </summary>
[GenerateSerializer]
[Description("批量Result of device property read operation")]
public class MultipleDevicePropertiesReadResultEvent : DeviceEventBase
{
    /// <summary>
    /// 属性值字典
    /// </summary>
    [Id(3)] public Dictionary<string, object?> PropertyValues { get; set; } = new();
    
    /// <summary>
    /// 读取失败的属性列表
    /// </summary>
    [Id(4)] public List<string> FailedProperties { get; set; } = new();
    
    /// <summary>
    /// 错误消息字典（属性名 -> 错误消息）
    /// </summary>
    [Id(5)] public Dictionary<string, string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// 批量属性写入结果事件
/// </summary>
[GenerateSerializer]
[Description("批量设备属性写入操作的结果")]
public class MultipleDevicePropertiesWriteResultEvent : DeviceEventBase
{
    /// <summary>
    /// 写入成功的属性列表
    /// </summary>
    [Id(3)] public List<string> SuccessfulProperties { get; set; } = new();
    
    /// <summary>
    /// 写入失败的属性列表
    /// </summary>
    [Id(4)] public List<string> FailedProperties { get; set; } = new();
    
    /// <summary>
    /// 错误消息字典（属性名 -> 错误消息）
    /// </summary>
    [Id(5)] public Dictionary<string, string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// 设备操作信息
/// </summary>
[GenerateSerializer]
public class DeviceActionInfo
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作描述
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数信息列表
    /// </summary>
    [Id(3)] public List<DeviceActionParameterInfo> Parameters { get; set; } = new();
    
    /// <summary>
    /// 返回值类型名称
    /// </summary>
    [Id(4)] public string? ReturnTypeName { get; set; }
}

/// <summary>
/// 设备操作参数信息
/// </summary>
[GenerateSerializer]
public class DeviceActionParameterInfo
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数描述
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数类型名称
    /// </summary>
    [Id(3)] public string ParameterTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否必需
    /// </summary>
    [Id(4)] public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// 默认值
    /// </summary>
    [Id(5)] public object? DefaultValue { get; set; }
}

/// <summary>
/// 设备属性信息
/// </summary>
[GenerateSerializer]
public class DevicePropertyInfo
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    [Id(1)] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 属性描述
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 属性类型名称
    /// </summary>
    [Id(3)] public string PropertyTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前值
    /// </summary>
    [Id(4)] public object? Value { get; set; }
    
    /// <summary>
    /// 是否可读
    /// </summary>
    [Id(5)] public bool IsReadable { get; set; } = true;
    
    /// <summary>
    /// 是否可写
    /// </summary>
    [Id(6)] public bool IsWritable { get; set; } = false;
    
    /// <summary>
    /// 单位
    /// </summary>
    [Id(7)] public string? Unit { get; set; }
    
    /// <summary>
    /// 最小值
    /// </summary>
    [Id(8)] public object? MinValue { get; set; }
    
    /// <summary>
    /// 最大值
    /// </summary>
    [Id(9)] public object? MaxValue { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    [Id(10)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
