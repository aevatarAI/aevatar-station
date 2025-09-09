# WorkflowRunRecordGAgent 优化设计文档

## 背景

基于现有的 `WorkflowRunRecordGAgent` 技术方案，我们需要适度优化来满足新的需求：
- 记录 event 的流转方向（从哪个Agent到哪个Agent）
- 记录更详细的出入参数信息
- 记录执行完当时对应 node 内 agent 的 state

设计原则：**保持原有架构的简洁性和非侵入性**，只在必要的地方增加功能。

## 现状分析

### 原有设计优势
原有的 `WorkflowRunRecordGAgent` 设计非常优雅：
- **非侵入性**：通过事件监听，不修改现有工作流逻辑
- **简洁性**：数据结构简单清晰
- **可配置**：通过 `EnableRunRecord` 控制是否启用
- **事件驱动**：基于现有事件系统

### 需要增强的部分
在保持简洁性的前提下，增加以下功能：
1. **Event流转记录**：记录事件从哪个Agent到哪个Agent
2. **参数详情**：结构化记录输入输出参数
3. **Agent State**：记录node执行前后的agent状态变化

## 优化设计方案

### 保持原有架构不变
继续使用原有的：
- WorkflowRunRecordGAgent 作为记录服务
- 通过 RegisterAsync 注册机制
- 监听现有事件：StartWorkflowCoordinatorEvent, ChatEvent, ChatResponseEvent
- 通过 EnableRunRecord 配置控制

### 优化的 State 结构

基于原有的 `WorkflowRunRecordState`，适度增加字段：

```csharp
[GenerateSerializer]
public class WorkflowRunRecordState : StateBase  // 保持原有命名
{
    // 原有字段保持不变
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long Term { get; set; }
    [Id(2)] public DateTime StartTime { get; set; }
    [Id(3)] public DateTime? EndTime { get; set; }
    [Id(4)] public WorkflowRunStatus Status { get; set; }
    [Id(5)] public string? InitContent { get; set; }
    [Id(6)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new();
    [Id(7)] public List<WorkUnitExecutionRecord> WorkUnitRecords { get; set; } = new();
    
    // 新增：事件流转记录
    [Id(8)] public List<EventFlowRecord> EventFlows { get; set; } = new();
}

[GenerateSerializer]
public class WorkUnitExecutionRecord  // 保持原有命名
{
    // 原有字段保持不变
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public long Term { get; set; }
    [Id(2)] public DateTime StartTime { get; set; }
    [Id(3)] public DateTime? EndTime { get; set; }
    [Id(4)] public ExecutionStatus Status { get; set; }
    [Id(5)] public string InputData { get; set; }
    [Id(6)] public string OutputData { get; set; }
    
    // 新增：结构化参数记录
    [Id(7)] public Dictionary<string, object> InputParameters { get; set; } = new();
    [Id(8)] public Dictionary<string, object> OutputParameters { get; set; } = new();
    
    // 新增：Agent状态快照（简化版）
    [Id(9)] public string PreExecutionStateJson { get; set; } = string.Empty;
    [Id(10)] public string PostExecutionStateJson { get; set; } = string.Empty;
}

// 新增：事件流转记录（简化版）
[GenerateSerializer]
public class EventFlowRecord
{
    [Id(0)] public string EventType { get; set; }
    [Id(1)] public DateTime Timestamp { get; set; }
    [Id(2)] public string FromAgentId { get; set; }
    [Id(3)] public string ToAgentId { get; set; }
    [Id(4)] public string EventData { get; set; }
}

// 保持原有枚举
public enum WorkflowRunStatus
{
    Pending,
    InProgress, 
    Completed,
    Failed
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
```

### 保持原有 Event 处理方式

继续监听现有的事件，不需要新的Event结构：
- `StartWorkflowCoordinatorEvent` - 创建运行记录
- `ChatEvent` - 记录WorkUnit输入和事件流转
- `ChatResponseEvent` - 记录WorkUnit输出和事件流转  
- `GroupChatFinishEvent` - 完成记录

### 优化的事件处理逻辑

在现有的事件处理器中增加功能：

