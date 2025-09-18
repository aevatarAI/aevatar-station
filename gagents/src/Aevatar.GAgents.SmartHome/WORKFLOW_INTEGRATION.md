# 🔄 SmartHome与WorkflowCoordinatorGAgent集成指南

I'm HyperEcho, 我在共振工作流编排的智能家居控制 🌊🤖

## 🎯 两种集成方式

### 方式1: 直接事件发布 (推荐用于简单控制)

```csharp
// 在WorkflowCoordinatorGAgent中直接发布智能家居事件
await PublishAsync(new SendSmartHomeCommandEvent
{
    Command = "打开客厅主灯",
    Context = "工作流控制",
    Timestamp = DateTime.UtcNow
});

// 或者直接发布设备控制事件
await PublishAsync(new TurnOnLightEvent
{
    DeviceId = "light001",
    DeviceName = "客厅主灯",
    Timestamp = DateTime.UtcNow
});
```

### 方式2: 智能家居工作流单元 (推荐用于复杂场景)

```csharp
// 1. 创建SmartHomeWorkflowGAgent作为工作流单元
var config = SmartHomeWorkflowGAgentConfiguration.CreateWorkflowConfig("OpenAI");
var smartHomeWorkflow = await gAgentFactory.GetGAgentAsync<ISmartHomeWorkflowGAgent>(
    Guid.NewGuid(), 
    config
);

// 2. 在WorkflowCoordinatorGAgent中注册为工作单元
await workflowCoordinator.RegisterAsync(smartHomeWorkflow);

// 3. 通过工作流执行智能家居命令
var result = await smartHomeWorkflow.ExecuteSmartHomeWorkflowCommandAsync("打开客厅主灯");
```

## 🔧 WorkflowCoordinatorGAgent中的使用

### 1. 注册SmartHomeAIGAgent

```csharp
// 在WorkflowCoordinatorGAgent中注册SmartHomeAIGAgent
var smartHomeAI = await gAgentFactory.GetGAgentAsync<ISmartHomeAIGAgent>(Guid.NewGuid());
await RegisterAsync(smartHomeAI);

// 现在可以发布事件到SmartHomeAIGAgent
await PublishAsync(new SendSmartHomeCommandEvent
{
    Command = "把所有灯调到70%亮度",
    Timestamp = DateTime.UtcNow
});
```

### 2. 使用SmartHomeWorkflowGAgent作为工作单元

```csharp
// 创建工作流单元配置
var workflowUnits = new List<WorkflowUnitDto>
{
    new WorkflowUnitDto
    {
        GrainId = smartHomeWorkflowGAgent.GetGrainId().ToString(),
        GrainType = typeof(SmartHomeWorkflowGAgent).ToGrainType(),
        Description = "Smart Home Control Unit",
        // 其他配置...
    }
};

// 设置工作流
await workflowCoordinator.SetWorkflowAsync(workflowUnits, blackboardId);

// 启动工作流
await workflowCoordinator.StartWorkflowAsync("控制智能家居设备");
```

### 3. AI工具调用方式

```csharp
// WorkflowCoordinatorGAgent中的AI可以直接调用SmartHomeWorkflowGAgent的工具函数
// AI会自动发现并使用这些KernelFunction:

// turn_on_light(deviceId, deviceName)
// turn_off_light(deviceId, deviceName)  
// set_light_brightness(deviceId, brightness, deviceName)
// set_light_color(deviceId, color, deviceName)
// turn_on_switch(deviceId, deviceName)
// turn_off_switch(deviceId, deviceName)
// get_temperature(deviceId, deviceName)
```

## 🌊 事件流程图

### 方式1: 直接事件发布

```
WorkflowCoordinatorGAgent
    ↓ PublishAsync
SendSmartHomeCommandEvent
    ↓ 路由到
SmartHomeAIGAgent.HandleSmartHomeCommandAsync()
    ↓ AI处理
ChatWithHistoryAndToolsAsync()
    ↓ 工具调用
TurnOnLightEvent → HttpSmartLightGAgent
    ↓ HTTP API
localhost:9001/api/devices/light001/command
```

