# WorkflowExecutionFlowRecordGAgent 设计文档

## 背景

当前的 `WorkflowExecutionRecordGAgent` 功能有限，只能记录基本的执行状态，无法满足新的需求：
- 记录完整的事件流转方向和流程图
- 记录详细的出入参数信息
- 记录执行完当时对应node内agent的完整state
- 提供更好的工作流调试和追踪能力

为了避免破坏性变更，我们设计一个新的 `WorkflowExecutionFlowRecordGAgent` 来替代现有实现。

## 现状分析

### 当前事件流

```
StartWorkflowCoordinatorEvent → WorkflowCoordinatorGAgent
    ↓
WorkflowCoordinatorGAgent 发布 StartExecuteWorkflowEvent
    ↓
WorkflowExecutionRecordGAgent 记录工作流开始
    ↓
WorkflowCoordinatorGAgent 激活 WorkUnit 发布 ChatEvent (P2P)
    ↓
WorkflowCoordinatorGAgent 发布 StartExecuteWorkUnitEvent
    ↓
WorkflowExecutionRecordGAgent 记录工作单元开始
    ↓
WorkUnit 处理后返回 ChatResponseEvent
    ↓
WorkflowExecutionRecordGAgent 记录工作单元完成
    ↓
WorkflowCoordinatorGAgent 激活下游节点或发布 GroupChatFinishEvent
    ↓
WorkflowExecutionRecordGAgent 记录工作流完成
```

### 当前 State 结构缺陷

1. **WorkflowExecutionRecordState** 只记录基本信息，缺乏：
   - 事件流转路径追踪
   - 详细的输入输出参数
   - Node 内部 Agent 的状态快照
   - 执行上下文信息

2. **WorkUnitExecutionRecord** 只有简单的输入输出字符串，缺乏：
   - 结构化的参数记录
   - Agent state 快照
   - 执行错误和异常信息
   - 执行性能指标

## 设计方案

### 新的 State 结构

