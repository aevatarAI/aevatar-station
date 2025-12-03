# Workflow View 管理设计文档

## 1. 概述

**重要发现：WorkflowView本身可以作为Agent进行管理，无需额外开发新的接口！**

Workflow View 管理系统通过复用现有的Agent管理基础设施，实现工作流可视化编排界面的数据保存和管理。该系统将WorkflowView作为一个特殊的Agent类型，利用Agent系统的完整CRUD操作、权限管理、事件溯源等能力。

## 2. 核心设计原则

### 2.1 Agent统一管理
- **复用现有基础设施**：WorkflowView作为Agent，使用`/api/agent`的所有现有接口
- **配置驱动**：通过`WorkflowViewConfigDto`作为Agent的Configuration管理工作流数据
- **事件溯源**：利用Agent的事件溯源能力实现版本控制和历史追踪

### 2.2 职责分离
- **视图与执行分离**：WorkflowViewAgent只负责视图管理，不涉及实际执行
- **配置与逻辑分离**：配置数据存储在ConfigurationBase中，业务逻辑在Agent中
- **统一与特化分离**：使用统一的Agent接口，特化的WorkflowView业务逻辑

## 3. Agent架构设计 

### 3.1 WorkflowViewAgent定义

```csharp
/// <summary>
/// 工作流视图管理Agent接口
/// </summary>
public interface IWorkflowViewAgent : IGAgent
{
}

/// <summary>
/// 工作流视图管理Agent实现
/// </summary>
[GAgent]
public class WorkflowViewAgent : GAgentBase<WorkflowViewState, WorkflowViewEvent>, IWorkflowViewAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow View Management Agent - 工作流视图管理代理");
    }
    
    protected override async Task PerformConfigAsync(WorkflowViewConfigDto configuration)
    {
        //更新workflowView配置信息
    }
    
}
```

### 3.2 状态和事件定义

```csharp
[GenerateSerializer]
public class WorkflowViewState : StateBase
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public Guid WorkflowCoordinatorGAgentId { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public Guid AgentId { get; set; }
}


[GenerateSerializer]
public class WorkflowViewConfigDto : ConfigurationBase
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public string Name { get; set; }
    [Id(3)] public Guid WorkflowCoordinatorGAgentId { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeDto
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public Dictionary<string,string> ExtendedData { get; set; } = new();
    [Id(3)] public Dictionary<string, object> Properties { get; set; } = new();
    [Id(4)] public Guid NodeId { get; set; }
    [Id(5)] public Guid AgentId { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeUnitDto
{
    [Id(0)] public Guid NodeId { get; set; }
    [Id(1)] public Guid NextNodeId { get; set; }
}
```

## 4
### 4.1 创建workflowView
```csharp
/api/agent Post 
var createWorkflowViewRequest = new CreateAgentInputDto
{
    AgentId = "",
    AgentType = "WorkflowViewAgent",
    Name = "我的工作流视图",
    Properties = new Dictionary<string, object>
    {
        ["workflowNodeList"] = new List<object>
        {
            new {
                agentType = "DataProcessorAgent",
                name = "数据处理节点",
                extendedData = new Dictionary<string, string>
                {
                    ["position_x"] = "100",
                    ["position_y"] = "100",
                    ["width"] = "200",
                    ["height"] = "80"
                },
                properties = new Dictionary<string, object>
                {
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                },
                nodeId = "guid1"
            }
        },
        ["workflowNodeUnitList"] = new List<object>
        {
            new { nodeId = "guid1", nextnodeId = "guid2" }
        },
        ["Name"] = ""
    }
};

// 通过现有API创建
var result = await agentController.CreateAgent(createWorkflowViewRequest);
```

### 4.2 更新workflowView
```csharp
/api/agent/{agentId} Post
var updateWorkflowViewRequest = new UpdateAgentInputDto
{
    Name = "我的工作流视图",
    Properties = new Dictionary<string, object>
    {
        // WorkflowViewConfigDto的字段映射到Properties
        ["workflowNodeList"] = new List<object>
        {
            new {
                agentType = "DataProcessorAgent",
                name = "数据处理节点",
                agentId = "grain-id-1",
                extendedData = new Dictionary<string, string>
                {
                    ["position_x"] = "100",
                    ["position_y"] = "100",
                    ["width"] = "200",
                    ["height"] = "80"
                },
                properties = new Dictionary<string, object>
                {
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                },
                nodeIndex = 0
            }
        },
        ["workflowNodeUnitList"] = new List<object>
        {
            new { nodeIndex = 0, nextNodeIndex = 1 }
        },
        ["Name"] = ""
    }
};

// 通过现有API创建
var result = await agentController.UpdateAgentAsync(createWorkflowViewRequest);
```

### 4.3 发布workflowView为WorkflowCoordinatorGAgent
```csharp
/api/workflow-view/{workflowViewId}/publish-workflow Post

[HttpPost]
[Route("{guid}/publish-workflow")]
public virtual Task<AgentDto> PublishWorkflowAsync(Guid guid)
{
    return _workflowViewService.PublishWorkflowAsync(guid);
}

public async Task<AgentDto> PublishWorkflowAsync(Guid viewAgentId)
{
  // step1: getWorkflowViewGAgent
  // step2: create or update workflowNode agent
  // step3  create or update workflowCoordinaterGAgent
  // step4: update workflowViewGAgent config
}
```