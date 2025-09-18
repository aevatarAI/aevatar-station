# Smart Home AI GAgent

智能家居AI助手GAgent，提供自然语言控制智能家居设备的能力。

## 功能特性

### 🏠 设备管理
- 支持智能灯泡、智能开关、温度传感器
- 自动设备注册和发现
- 实时设备状态监控
- 设备在线状态检测

### 🗣️ 自然语言控制
- 理解中文自然语言命令
- 智能解析用户意图
- 自动匹配目标设备
- 提供清晰的执行反馈

### 🤖 AI增强功能
- 基于大语言模型的命令理解
- 上下文感知的设备操作
- 智能化的场景控制
- 学习用户使用习惯

## 支持的设备类型

### 智能灯泡 (Smart Light)
- **控制功能**: 开关、亮度调节、颜色设置、色温调节
- **状态查询**: 电源状态、亮度、颜色、能耗、运行时间
- **示例命令**:
  - "打开客厅主灯"
  - "把卧室台灯调到50%亮度"
  - "将厨房灯设为红色"
  - "把所有灯调成暖白色"

### 智能开关 (Smart Switch)
- **控制功能**: 开关控制、能耗监控
- **状态查询**: 电源状态、电流、电压、功耗、每日用电量、温度
- **示例命令**:
  - "打开客厅总开关"
  - "关闭厨房插座"
  - "查看客厅用电情况"

### 温度传感器 (Temperature Sensor)
- **监控功能**: 温度、湿度、电池电量监测
- **维护功能**: 传感器校准
- **示例命令**:
  - "查看客厅温度"
  - "卧室湿度是多少"
  - "检查所有传感器电池"

## 使用方法

### 1. 配置和初始化

```csharp
// 创建配置
var config = new SmartHomeAIGAgentConfiguration
{
    DeviceHubApiUrl = "http://localhost:9001",
    DeviceHubApiKey = "your-api-key",
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

// 获取GAgent实例
var smartHomeAI = await gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(
    Guid.NewGuid(), 
    config
);
```

### 2. 自然语言命令控制

```csharp
// 执行自然语言命令
var result = await smartHomeAI.ExecuteNaturalLanguageCommandAsync("打开客厅主灯");
Console.WriteLine($"命令执行结果: {result.Response}");

// 更复杂的命令
await smartHomeAI.ExecuteNaturalLanguageCommandAsync("把所有灯调到70%亮度");
await smartHomeAI.ExecuteNaturalLanguageCommandAsync("查看卧室的温度和湿度");
await smartHomeAI.ExecuteNaturalLanguageCommandAsync("关闭所有开关");
```

### 3. 设备管理

```csharp
// 注册新设备
await smartHomeAI.RegisterDeviceAsync("light003", "阳台灯", "smart-light");

// 获取所有设备
var devices = await smartHomeAI.GetRegisteredDevicesAsync();
foreach (var device in devices)
{
    Console.WriteLine($"设备: {device.Name} ({device.DeviceId}) - {(device.IsOnline ? "在线" : "离线")}");
}

// 获取家居状态概览
var overview = await smartHomeAI.GetHomeStatusOverviewAsync();
Console.WriteLine($"总设备数: {overview.TotalDevices}, 在线: {overview.OnlineDevices}");
```

### 4. 事件驱动控制

```csharp
// 发送自然语言命令事件
await smartHomeAI.PublishAsync(new SendSmartHomeCommandEvent
{
    Command = "把客厅灯调成蓝色",
    Context = "用户在客厅"
});

// 注册设备事件
await smartHomeAI.PublishAsync(new RegisterSmartHomeDeviceEvent
{
    DeviceId = "light004",
    DeviceName = "书房灯",
    DeviceType = "smart-light"
});

// 获取状态事件
await smartHomeAI.PublishAsync(new GetSmartHomeStatusEvent());
```

## 自然语言命令示例