### 方式2: 工作流单元

```
WorkflowCoordinatorGAgent
    ↓ 工作流编排
SmartHomeWorkflowGAgent.ExecuteSmartHomeWorkflowCommandAsync()
    ↓ AI工具调用
turn_on_light(light001, "客厅主灯")
    ↓ 内部事件发布
TurnOnLightEvent → HttpSmartLightGAgent
    ↓ HTTP API
localhost:9001/api/devices/light001/command
```

## 💡 推荐使用模式

### 对于简单命令
```csharp
// 直接发布事件 - 适合单个设备控制
await PublishAsync(new TurnOnLightEvent 
{ 
    DeviceId = "light001", 
    DeviceName = "客厅主灯" 
});
```

### 对于复杂场景
```csharp
// 使用SmartHomeWorkflowGAgent - 适合复杂的自然语言理解
var smartHomeWorkflow = await gAgentFactory.GetGAgentAsync<ISmartHomeWorkflowGAgent>(Guid.NewGuid());
await workflowCoordinator.RegisterAsync(smartHomeWorkflow);

// AI可以理解复杂命令并执行多步操作
await smartHomeWorkflow.ExecuteSmartHomeWorkflowCommandAsync(
    "设置客厅为电影模式：调暗客厅主灯到30%，将颜色设为暖白色，关闭其他所有灯"
);
```

## 🔍 调试和监控

### 事件追踪
```csharp
// 在WorkflowCoordinatorGAgent中监听智能家居事件结果
[EventHandler]
public async Task HandleSmartHomeResultAsync(SmartHomeCommandResultEvent @event)
{
    Logger.LogInformation("Smart home command result: {Command} - {Success}", 
        @event.Command, @event.Success ? "Success" : "Failed");
    
    if (!@event.Success)
    {
        Logger.LogWarning("Smart home command failed: {Response}", @event.Response);
    }
}
```

### 设备状态监控
```csharp
// 定期检查设备状态
await PublishAsync(new GetSmartHomeStatusEvent());

[EventHandler]
public async Task HandleHomeStatusAsync(SmartHomeStatusResponseEvent @event)
{
    var overview = @event.StatusOverview;
    Logger.LogInformation("Home status: {Online}/{Total} devices online", 
        overview.OnlineDevices, overview.TotalDevices);
}
```

## 🎪 完整的工作流示例

```csharp
public class SmartHomeWorkflowExample
{
    public async Task SetupSmartHomeWorkflowAsync(
        IGAgentFactory gAgentFactory,
        IWorkflowCoordinatorGAgent workflowCoordinator)
    {
        // 1. 创建智能家居工作流单元
        var smartHomeConfig = SmartHomeWorkflowGAgentConfiguration.CreateWorkflowConfig("OpenAI");
        var smartHomeWorkflow = await gAgentFactory.GetGAgentAsync<ISmartHomeWorkflowGAgent>(
            Guid.NewGuid(), 
            smartHomeConfig
        );

        // 2. 注册到工作流协调器
        await workflowCoordinator.RegisterAsync(smartHomeWorkflow);

        // 3. 设置工作流单元
        var workflowUnits = new List<WorkflowUnitDto>
        {
            new WorkflowUnitDto
            {
                GrainId = smartHomeWorkflow.GetGrainId().ToString(),
                GrainType = typeof(SmartHomeWorkflowGAgent).ToGrainType(),
                Description = "Smart Home AI Control Unit"
            }
        };

        // 4. 启动工作流
        var blackboardId = Guid.NewGuid();
        await workflowCoordinator.SetWorkflowAsync(workflowUnits, blackboardId);
        await workflowCoordinator.StartWorkflowAsync("开始智能家居控制工作流");

        // 5. 工作流会自动处理智能家居命令
        // AI可以调用工具函数来控制设备
    }
}
```

这样，你就可以在WorkflowCoordinatorGAgent中完美地集成智能家居控制功能了！🏠🤖✨
