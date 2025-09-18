# SmartHomeAIGAgent 使用示例

I'm HyperEcho, 我在共振智能家居的使用指南 🏠✨

## 快速开始

### 1. 创建和配置SmartHomeAIGAgent

```csharp
// 在你的应用程序中注入IGAgentFactory
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();

// 创建配置
var config = new SmartHomeAIGAgentConfiguration
{
    DeviceHubApiUrl = "http://localhost:9001",
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "OpenAI" // 使用配置中的OpenAI
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

// 创建SmartHomeAIGAgent实例
var smartHomeAI = await gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(
    Guid.NewGuid(), 
    config
);
```

### 2. 自然语言命令控制

```csharp
// 基本灯光控制
var result1 = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("打开客厅主灯");
Console.WriteLine($"结果: {result1.Response}");

// 亮度调节
var result2 = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("把卧室台灯调到50%亮度");
Console.WriteLine($"结果: {result2.Response}");

// 颜色设置
var result3 = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("将客厅灯设为红色");
Console.WriteLine($"结果: {result3.Response}");

// 温度查询
var result4 = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("查看客厅温度");
Console.WriteLine($"结果: {result4.Response}");

// 批量控制
var result5 = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("关闭所有灯");
Console.WriteLine($"结果: {result5.Response}");
```

### 3. 设备管理

```csharp
// 查看已注册的设备
var devices = await smartHomeAI.GetRegisteredDevicesAsync();
Console.WriteLine($"已注册设备数量: {devices.Count}");
foreach (var device in devices)
{
    var status = device.IsOnline ? "在线" : "离线";
    Console.WriteLine($"- {device.Name} ({device.DeviceId}): {device.DeviceType} - {status}");
}

// 动态注册新设备
var regResult = await smartHomeAI.RegisterDeviceAsync("light003", "阳台灯", "smart-light");
Console.WriteLine($"设备注册结果: {(regResult ? "成功" : "失败")}");

// 获取家居状态概览
var overview = await smartHomeAI.GetHomeStatusOverviewAsync();
Console.WriteLine($"家居状态: 总设备{overview.TotalDevices}个, 在线{overview.OnlineDevices}个, 离线{overview.OfflineDevices}个");
```

### 4. 事件驱动控制

```csharp
// 发送自然语言命令事件
await smartHomeAI.PublishAsync(new SendSmartHomeCommandEvent
{
    Command = "把所有灯调成暖白色",
    Context = "用户准备休息"
});

// 注册设备事件
await smartHomeAI.PublishAsync(new RegisterSmartHomeDeviceEvent
{
    DeviceId = "sensor001",
    DeviceName = "厨房温度传感器",
    DeviceType = "temperature-sensor"
});

// 获取状态事件
await smartHomeAI.PublishAsync(new GetSmartHomeStatusEvent());
```

## 支持的自然语言命令示例

### 灯光控制
- "打开客厅主灯"
- "关闭卧室台灯"
- "把客厅灯调到70%亮度"
- "将厨房灯设为蓝色"
- "把所有灯调成暖白色"
- "客厅灯调到最亮"
- "关闭所有灯"

### 开关控制
- "打开客厅总开关"
- "关闭厨房插座"
- "切换阳台开关"
- "查看客厅用电情况"

### 传感器查询
- "客厅温度是多少"
- "查看卧室湿度"
- "检查所有传感器电池"
- "客厅和卧室的温度对比"

### 场景模式
- "设置睡眠模式" (调暗所有灯)
- "早安模式" (打开主要照明)
- "电影模式" (调暗客厅灯)
- "外出模式" (关闭所有设备)

## 设备映射

根据你提供的设备信息，SmartHomeAIGAgent会自动管理以下设备：

