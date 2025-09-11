# Aevatar.GAgents.Device

基于Aevatar框架的物联网设备GAgent实现，提供智能家居和物联网设备的统一管理和AI控制能力。

## 🌟 概述

DeviceGAgent是一个专门为物联网设备设计的GAgent框架，它将物理或虚拟设备包装成Orleans Grain，并通过Semantic Kernel将设备操作注册为AI工具函数，让LLM能够通过自然语言控制设备。

### 核心特性

- 🔌 **设备连接抽象**: 统一的设备连接接口，支持真实和虚拟设备
- 🤖 **AI集成**: 自动将设备操作注册为Semantic Kernel函数
- 📊 **状态管理**: 基于Orleans事件溯源的设备状态管理
- 🔄 **实时监控**: 设备属性变化的实时监控和事件发布
- ⚡ **事件驱动**: 完整的事件驱动架构，支持设备间协调
- 🛡️ **容错处理**: 连接断开重连、错误恢复等机制
- 📈 **性能监控**: 设备健康状态检查和性能统计

## 🏗️ 架构设计

```
DeviceGAgentBase<TDeviceConnection>
    ├── IDeviceConnection (设备连接抽象)
    ├── DeviceGAgentState (设备状态管理)
    ├── DeviceEvents (设备事件定义)
    └── Semantic Kernel Integration (AI工具注册)
```

### 继承层次

```
AIGAgentBase<TState, TStateLogEvent>
    └── DeviceGAgentBase<TDeviceConnection>
            └── SmartLightGAgent (智能灯泡)
            └── TemperatureSensorGAgent (温度传感器)
            └── [其他设备GAgent...]
```

## 🚀 快速开始

### 1. 创建设备GAgent

```csharp
// 1. 定义设备接口
public interface IMyDeviceGAgent : IDeviceGAgent<VirtualDeviceConnection>
{
    Task<bool> DoSomethingAsync();
}

// 2. 实现设备GAgent
[Description("我的设备代理")]
[GAgent("my-device", "device")]
public class MyDeviceGAgent : DeviceGAgentBase<VirtualDeviceConnection>, IMyDeviceGAgent
{
    protected override async Task<VirtualDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
        return new VirtualDeviceConnection(config.DeviceId, config.DeviceName, "MyDevice", logger);
    }

    public async Task<bool> DoSomethingAsync()
    {
        if (DeviceConnection == null) return false;
        
        var result = await DeviceConnection.ExecuteActionAsync("DoSomething");
        return result.IsSuccess;
    }
}
```

### 2. 初始化设备连接

```csharp
// 创建设备配置
var config = new DeviceConnectionConfig
{
    DeviceId = "device-001",
    DeviceName = "我的设备",
    DeviceType = "MyDevice",
    ConnectionString = "localhost:1234",
    ConnectionTimeoutSeconds = 30
};

// 获取设备GAgent实例
var deviceAgent = await gAgentFactory.GetGAgentAsync<IMyDeviceGAgent>(Guid.NewGuid());

// 初始化设备连接
await deviceAgent.InitializeDeviceConnectionAsync(config);

// 启用设备监控
await deviceAgent.SetDeviceMonitoringAsync(true);
```

### 3. 使用AI控制设备

```csharp
// 初始化AI GAgent并配置设备工具
var aiAgent = await gAgentFactory.GetGAgentAsync<IAIGAgent>(Guid.NewGuid());

await aiAgent.InitializeAsync(new InitializeDto
{
    Instructions = "你是一个智能家居助手，可以帮助用户控制各种设备",
    LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
    EnableGAgentTools = true,
    ToolGAgentTypes = new List<GrainType> { typeof(MyDeviceGAgent).ToGrainType() }
});

// AI现在可以通过自然语言控制设备
var response = await aiAgent.ChatAsync(new ChatRequestDto
{
    Prompt = "请帮我打开客厅的灯并调到50%亮度",
    ChatId = Guid.NewGuid().ToString()
});
```

## 📱 示例设备

### 智能灯泡 (SmartLightGAgent)

```csharp
// 获取智能灯泡GAgent
var smartLight = await gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());

// 初始化连接
await smartLight.InitializeDeviceConnectionAsync(new DeviceConnectionConfig
{
    DeviceId = "light-001",
    DeviceName = "客厅灯泡",
    DeviceType = "SmartLight"
});

// 控制灯泡
await smartLight.TurnOnAsync();              // 开灯
await smartLight.SetBrightnessAsync(75);     // 设置亮度75%
await smartLight.SetColorAsync("#FF0000");   // 设置红色
var status = await smartLight.GetLightStatusAsync(); // 获取状态
```

### 温度传感器 (TemperatureSensorGAgent)