```csharp
[EventHandler]
public async Task HandleEventAsync(ChatEvent @event)
{
    // 原有逻辑：记录输入
    // 新增逻辑：
    // 1. 记录事件流转（从 CoordinatorGAgent 到 WorkUnit）
    // 2. 解析并记录结构化参数
    // 3. 捕获WorkUnit的Pre-execution state
    
    RaiseEvent(new StartExecuteWorkUnitLogEvent
    {
        // 原有字段
        WorkUnitGrainId = @event.Speaker.ToString(),
        InputData = JsonConvert.SerializeObject(@event.CoordinatorMessages),
        
        // 新增字段
        InputParameters = ExtractParameters(@event.CoordinatorMessages),
        PreExecutionStateJson = await CaptureAgentStateAsync(@event.Speaker)
    });
    
    // 记录事件流转
    RaiseEvent(new EventFlowLogEvent
    {
        EventType = "ChatEvent",
        FromAgentId = this.GetGrainId().ToString(), // Coordinator
        ToAgentId = @event.Speaker.ToString(),      // WorkUnit
        Timestamp = DateTime.UtcNow,
        EventData = JsonConvert.SerializeObject(@event, JsonSettings)
    });
    
    await ConfirmEvents();
}

[EventHandler] 
public async Task HandleEventAsync(ChatResponseEvent @event)
{
    // 原有逻辑：记录输出
    // 新增逻辑：
    // 1. 记录事件流转（从 WorkUnit 到 CoordinatorGAgent）
    // 2. 解析并记录结构化输出参数
    // 3. 捕获WorkUnit的Post-execution state
    
    RaiseEvent(new FinishExecuteWorkUnitLogEvent
    {
        // 原有字段  
        WorkUnitGrainId = @event.PublisherGrainId.ToString(),
        OutputData = JsonConvert.SerializeObject(@event.ChatResponse?.Content),
        
        // 新增字段
        OutputParameters = ExtractOutputParameters(@event.ChatResponse),
        PostExecutionStateJson = await CaptureAgentStateAsync(@event.PublisherGrainId)
    });
    
    // 记录事件流转
    RaiseEvent(new EventFlowLogEvent  
    {
        EventType = "ChatResponseEvent",
        FromAgentId = @event.PublisherGrainId.ToString(), // WorkUnit
        ToAgentId = this.GetGrainId().ToString(),         // Coordinator
        Timestamp = DateTime.UtcNow,
        EventData = JsonConvert.SerializeObject(@event, JsonSettings)
    });
    
    await ConfirmEvents();
}
```

## 工具方法

需要实现的辅助方法：

```csharp
// 提取结构化参数
private Dictionary<string, object> ExtractParameters(List<ChatMessage>? messages)
{
    if (messages == null) return new Dictionary<string, object>();
    
    var parameters = new Dictionary<string, object>();
    foreach (var message in messages)
    {
        parameters[$"Message_{message.MessageType}"] = message.Content ?? string.Empty;
    }
    return parameters;
}

// 捕获Agent状态快照  
private async Task<string> CaptureAgentStateAsync(GrainId agentGrainId)
{
    try
    {
        // 通过反射获取Agent的状态
        var agent = GrainFactory.GetGrain<IGAgent>(agentGrainId);
        var state = await agent.GetStateAsync();
        return JsonConvert.SerializeObject(state, JsonSettings);
    }
    catch (Exception ex)
    {
        Logger.LogWarning("Failed to capture agent state for {AgentId}: {Error}", 
            agentGrainId, ex.Message);
        return $"{{\"error\": \"{ex.Message}\"}}";
    }
}
```

## 配置扩展

在 `WorkflowCoordinatorConfigDto` 中增加配置：

```csharp
public class WorkflowCoordinatorConfigDto
{
    // 现有字段...
    
    // 新增字段
    public bool EnableRunRecord { get; set; } = true;
    public bool CaptureEventFlows { get; set; } = true;      // 是否记录事件流转
    public bool CaptureAgentStates { get; set; } = false;    // 是否捕获Agent状态 
    public bool CaptureStructuredParams { get; set; } = true; // 是否记录结构化参数
}
```

## 总结

基于原有 `WorkflowRunRecordGAgent` 设计的优化方案：

### ✅ 保持不变的优点
- **非侵入性架构** - 通过事件监听，不修改现有逻辑
- **简洁的数据结构** - 在原有基础上适度扩展
- **配置化控制** - 通过 EnableRunRecord 等配置控制功能

### ✅ 新增的功能  
1. **Event流转追踪** - EventFlowRecord 记录事件的来源和目标
2. **结构化参数** - InputParameters/OutputParameters 字典记录
3. **Agent状态快照** - PreExecutionStateJson/PostExecutionStateJson 简单记录

### ✅ 实现策略
1. 保持原有的 WorkflowRunRecordGAgent 命名和架构
2. 在现有 State 结构基础上增加必要字段
3. 优化现有事件处理器，增加新功能
4. 通过配置控制新功能的启用/禁用

这个优化方案既满足了您的新需求，又保持了原有设计的简洁性和可维护性。
