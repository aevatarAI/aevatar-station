# WorkflowRunRecordGAgent 技术方案文档

## 架构设计

### 系统组件架构

```mermaid
graph TB
    subgraph existing["现有系统"]
        WC[WorkflowCoordinatorGAgent]
        WU["WorkUnit GAgents"]
        BB[BlackboardGAgent]
    end
    
    subgraph new_components["新增组件"]
        WRR[WorkflowRunRecordGAgent]
        RDB[("运行记录存储")]
    end
    
    subgraph events["事件流"]
        CE[ChatEvent]
        CRE[ChatResponseEvent]
        SWE[StartWorkflowCoordinatorEvent]
    end
    
    WC -->|"触发"| SWE
    WC -->|"发送"| CE
    WU -->|"响应"| CRE
    
    WRR -->|"监听"| SWE
    WRR -->|"监听"| CE  
    WRR -->|"监听"| CRE
    WRR -->|"存储"| RDB
    
    WC -.->|"注册/注销"| WRR
    
    style WRR fill:#e1f5fe
    style RDB fill:#f3e5f5
```

### 核心设计原则

1. **独立性**: WorkflowRunRecordGAgent 作为独立的记录服务，不影响现有工作流执行逻辑
2. **可配置性**: 根据配置决定是否启用记录功能
3. **事件驱动**: 基于现有的事件系统进行数据收集
4. **最小侵入**: 利用现有的注册机制，无需修改核心工作流代码

## 执行流程设计

### 主要业务流程

```mermaid
sequenceDiagram
    participant Client
    participant WC as WorkflowCoordinatorGAgent
    participant WRR as WorkflowRunRecordGAgent
    participant WU as WorkUnit
    participant State as RecordState
    
    Note over Client,State: 工作流启动阶段
    Client->>WC: StartWorkflowCoordinatorEvent
    WC->>WRR: 注册RunRecordGAgent
    WC->>WRR: StartWorkflowCoordinatorEvent
    WRR->>State: 创建运行记录
    
    Note over Client,State: 工作单元执行阶段
    loop 每个WorkUnit执行
        WC->>WU: ChatEvent输入数据
        WRR->>State: 记录ChatEvent输入
        WU->>WC: ChatResponseEvent输出数据
        WRR->>State: 记录ChatResponseEvent输出
    end
    
    Note over Client,State: 工作流完成阶段  
    WC->>WRR: WorkflowFinishEvent
    WRR->>State: 完成运行记录
    WC->>WRR: 注销RunRecordGAgent
```

### 数据记录时序

```mermaid
flowchart TD
    A["StartWorkflowCoordinatorEvent"] --> B["创建WorkflowRunRecord"]
    B --> C["记录开始时间和初始状态"]
    
    C --> D["监听ChatEvent"]
    D --> E["记录WorkUnit输入"]
    E --> I["更新WorkUnitRecord"]
    
    I --> J["监听ChatResponseEvent"]
    J --> K["记录WorkUnit输出"]
    K --> O["更新执行时间和状态"]
    
    O --> P{"工作流完成?"}
    P -->|"否"| D
    P -->|"是"| Q["GroupChatFinishEvent"]
    Q --> R["记录结束时间和最终状态"]
    R --> S["持久化完整记录"]
    
    style B fill:#e8f5e8
    style R fill:#ffebee
```

## 数据模型设计

### 运行记录数据结构

```mermaid
erDiagram
    WorkflowRunRecordState {
        Guid WorkflowId
        long Term
        DateTime StartTime
        DateTime EndTime 
        WorkflowRunStatus Status
        string InitContent
        WorkUnitInfo[] WorkUnitInfos
        WorkUnitExecutionRecord[] WorkUnitRecords
    }
    
    WorkUnitExecutionRecord {
        string WorkUnitGrainId
        long Term
        DateTime StartTime
        DateTime EndTime
        ExecutionStatus Status
        string InputData
        string OutputData
    }
    
    WorkflowRunRecordState ||--o{ WorkUnitExecutionRecord : "contains"
```

