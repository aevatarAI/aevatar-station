# Aevatar.GAgents.GroupChat - 工作流编排框架

## 概述

Aevatar.GAgents.GroupChat 是一个基于 Orleans 构建的强大工作流编排框架，通过分布式代理架构实现复杂工作流的创建和执行。它为构建类似 Dify 或 LangFlow 的企业级工作流系统提供了灵活且可扩展的解决方案。

## 核心概念

### 1. WorkflowCoordinatorGAgent（工作流协调器）
管理整个工作流生命周期的中央协调器：
- **状态管理**：跟踪工作流状态（待定 → 进行中 → 已完成/失败）
- **拓扑管理**：确保所有路径可到达终端节点（无死循环）
- **动态配置**：支持运行时修改工作流结构
- **事件驱动协调**：使用 Orleans 流进行实时协调

### 2. WorkUnit（工作单元/节点）
工作流中的独立处理单元：
- 每个 WorkUnit 是执行特定任务的 GAgent
- 可以是任何继承自 `GroupMemberGAgentBase` 的 GAgent
- 支持同步和异步执行
- 可产生供下游节点使用的输出

### 3. 黑板模式（Blackboard Pattern）
工作流的共享数据上下文：
- **BlackboardGAgent**：工作流执行的集中数据存储
- 实现非连接节点之间的数据共享
- 维护对话历史和中间结果
- 线程安全且分布式设计

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                   WorkflowCoordinatorGAgent                 │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ • 工作流状态管理                                     │  │
│  │ • 拓扑验证                                          │  │
│  │ • 事件分发                                          │  │
│  │ • 进度跟踪                                          │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────┬───────────────────────────────────────┘
                      │ 协调
     ┌────────────────┴────────────────┬─────────────────┐
     ▼                                 ▼                 ▼
┌─────────┐                      ┌─────────┐       ┌─────────┐
│工作单元1│                      │工作单元2│       │工作单元3│
│(GAgent) │──────发布事件────────▶│(GAgent) │       │(GAgent) │
└────┬────┘                      └────┬────┘       └────┬────┘
     │                                 │                 │
     └─────────────┬───────────────────┴─────────────────┘
                   ▼
           ┌──────────────┐
           │    黑板      │
           │   GAgent     │
           └──────────────┘
```

## 工作流执行流程

### 1. 初始化阶段
```csharp
// 配置工作流单元
var config = new WorkflowCoordinatorConfigDto
{
    WorkflowUnitList = new List<WorkflowUnitDto>
    {
        new() { GrainId = "unit1", NextGrainId = "unit2" },
        new() { GrainId = "unit2", NextGrainId = "unit3" },
        new() { GrainId = "unit3", NextGrainId = null } // 终端节点
    }
};

// 应用配置
await coordinator.ConfigAsync(config);
```

### 2. 执行阶段
1. **启动事件**：`StartWorkflowCoordinatorEvent` 触发工作流执行
2. **节点激活**：顶层节点（无上游依赖）首先激活
3. **数据流**：节点通过黑板读取上游结果
4. **进度跟踪**：每个节点通过 `ChatResponseEvent` 报告完成状态
5. **下游激活**：已完成节点触发其下游依赖
6. **完成检测**：所有终端节点完成时工作流结束

### 3. 事件流
```
StartWorkflowCoordinatorEvent
    ↓
ChatEvent (发送到工作单元)
    ↓
工作单元处理
    ↓
ChatResponseEvent (来自工作单元)
    ↓
CoordinatorConfirmChatResponse (发送到黑板)
    ↓
下一个工作单元激活 或 WorkflowFinishEvent
```

## 主要特性

### 1. 循环检测
框架自动检测并防止无限循环：
```csharp
public bool IsAllPathsCanReachTerminal(List<WorkflowUnitDto> workflowUnits)
{
    // 从终端节点进行反向图遍历
    // 确保所有节点至少可以到达一个终端
}
```

### 2. 并行执行
没有依赖关系的多个节点可以同时执行：
- 独立分支并行运行
- 在连接点自动同步
- 高效的资源利用

### 3. 动态重配置
工作流可以在运行时修改：
- 添加/删除节点
- 更改连接
- 热插拔实现

### 4. 状态持久化
所有工作流状态使用 Orleans 的事件溯源持久化：
- 故障自动恢复
- 完整的审计跟踪
- 时间旅行调试能力

## 使用示例

### 1. 定义自定义工作单元
```csharp
[GAgent]
public class DataProcessorGAgent : GroupMemberGAgentBase<DataProcessorState, DataProcessorLogEvent, EventBase, GroupMemberConfigDto>
{
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        // 处理输入数据
        var inputData = coordinatorMessages?.LastOrDefault()?.Content;
        var result = await ProcessDataAsync(inputData);
        