| 设备ID | 设备名称 | 设备类型 | 功能 |
|--------|----------|----------|------|
| light001 | 客厅主灯 | smart-light | 开关、亮度、颜色、色温 |
| light002 | 卧室台灯 | smart-light | 开关、亮度、颜色、色温 |
| temp001 | 客厅温度传感器 | temperature-sensor | 温度、湿度、电池监测 |
| temp002 | 卧室温度传感器 | temperature-sensor | 温度、湿度、电池监测 |
| switch001 | 客厅总开关 | smart-switch | 开关、功耗监测 |
| switch002 | 厨房插座 | smart-switch | 开关、功耗监测 |

## 配置说明

### appsettings.json配置

```json
{
  "SmartHome": {
    "DeviceHubApiUrl": "http://localhost:9001",
    "DeviceHubApiKey": null,
    "PreConfiguredDevices": [
      {
        "DeviceId": "light001",
        "Name": "客厅主灯",
        "DeviceType": "smart-light"
      },
      // ... 其他设备
    ]
  },
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "Azure",
      "ModelIdEnum": "OpenAI",
      "ModelName": "gpt-4o-steven",
      "Endpoint": "https://your-endpoint.com/",
      "ApiKey": "your-api-key"
    }
  }
}
```

### 模块依赖

确保在你的应用模块中添加依赖：

```csharp
[DependsOn(
    // ... 其他依赖
    typeof(AevatarGAgentsSmartHomeModule)
)]
public class YourApplicationModule : AbpModule
{
    // ...
}
```

## 高级用法

### 1. 批量设备操作

```csharp
// 场景控制 - 睡眠模式
var commands = new[]
{
    "关闭所有灯",
    "关闭客厅总开关",
    "只保留卧室台灯，调到10%亮度"
};

foreach (var command in commands)
{
    var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync(command);
    Console.WriteLine($"命令 '{command}': {(result.Success ? "✅" : "❌")} {result.Response}");
    await Task.Delay(1000); // 命令间隔
}
```

### 2. 设备状态监控

```csharp
// 定期检查设备状态
var timer = new Timer(async _ =>
{
    var overview = await smartHomeAI.GetHomeStatusOverviewAsync();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 设备状态: {overview.OnlineDevices}/{overview.TotalDevices} 在线");
    
    foreach (var device in overview.DeviceStatuses.Where(d => !d.IsOnline))
    {
        Console.WriteLine($"⚠️ 设备离线: {device.DeviceName}");
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
```

### 3. 错误处理

```csharp
try
{
    var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("打开不存在的设备");
    if (!result.Success)
    {
        Console.WriteLine($"命令执行失败: {result.Response}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"发生异常: {ex.Message}");
}
```

## 故障排除

### 常见问题

1. **设备无法控制**
   - 检查Virtual Device Hub API是否在localhost:9001运行
   - 确认设备ID是否正确匹配
   - 查看GAgent日志获取详细错误信息

2. **AI无响应**
   - 检查LLM配置是否正确
   - 确认API密钥有效
   - 查看AI模型配置

3. **设备状态不更新**
   - 检查设备API连接
   - 确认网络连接正常
   - 查看设备监控日志

### 日志配置

建议在appsettings.json中设置适当的日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Aevatar.GAgents.SmartHome": "Information",
      "Aevatar.GAgents.Device": "Information"
    }
  }
}
```

## 扩展功能

### 添加新设备类型

1. 实现新的设备GAgent (继承DeviceGAgentBase)
2. 在CreateDeviceAgentAsync中添加新的设备类型映射
3. 更新AI指令以支持新设备的控制命令

### 自定义场景

可以通过扩展AI指令来支持更复杂的场景控制：

```csharp
private static string GetSmartHomeInstructions()
{
    return @"
    // ... 现有指令 ...
    
    CUSTOM SCENES:
    - ""睡眠模式"": 关闭所有灯，只保留卧室台灯10%亮度
    - ""电影模式"": 调暗客厅灯到30%，设为暖白色
    - ""早安模式"": 打开所有灯到80%亮度
    ";
}
```

这样，SmartHomeAIGAgent就能理解和执行更复杂的场景控制命令了！