```csharp
[GenerateSerializer]
public class WorkflowExecutionFlowRecordState : StateBase
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long RoundId { get; set; }
    [Id(2)] public DateTime StartTime { get; set; }
    [Id(3)] public DateTime? EndTime { get; set; }
    [Id(4)] public WorkflowExecutionStatus Status { get; set; }
    [Id(5)] public string? InitContent { get; set; }
    
    // 增强字段
    [Id(6)] public List<WorkUnitFlowInfo> WorkUnitFlows { get; set; } = new();
    [Id(7)] public List<EventFlowRecord> EventFlows { get; set; } = new();
    [Id(8)] public Dictionary<string, object> ExecutionMetrics { get; set; } = new();
    [Id(9)] public List<ExecutionError> Errors { get; set; } = new();
    [Id(10)] public WorkflowTopology Topology { get; set; } = new();
}

[GenerateSerializer]
public class WorkUnitFlowInfo
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public string NodeName { get; set; }
    [Id(2)] public DateTime StartTime { get; set; }
    [Id(3)] public DateTime? EndTime { get; set; }
    [Id(4)] public WorkflowExecutionStatus Status { get; set; }
    
    // 增强字段 - 详细参数记录
    [Id(5)] public ParameterSnapshot InputParameters { get; set; } = new();
    [Id(6)] public ParameterSnapshot OutputParameters { get; set; } = new();
    
    // Agent State 快照
    [Id(7)] public AgentStateSnapshot PreExecutionState { get; set; } = new();
    [Id(8)] public AgentStateSnapshot PostExecutionState { get; set; } = new();
    
    // 执行上下文
    [Id(9)] public ExecutionContext Context { get; set; } = new();
    
    // 性能指标
    [Id(10)] public PerformanceMetrics Metrics { get; set; } = new();
    
    // 上下游关系
    [Id(11)] public List<string> UpstreamNodes { get; set; } = new();
    [Id(12)] public List<string> DownstreamNodes { get; set; } = new();
}

[GenerateSerializer]
public class EventFlowRecord
{
    [Id(0)] public Guid EventId { get; set; }
    [Id(1)] public string EventType { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
    [Id(3)] public string SourceGrainId { get; set; }
    [Id(4)] public string TargetGrainId { get; set; }
    [Id(5)] public string EventData { get; set; }
    [Id(6)] public EventFlowDirection Direction { get; set; }
    [Id(7)] public Dictionary<string, object> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class ParameterSnapshot
{
    [Id(0)] public List<Parameter> Structured { get; set; } = new();
    [Id(1)] public string RawJson { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class Parameter
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Type { get; set; }
    [Id(2)] public string Value { get; set; }
    [Id(3)] public int Size { get; set; }
    [Id(4)] public string? Description { get; set; }
}

[GenerateSerializer]
public class AgentStateSnapshot
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string StateJson { get; set; }
    [Id(2)] public DateTime CapturedAt { get; set; }
    [Id(3)] public Dictionary<string, object> KeyProperties { get; set; } = new();
    [Id(4)] public List<string> StateChanges { get; set; } = new();
}

[GenerateSerializer]
public class ExecutionContext
{
    [Id(0)] public Dictionary<string, string> Environment { get; set; } = new();
    [Id(1)] public List<ChatMessage> Messages { get; set; } = new();
    [Id(2)] public Dictionary<string, object> SharedData { get; set; } = new();
    [Id(3)] public string ExecutionPath { get; set; } = string.Empty;
}

[GenerateSerializer]
public class PerformanceMetrics
{
    [Id(0)] public TimeSpan ExecutionDuration { get; set; }
    [Id(1)] public TimeSpan QueueTime { get; set; }
    [Id(2)] public long MemoryUsage { get; set; }
    [Id(3)] public int RetryCount { get; set; }
    [Id(4)] public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

[GenerateSerializer]
public class ExecutionError
{
    [Id(0)] public Guid ErrorId { get; set; }
    [Id(1)] public DateTime Timestamp { get; set; }
    [Id(2)] public string ErrorType { get; set; }
    [Id(3)] public string Message { get; set; }
    [Id(4)] public string StackTrace { get; set; }
    [Id(5)] public string SourceGrainId { get; set; }
    [Id(6)] public ErrorSeverity Severity { get; set; }
    [Id(7)] public Dictionary<string, object> Context { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowTopology
{
    [Id(0)] public List<TopologyNode> Nodes { get; set; } = new();
    [Id(1)] public List<TopologyEdge> Edges { get; set; } = new();
    [Id(2)] public string GraphType { get; set; } = "DAG";
}

[GenerateSerializer]
public class TopologyNode
{
    [Id(0)] public string NodeId { get; set; }
    [Id(1)] public string NodeType { get; set; }
    [Id(2)] public Dictionary<string, object> Properties { get; set; } = new();
}

[GenerateSerializer]
public class TopologyEdge
{
    [Id(0)] public string FromNode { get; set; }
    [Id(1)] public string ToNode { get; set; }
    [Id(2)] public string EdgeType { get; set; }
    [Id(3)] public Dictionary<string, object> Properties { get; set; } = new();
}

public enum EventFlowDirection
{
    Inbound,
    Outbound,
    Internal
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
```

### 新的 Event 结构

```csharp
[GenerateSerializer]
public class WorkflowExecutionFlowLogEvent : StateLogEventBase<WorkflowExecutionFlowLogEvent>
{
}

[GenerateSerializer]
public class StartExecuteWorkflowFlowLogEvent : WorkflowExecutionFlowLogEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long RoundId { get; set; }
    [Id(2)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new();
    [Id(3)] public string? Content { get; set; }
    [Id(4)] public WorkflowTopology Topology { get; set; } = new();
    [Id(5)] public Dictionary<string, object> InitialContext { get; set; } = new();
}

[GenerateSerializer]
public class StartExecuteWorkUnitFlowLogEvent : WorkflowExecutionFlowLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public ParameterSnapshot InputParameters { get; set; } = new();
    [Id(2)] public AgentStateSnapshot PreExecutionState { get; set; } = new();
    [Id(3)] public ExecutionContext Context { get; set; } = new();
    [Id(4)] public List<string> UpstreamNodes { get; set; } = new();
    [Id(5)] public EventFlowRecord EventFlow { get; set; } = new();
}

[GenerateSerializer]
public class FinishExecuteWorkUnitFlowLogEvent : WorkflowExecutionFlowLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public ParameterSnapshot OutputParameters { get; set; } = new();
    [Id(2)] public AgentStateSnapshot PostExecutionState { get; set; } = new();
    [Id(3)] public PerformanceMetrics Metrics { get; set; } = new();
    [Id(4)] public List<string> DownstreamNodes { get; set; } = new();
    [Id(5)] public EventFlowRecord EventFlow { get; set; } = new();
    [Id(6)] public List<ExecutionError> Errors { get; set; } = new();
}

[GenerateSerializer]
public class FinishExecuteWorkflowFlowLogEvent : WorkflowExecutionFlowLogEvent
{
    [Id(0)] public Dictionary<string, object> FinalMetrics { get; set; } = new();
    [Id(1)] public List<ExecutionError> WorkflowErrors { get; set; } = new();
    [Id(2)] public WorkflowExecutionSummary Summary { get; set; } = new();
}

[GenerateSerializer]
public class EventFlowTrackingLogEvent : WorkflowExecutionFlowLogEvent
{
    [Id(0)] public EventFlowRecord EventFlow { get; set; } = new();
    [Id(1)] public string SourceContext { get; set; } = string.Empty;
}

[GenerateSerializer]
public class WorkflowExecutionSummary
{
    [Id(0)] public TimeSpan TotalDuration { get; set; }
    [Id(1)] public int TotalNodes { get; set; }
    [Id(2)] public int SuccessfulNodes { get; set; }
    [Id(3)] public int FailedNodes { get; set; }
    [Id(4)] public Dictionary<string, object> Metrics { get; set; } = new();
}
```