```csharp
// 获取温度传感器GAgent
var tempSensor = await gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());

// 初始化连接
await tempSensor.InitializeDeviceConnectionAsync(new DeviceConnectionConfig
{
    DeviceId = "temp-001",
    DeviceName = "客厅温度传感器",
    DeviceType = "TemperatureSensor"
});

// 读取数据
var temperature = await tempSensor.GetTemperatureAsync();    // 获取温度
var humidity = await tempSensor.GetHumidityAsync();          // 获取湿度
var reading = await tempSensor.GetReadingAsync();            // 获取完整读数
var history = await tempSensor.GetHistoryAsync(24);         // 获取24小时历史
```

## 🔧 自定义设备连接

### 实现真实设备连接

```csharp
public class MyRealDeviceConnection : IDeviceConnection
{
    public string DeviceId { get; private set; }
    public string DeviceName { get; private set; }
    public string DeviceType { get; private set; }
    public DeviceConnectionStatus Status { get; private set; }
    
    // 实现真实的设备通信逻辑
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        // 连接到真实设备（如TCP、HTTP、MQTT等）
        // 返回连接结果
    }
    
    public async Task<object?> ReadPropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        // 从真实设备读取属性
    }
    
    // ... 实现其他接口方法
}
```

## 📊 事件系统

### 设备事件

设备GAgent支持丰富的事件系统：

```csharp
// 监听设备属性变化
await deviceAgent.SubscribeToAsync(propertyChangeHandler);

// 发布自定义设备事件
await deviceAgent.PublishAsync(new DevicePropertyChangedNotificationEvent
{
    DeviceId = "device-001",
    PropertyName = "Temperature",
    NewValueJson = "25.5"
});
```

### 支持的事件类型

- `ReadDevicePropertyEvent` - 读取设备属性
- `WriteDevicePropertyEvent` - 写入设备属性  
- `ExecuteDeviceActionEvent` - 执行设备操作
- `DevicePropertyChangedNotificationEvent` - 属性变化通知
- `DeviceConnectionStatusChangedNotificationEvent` - 连接状态变化
- `DeviceErrorEvent` - 设备错误
- `EnvironmentAlertEvent` - 环境警告

## 🛠️ 配置选项

### 设备连接配置

```csharp
var config = new DeviceConnectionConfig
{
    DeviceId = "unique-device-id",
    DeviceName = "用户友好的设备名称",
    DeviceType = "设备类型标识",
    ConnectionString = "设备连接字符串",
    ConnectionTimeoutSeconds = 30,
    RetryAttempts = 3,
    RetryIntervalSeconds = 5,
    ExtendedProperties = new Dictionary<string, string>
    {
        ["CustomProperty1"] = "Value1",
        ["CustomProperty2"] = "Value2"
    }
};
```

### 监控配置

```csharp
// 设置监控间隔
State.MonitoringIntervalSeconds = 60; // 60秒检查一次

// 启用/禁用监控
await deviceAgent.SetDeviceMonitoringAsync(true);
```

## 🧪 测试

### 单元测试示例

```csharp
[Test]
public async Task SmartLight_TurnOn_ShouldWork()
{
    // Arrange
    var smartLight = await GAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
    await smartLight.InitializeDeviceConnectionAsync(CreateTestConfig());
    
    // Act
    var result = await smartLight.TurnOnAsync();
    
    // Assert
    Assert.IsTrue(result);
    var status = await smartLight.GetLightStatusAsync();
    Assert.IsTrue(status.IsOn);
}
```

## 🔍 故障排除

### 常见问题

1. **设备连接失败**
   - 检查设备配置是否正确
   - 验证网络连接
   - 查看日志中的错误信息

2. **属性读写失败**
   - 确认属性名称正确
   - 检查属性的可读/可写权限
   - 验证数据类型匹配

3. **AI工具调用失败**
   - 确认设备GAgent已注册为工具
   - 检查Semantic Kernel配置
   - 验证函数参数格式

### 日志级别

```csharp
// 在appsettings.json中配置日志级别
{
  "Logging": {
    "LogLevel": {
      "Aevatar.GAgents.Device": "Information",
      "Aevatar.GAgents.Device.Connections": "Debug"
    }
  }
}
```

## 🚀 扩展开发

### 添加新的设备类型

1. 继承 `DeviceGAgentBase<TDeviceConnection>`
2. 实现 `CreateDeviceConnectionAsync` 方法
3. 添加设备特定的业务方法
4. 注册为GAgent服务

### 集成第三方协议

1. 实现 `IDeviceConnection` 接口
2. 添加协议特定的连接逻辑
3. 处理协议消息和状态同步
4. 实现错误处理和重连机制

## 📄 许可证

本项目采用 MIT 许可证。详情请参阅 [LICENSE](LICENSE) 文件。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如有问题，请通过以下方式联系：

- 提交 GitHub Issue
- 发送邮件至 support@aevatar.com
- 查看文档：[Aevatar Documentation](https://docs.aevatar.com)