        return new ChatResponse
        {
            Content = result,
            Continue = true, // 信号继续工作流
            Skip = false
        };
    }
    
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(100); // 总是对执行感兴趣
    }
}
```

### 2. 创建并启动工作流
```csharp
// 获取协调器实例
var coordinator = grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);

// 配置工作流
var units = new List<WorkflowUnitDto>
{
    new() { GrainId = "数据获取器", NextGrainId = "数据处理器" },
    new() { GrainId = "数据处理器", NextGrainId = "数据保存器" },
    new() { GrainId = "数据保存器", NextGrainId = null }
};

await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto 
{ 
    WorkflowUnitList = units,
    InitContent = "开始处理客户数据"
});

// 启动执行
await coordinator.PublishAsync(new StartWorkflowCoordinatorEvent());
```

### 3. 监控进度
```csharp
// 订阅工作流事件
var stream = streamProvider.GetStream<EventBase>("workflow-events", workflowId);
await stream.SubscribeAsync(async (evt, token) =>
{
    switch (evt)
    {
        case ChatResponseEvent response:
            Console.WriteLine($"节点 {response.MemberName} 已完成");
            break;
        case WorkflowFinishEvent:
            Console.WriteLine("工作流已完成！");
            break;
    }
});
```

## 高级模式

### 1. 条件分支
```csharp
protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
{
    var condition = EvaluateCondition(messages);
    
    return new ChatResponse
    {
        Content = JsonSerializer.Serialize(new { branch = condition ? "A" : "B" }),
        Continue = true,
        // 使用 ExtendedData 来指示采取哪个分支
        ExtendedData = new Dictionary<string, object> { ["nextNode"] = condition ? "nodeA" : "nodeB" }
    };
}
```

### 2. 人工审批流程
```csharp
public class ApprovalGAgent : GroupMemberGAgentBase<ApprovalState, ApprovalLogEvent, EventBase, GroupMemberConfigDto>
{
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        // 发送审批请求
        await SendApprovalRequestAsync(messages);
        
        // 等待人工响应
        var approved = await WaitForApprovalAsync();
        
        return new ChatResponse
        {
            Content = approved ? "已批准" : "已拒绝",
            Continue = approved
        };
    }
}
```

### 3. 错误处理和重试
```csharp
protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
{
    try
    {
        var result = await ProcessWithRetryAsync(messages);
        return new ChatResponse { Content = result, Continue = true };
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "处理失败");
        return new ChatResponse 
        { 
            Content = $"错误：{ex.Message}", 
            Continue = false // 出错时停止工作流
        };
    }
}
```

## 最佳实践

1. **保持工作单元专注**：每个工作单元应该有单一职责
2. **明智使用黑板**：只存储必要的共享数据
3. **优雅处理故障**：实现适当的错误处理和恢复
4. **监控性能**：使用 Orleans Dashboard 进行监控
5. **全面测试**：对单个工作单元进行单元测试，对工作流进行集成测试

## 配置选项

### WorkflowCoordinatorConfigDto
```csharp
public class WorkflowCoordinatorConfigDto
{
    // 工作流单元及其连接列表
    public List<WorkflowUnitDto> WorkflowUnitList { get; set; }
    
    // 工作流的初始内容/上下文
    public string? InitContent { get; set; }
}
```

### WorkflowUnitDto
```csharp
public class WorkflowUnitDto
{
    // 工作单元的唯一标识符（GrainId）
    public string GrainId { get; set; }
    
    // 工作流中的下一个单元（终端节点为 null）
    public string? NextGrainId { get; set; }
    
    // 额外的配置数据
    public Dictionary<string, object>? ExtendedData { get; set; }
}
```

## 故障排除

### 常见问题

1. **工作流卡住**：检查所有节点是否正确响应 ChatEvent
2. **循环检测错误**：确保所有路径都通向终端节点
3. **节点未激活**：验证上游依赖是否正在完成
4. **数据未共享**：确认黑板正在正确更新

### 调试技巧

1. 启用详细日志记录：
```csharp
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
```

2. 使用 Orleans Dashboard 进行可视化监控
3. 实现自定义事件处理程序以进行详细跟踪
4. 检查 grain 激活日志以了解初始化问题

## 性能考虑

1. **Grain 激活**：通过重用实例来最小化 grain 激活开销
2. **消息大小**：保持消息较小；对大数据使用引用
3. **并行性**：设计工作流以最大化并行执行
4. **状态大小**：限制状态大小以提高持久化性能

## 总结

Aevatar.GAgents.GroupChat 为构建复杂的工作流系统提供了坚实的基础。其事件驱动架构与 Orleans 的分布式计算能力相结合，使其适合企业级工作流编排需求。 