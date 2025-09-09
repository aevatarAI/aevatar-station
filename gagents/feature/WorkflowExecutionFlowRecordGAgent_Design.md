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

基于原有的 `WorkflowRunRecordState`，采用"点+数据来源"设计：

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
    
    // 核心：纯"点"记录，专注于单个Agent节点的执行过程
    [Id(6)] public List<WorkUnitExecutionRecord> ExecutionRecords { get; set; } = new();
    
    // 新增：事件流转记录
    [Id(7)] public List<EventFlowRecord> EventFlows { get; set; } = new();
}

[GenerateSerializer]
public class WorkUnitExecutionRecord  // 专注于记录单个Agent节点
{
    // 节点标识
    [Id(0)] public string AgentGrainId { get; set; }
    [Id(1)] public string AgentType { get; set; }
    
    // 数据来源（记录数据血缘关系）
    [Id(2)] public List<string> BeforeAgentIds { get; set; } = new();
    
    // 执行时间
    [Id(3)] public DateTime? StartTime { get; set; }
    [Id(4)] public DateTime? EndTime { get; set; }
    [Id(5)] public WorkflowExecutionStatus Status { get; set; }
    
    // 节点数据（JSON字符串格式，避免重复）
    [Id(6)] public string InputDataJson { get; set; } = string.Empty;
    [Id(7)] public string OutputDataJson { get; set; } = string.Empty;
    
    // 实时状态快照（根据执行阶段动态更新）
    [Id(8)] public string CurrentStateJson { get; set; } = string.Empty;
    
    // 执行统计
    [Id(9)] public int RetryCount { get; set; } = 0;
    [Id(10)] public List<string> ErrorMessages { get; set; } = new();
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
    // 开始执行WorkUnit时：
    // 1. 记录输入数据
    // 2. 捕获执行前的实时状态
    // 3. 记录事件流转
    
    RaiseEvent(new StartExecuteWorkUnitLogEvent
    {
        WorkUnitGrainId = @event.Speaker.ToString(),
        BeforeAgentIds = GetBeforeAgentIds(@event.Speaker),
        InputDataJson = JsonConvert.SerializeObject(@event.CoordinatorMessages),
        CurrentStateJson = await CaptureAgentStateAsync(@event.Speaker), // 执行前状态
        Status = WorkflowExecutionStatus.Running
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
    // 完成执行WorkUnit时：
    // 1. 记录输出数据
    // 2. 更新为执行后的实时状态
    // 3. 记录事件流转
    
    RaiseEvent(new FinishExecuteWorkUnitLogEvent
    {
        WorkUnitGrainId = @event.PublisherGrainId.ToString(),
        OutputDataJson = JsonConvert.SerializeObject(@event.ChatResponse?.Content),
        CurrentStateJson = await CaptureAgentStateAsync(@event.PublisherGrainId), // 执行后状态
        Status = WorkflowExecutionStatus.Completed,
        EndTime = DateTime.UtcNow
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

## WorkUnitExecutionRecord 便利方法

为了方便使用JSON数据，提供便利方法：

```csharp
public partial class WorkUnitExecutionRecord
{
    /// <summary>
    /// 获取结构化输入参数（按需解析）
    /// </summary>
    public Dictionary<string, object>? GetInputParameters()
    {
        if (string.IsNullOrEmpty(InputDataJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(InputDataJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// 获取结构化输出参数（按需解析）
    /// </summary>
    public Dictionary<string, object>? GetOutputParameters()
    {
        if (string.IsNullOrEmpty(OutputDataJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(OutputDataJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// 获取当前Agent状态（按需解析）
    /// 根据执行阶段，可能是执行前状态或执行后状态
    /// </summary>
    public T? GetCurrentState<T>() where T : class
    {
        if (string.IsNullOrEmpty(CurrentStateJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<T>(CurrentStateJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// 获取执行持续时间
    /// </summary>
    public TimeSpan? GetDuration()
    {
        if (!StartTime.HasValue || !EndTime.HasValue) return null;
        return EndTime.Value - StartTime.Value;
    }
}
```

## 辅助工具方法

GAgent中需要实现的辅助方法：

```csharp
// 捕获Agent状态快照  
private async Task<string> CaptureAgentStateAsync(GrainId agentGrainId)
{
    try
    {
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

// 获取上游Agent列表
private List<string> GetBeforeAgentIds(GrainId currentAgentId)
{
    // 从WorkflowCoordinator状态中获取上游节点
    var upstreamGrains = State.GetUpStreamGrainIds(currentAgentId.ToString());
    return upstreamGrains.ToList();
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
    public bool CaptureAgentStates { get; set; } = false;    // 是否捕获Agent状态快照
    public bool CaptureErrorDetails { get; set; } = true;    // 是否记录错误详情
}
```

## 总结

基于原有 `WorkflowRunRecordGAgent` 设计的优化方案：

### ✅ 核心设计理念
- **"点"记录模式** - 专注于记录单个Agent节点的执行过程，不关心连接关系
- **数据血缘追踪** - 通过 BeforeAgentIds 记录数据来源，便于调试追溯
- **JSON字符串存储** - 避免数据重复，按需解析，存储高效

### ✅ 保持不变的优点
- **非侵入性架构** - 通过事件监听，不修改现有逻辑
- **简洁的数据结构** - 避免复杂的嵌套对象
- **配置化控制** - 通过 EnableRunRecord 等配置控制功能

### ✅ 新增的功能  
1. **数据来源追踪** - BeforeAgentIds 记录Agent的数据来源
2. **JSON数据存储** - InputDataJson/OutputDataJson 统一存储
3. **实时状态快照** - CurrentStateJson 根据执行阶段动态更新，更关注当前状态
4. **便利解析方法** - 提供按需解析的便利方法
5. **执行统计信息** - RetryCount、ErrorMessages等

### ✅ 实现优势
1. **存储效率** - 避免String和Dictionary的重复存储，单一状态避免前后状态重复
2. **调试友好** - JSON格式易于查看和分析
3. **灵活性强** - 可以存储任何结构的数据
4. **性能优化** - 按需解析，不是每次都解析
5. **类型安全** - 提供泛型解析方法
6. **实时性强** - CurrentStateJson 反映当前执行阶段的真实状态，更有实际意义

### ✅ 使用场景示例
```csharp
// 调试场景：追踪数据流向和实时状态
var record = records.First(r => r.AgentGrainId == "AgentC");

// 数据血缘追踪
var sources = record.BeforeAgentIds; // 数据来源: ["AgentA", "AgentB"]

// 按需解析数据
var inputParams = record.GetInputParameters(); // 输入参数
var outputParams = record.GetOutputParameters(); // 输出参数

// 实时状态查看
var currentState = record.GetCurrentState<MyAgentState>(); // 当前状态
var isCompleted = record.Status == WorkflowExecutionStatus.Completed; // 是否完成

// 性能分析
var duration = record.GetDuration(); // 执行时长
var retryCount = record.RetryCount; // 重试次数

// 状态含义：
// - 如果Status是Running，CurrentStateJson是执行前状态
// - 如果Status是Completed，CurrentStateJson是执行后状态
Console.WriteLine($"Agent {record.AgentGrainId} 当前状态: {record.Status}");
Console.WriteLine($"状态快照时间: {(isCompleted ? record.EndTime : record.StartTime)}");
```

这个优化方案既满足了记录event流转方向、出入参数和agent state的需求，又保持了原有设计的简洁性和高效性。
