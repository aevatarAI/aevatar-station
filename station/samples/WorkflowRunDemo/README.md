# Workflow Run Demo

这是一个独立的测试demo，用于测试工作流的创建和运行流程。

## 功能说明

该Demo完成以下步骤：

1. **创建 WorkflowViewAgent** - 通过 `POST /api/agent` 创建一个包含两个节点的工作流：
   - InputGAgentPlus：输入节点
   - ChatAIGAgentPlus：AI聊天节点
   
2. **运行工作流** - 使用返回的Agent ID调用 `POST /api/workflow/run` 执行工作流

## 工作流配置

Demo创建的工作流包含：

- **节点 1**: InputGAgentPlus
  - 提供输入：`"Hello World"`
  
- **节点 2**: ChatAIGAgentPlus  
  - 接收输入并使用AI处理
  - 配置：`"You are a helpful assistant"`
  
- **连接**: InputGAgentPlus → ChatAIGAgentPlus

## 使用方法

### 前置条件

1. 确保后端API服务正在运行
2. 默认API地址：`http://localhost:5000`

### 运行Demo

使用默认URL：
```bash
cd station/samples/WorkflowRunDemo
dotnet run
```

指定自定义URL：
```bash
cd station/samples/WorkflowRunDemo
dotnet run http://your-api-server:port
```

例如：
```bash
dotnet run https://api.example.com
```

## 输出示例

```
=== Workflow Run Demo ===

Using default base URL: http://localhost:5000

Step 1: Creating WorkflowViewAgent...
Request body:
{
  "AgentType": "Aevatar.GAgents.Workflow.IWorkflowViewGAgentPlus",
  "Name": "untitled_workflow",
  ...
}

Response status: OK
✅ Agent created successfully with ID: d2ba5534-f602-4288-b2a3-ef033877a0c8

Step 2: Running workflow...
Request body:
{
  "ViewAgentId": "d2ba5534-f602-4288-b2a3-ef033877a0c8",
  ...
}

Response status: OK
✅ Workflow execution completed:
   - Success: True
   - Message: Workflow executed successfully. Execution event ID: xxx
   - Workflow ID: yyy

Press any key to exit...
```

## API接口说明

### 1. 创建Agent
```
POST /api/agent
Content-Type: application/json

{
  "agentType": "Aevatar.GAgents.Workflow.IWorkflowViewGAgentPlus",
  "name": "untitled_workflow",
  "properties": {
    "workflowNodeList": [...],
    "workflowNodeUnitList": [...],
    "name": "untitled_workflow"
  }
}
```

**响应**:
```json
{
  "code": "20000",
  "data": {
    "id": "d2ba5534-f602-4288-b2a3-ef033877a0c8",
    ...
  }
}
```

### 2. 运行工作流
```
POST /api/workflow/run
Content-Type: application/json

{
  "viewAgentId": "d2ba5534-f602-4288-b2a3-ef033877a0c8",
  "eventProperties": {}
}
```

**响应**:
```json
{
  "isSuccess": true,
  "message": "Workflow executed successfully",
  "workflowId": "yyy"
}
```

## 修改工作流配置

你可以修改 `Program.cs` 中的 `CreateWorkflowAgentAsync()` 方法来：

1. 添加更多节点
2. 修改节点配置（jsonProperties）
3. 调整节点连接关系（workflowNodeUnitList）

## 故障排查

### 连接失败
- 检查API服务是否正在运行
- 验证URL是否正确
- 检查防火墙设置

### Agent创建失败
- 检查节点的 `agentType` 是否正确
- 验证 `jsonProperties` 的JSON格式
- 查看API服务日志

### 工作流执行失败
- 确保所有节点的配置都有效
- 检查节点之间的连接是否正确
- 查看 `WorkflowCoordinator` 和 `ExecutionRecord` 的日志