### 灯光控制
- "打开客厅主灯"
- "关闭所有灯"
- "把卧室台灯调到30%亮度"
- "将厨房灯设为红色"
- "把客厅灯调成暖白色"
- "所有灯调到最亮"

### 开关控制
- "打开客厅总开关"
- "关闭厨房插座"
- "切换阳台开关"
- "查看客厅用电情况"

### 传感器查询
- "客厅温度是多少"
- "查看卧室湿度"
- "所有传感器的电池电量"
- "检查温度传感器状态"

### 场景控制
- "打开所有灯"
- "关闭所有设备"
- "设置睡眠模式" (调暗所有灯)
- "早安模式" (打开主要照明)

## API接口

### ISmartHomeAIGAgent 接口方法

```csharp
// 设备管理
Task<bool> RegisterDeviceAsync(string deviceId, string deviceName, string deviceType);
Task<bool> UnregisterDeviceAsync(string deviceId);
Task<List<SmartHomeDeviceInfo>> GetRegisteredDevicesAsync();

// 命令执行
Task<SmartHomeCommandResult> ExecuteNaturalLanguageCommandAsync(string command);

// 状态查询
Task<SmartHomeStatusOverview> GetHomeStatusOverviewAsync();
```

### 事件类型

- `SendSmartHomeCommandEvent` - 发送自然语言命令
- `RegisterSmartHomeDeviceEvent` - 注册设备
- `UnregisterSmartHomeDeviceEvent` - 注销设备
- `GetSmartHomeStatusEvent` - 获取状态
- `SmartHomeCommandResultEvent` - 命令执行结果
- `SmartHomeStatusResponseEvent` - 状态响应

## 配置选项

### SmartHomeAIGAgentConfiguration

```csharp
public class SmartHomeAIGAgentConfiguration : AIGAgentConfigurationBase
{
    // 设备Hub API地址
    public string DeviceHubApiUrl { get; set; } = "http://localhost:9001";
    
    // API密钥
    public string? DeviceHubApiKey { get; set; }
    
    // 预配置设备列表
    public List<SmartHomeDeviceInfo> PreConfiguredDevices { get; set; } = new();
}
```

## 部署和集成

### 1. 添加到应用模块

```csharp
[DependsOn(typeof(AevatarGAgentsSmartHomeModule))]
public class YourApplicationModule : AbpModule
{
    // ...
}
```

### 2. 配置设备Hub

确保Virtual Device Hub API在指定端口运行，并且包含以下设备：
- light001 (客厅主灯)
- light002 (卧室台灯)  
- temp001 (客厅温度传感器)
- temp002 (卧室温度传感器)
- switch001 (客厅总开关)
- switch002 (厨房插座)

### 3. LLM配置

配置合适的大语言模型以支持中文自然语言理解：
- 推荐使用 GPT-4 或类似模型
- 确保API密钥正确配置
- 可选择本地部署的模型

## 注意事项

1. **API连接**: 确保Virtual Device Hub API服务正常运行
2. **设备ID匹配**: 设备ID必须与API中的设备ID完全匹配
3. **网络连接**: 确保GAgent能够访问设备API端点
4. **LLM配置**: 需要配置合适的大语言模型以支持自然语言理解
5. **错误处理**: 命令执行失败时会返回详细错误信息
6. **性能考虑**: 大量设备时建议调整状态更新频率

## 故障排除

### 常见问题

1. **设备无法注册**
   - 检查设备ID是否正确
   - 确认API服务是否运行
   - 验证网络连接

2. **自然语言命令无响应**
   - 检查LLM配置是否正确
   - 确认API密钥有效
   - 查看日志获取详细错误信息

3. **设备状态不更新**
   - 检查设备是否在线
   - 确认API连接状态
   - 查看设备监控日志

### 日志级别

建议设置适当的日志级别以便调试：
- `Information`: 正常操作日志
- `Warning`: 设备连接问题
- `Error`: 命令执行失败
- `Debug`: 详细的设备状态更新