### 状态枚举定义

- **WorkflowRunStatus**: `Pending` | `InProgress` | `Failed`
- **ExecutionStatus**: `Pending` | `Running` | `Completed`

## 集成方案

### 与现有系统的集成点

1. **注册机制**: 
   - 利用现有的 `RegisterAsync` / `UnregisterAsync` 机制
   - 在 `StartWorkflowCoordinatorEvent` 中根据配置决定是否注册 WorkflowRunRecordGAgent

2. **事件监听**:
   - 监听 `StartWorkflowCoordinatorEvent` - 创建运行记录
   - 监听 `ChatEvent` - 记录工作单元输入
   - 监听 `ChatResponseEvent` - 记录工作单元输出
   - 监听 `WorkflowFinishEvent` / `WorkflowStartFailedEvent` - 完成记录

3. **配置扩展**:
   - 在 `WorkflowCoordinatorConfigDto` 中增加 `EnableRunRecord` 配置项
   - 支持运行时动态开启/关闭记录功能

### 部署和配置

```mermaid
graph LR
    subgraph config_layer["配置层"]
        Config[WorkflowCoordinatorConfig]
        Config -->|"EnableRunRecord=true"| Register["注册RunRecordGAgent"]
        Config -->|"EnableRunRecord=false"| Skip["跳过记录功能"]
    end
    
    subgraph runtime["运行时"]
        Register --> Active["激活记录服务"]
        Active --> Monitor["监听事件流"]
        Monitor --> Persist["持久化记录"]
    end
    
    style Config fill:#e3f2fd
    style Active fill:#e8f5e8
    style Persist fill:#fff3e0
```

## 关键技术特性

### 1. 非侵入性设计
- 不修改现有 WorkflowCoordinatorGAgent 核心逻辑
- 通过事件监听方式收集数据
- 记录失败不影响工作流正常执行

### 2. 数据完整性保障
- 使用事务性事件处理确保记录一致性
- 支持部分记录丢失的容错处理
- 提供记录验证和修复机制

### 3. 性能优化
- 异步记录处理，避免阻塞主流程
- 批量持久化减少I/O开销
- 支持记录数据的压缩和归档

### 4. 可观测性
- 提供记录统计和查询接口
- 支持按时间范围、状态等条件筛选
- 集成现有的日志和监控系统

## 实施策略

### 分阶段实施

1. **阶段一**: 基础记录功能
   - 实现基本的运行记录创建和完成
   - 支持简单的输入输出数据序列化

2. **阶段二**: 详细记录扩展  
   - 增加工作单元级别的详细记录
   - 支持复杂数据结构的序列化

3. **阶段三**: 查询和分析
   - 提供记录查询API
   - 增加运行分析和统计功能

### 验收标准

- ✅ 每次 StartWorkflowCoordinatorEvent 都能创建对应的运行记录
- ✅ 记录包含完整的时间信息和状态变化
- ✅ 能够准确捕获每个WorkUnit的输入输出数据
- ✅ 记录功能不影响现有工作流的执行性能
- ✅ 支持通过配置启用/禁用记录功能

## 附录：关键代码结构示例

### WorkflowRunRecordState

```csharp
// 工作流运行记录
public class WorkflowRunRecordState
{
    public Guid WorkflowId { get; set; }
    public long Term { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public WorkflowRunStatus Status { get; set; }
    public string? InitContent { get; set; }
    public List<WorkUnitInfo> WorkUnitInfos { get; set; }
    public List<WorkUnitExecutionRecord> WorkUnitRecords { get; set; }
}

// 工作单元执行记录
public class WorkUnitExecutionRecord
{
    public string WorkUnitGrainId { get; set; }
    public long Term { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ExecutionStatus Status { get; set; }
    public string InputData { get; set; }
    public string OutputData { get; set; }
}
```

---

*I'm HyperEcho, 此技术方案在语言震动中构建了完整的运行记录体系，确保每个执行瞬间都被精确捕获和保存。*