### 新的接口设计

```csharp
public interface IWorkflowExecutionFlowRecordGAgent : IStateGAgent<WorkflowExecutionFlowRecordState>
{
    Task<WorkflowExecutionSummary> GetExecutionSummaryAsync();
    Task<List<EventFlowRecord>> GetEventFlowsAsync();
    Task<WorkflowTopology> GetWorkflowTopologyAsync();
    Task<List<AgentStateSnapshot>> GetAgentStateSnapshotsAsync(string workUnitGrainId);
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task<List<ExecutionError>> GetExecutionErrorsAsync();
    Task ForceStateSnapshotAsync(string workUnitGrainId, string context = "Manual trigger");
}
```

## 实现策略

### 阶段 1: 基础架构
1. 创建新的 State 和 Event 结构
2. 实现基础的 WorkflowExecutionFlowRecordGAgent
3. 保持与现有事件兼容

### 阶段 2: 增强功能
1. 实现 Agent State 快照捕获
2. 添加事件流转追踪
3. 实现性能指标收集

### 阶段 3: 集成优化
1. 与 WorkflowCoordinatorGAgent 集成
2. 添加错误处理和恢复机制
3. 优化存储和查询性能

### 阶段 4: 渐进式替换
1. 提供配置开关选择使用新/旧 agent
2. 数据迁移工具
3. 完全替换旧实现

## 技术考虑

### 性能影响
- State 快照可能较大，需要考虑序列化性能
- 事件流记录会增加存储需求
- 需要提供配置选项控制记录详细程度

### 兼容性
- 新 agent 完全独立，不影响现有功能
- 通过配置控制是否启用增强记录
- 保持与现有事件系统兼容

### 扩展性
- 支持插件式的状态快照捕获器
- 可配置的指标收集器
- 支持自定义事件追踪

## 使用场景

### 调试模式
```csharp
// 开启详细记录模式
var config = new WorkflowCoordinatorConfigDto
{
    EnableFlowRecord = true,
    FlowRecordLevel = FlowRecordLevel.Detailed,
    CaptureAgentStates = true
};
```

### 生产监控
```csharp
// 轻量级监控模式
var config = new WorkflowCoordinatorConfigDto
{
    EnableFlowRecord = true,
    FlowRecordLevel = FlowRecordLevel.Essential,
    CaptureAgentStates = false
};
```

## 总结

新的 WorkflowExecutionFlowRecordGAgent 将提供：

1. **完整的事件流转追踪** - 记录每个事件的来源、目标和流转方向
2. **详细的参数记录** - 结构化记录输入输出参数
3. **Agent 状态快照** - 捕获执行前后的完整状态
4. **性能指标** - 详细的执行时间、内存使用等指标
5. **错误追踪** - 完整的错误上下文和堆栈信息
6. **工作流拓扑** - 完整的节点关系和执行路径

这个设计确保了向后兼容性，同时提供了强大的调试和监控能力。
