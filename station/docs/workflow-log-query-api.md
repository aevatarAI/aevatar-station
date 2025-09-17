# Workflow日志查询API

## 接口

**GET** `/api/host/workflow-log`

## 参数

- `appId` (必填): 应用ID
- `hostType` (必填): 主机类型 (api/worker)
- `workflowId` (必填): 工作流ID
- `grainId` (可选): Grain实例ID
- `level` (可选): 日志级别 (Information/Warning/Error)
- `messagePattern` (可选): 消息匹配
- `pageSize` (可选): 返回条数，默认100

## 示例

```http
GET /api/host/workflow-log?appId=test&hostType=api&workflowId=3898f980-8410-470e-af7d-ba3c679f9cbe
```

查询错误日志：
```http
GET /api/host/workflow-log?appId=test&hostType=api&workflowId=xxx&level=Error
```

**示例响应**:
```json
[
  {
    "timestamp": "2025-09-16T11:09:36.2928358Z",
    "app_log": {
      "@m": "WORKFLOW: ENTER ChatAsync(blackboardId=3898f980-8410-470e-af7d-ba3c679f9cbe,messages=[...]) WorkflowId=3898f980-8410-470e-af7d-ba3c679f9cbe GrainId=18277c1f-0cd5-4503-8d0c-013bd69129a5",
      "@i": "3c939a07",
      "@t": "2025-09-16T11:09:36.2928358Z",
      "@l": "Information", 
      "@tr": "7fddc05231c60579a39ba955c1439817",
      "@sp": "643439fe64eb4d83",
      "logCategory": "WORKFLOW",
      "methodName": "ChatAsync",
      "workflowId": "3898f980-8410-470e-af7d-ba3c679f9cbe",
      "grainId": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent/18277c1f0cd545038d0c013bd69129a5",
      "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
      "application": "Aevatar.defaultproject521796.Host",
      "environment": "Staging"
    }
  },
  {
    "timestamp": "2025-09-16T11:09:36.3128358Z", 
    "app_log": {
      "@m": "WORKFLOW: EXIT ChatAsync WorkflowId=3898f980-8410-470e-af7d-ba3c679f9cbe GrainId=18277c1f-0cd5-4503-8d0c-013bd69129a5",
      "@i": "4d847b08",
      "@t": "2025-09-16T11:09:36.3128358Z",
      "@l": "Information",
      "logCategory": "WORKFLOW",
      "methodName": "ChatAsync", 
      "workflowId": "3898f980-8410-470e-af7d-ba3c679f9cbe",
      "grainId": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent/18277c1f0cd545038d0c013bd69129a5",
      "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
      "application": "Aevatar.defaultproject521796.Host",
      "environment": "Staging"
    }
  }
]
```