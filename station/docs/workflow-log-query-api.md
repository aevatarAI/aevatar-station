# Workflow日志查询API

## 接口

**GET** `/api/host/workflow-log`

## 参数

- `workflowId` (必填): 工作流ID
- `roundId` (可选): 轮次ID
- `grainId` (可选): Grain实例ID
- `level` (可选): 日志级别 (Information/Warning/Error)
- `messagePattern` (可选): 消息匹配
- `pageIndex` (可选): 页码，默认1
- `pageSize` (可选): 每页条数，默认100

## 示例

```http
GET /api/host/workflow-log?workflowId=3898f980-8410-470e-af7d-ba3c679f9cbe&pageIndex=1&pageSize=100
```

查询错误日志：
```http
GET /api/host/workflow-log?workflowId=xxx&level=Error&pageIndex=1&pageSize=50
```

## 响应结构

API返回包装在`data`数组中的日志记录列表，每条记录包含：

- `timestamp`: ES索引时间戳
- `appLog`: 应用日志详细信息
  - `message`: 格式化的日志消息
  - `logId`: 日志唯一标识符
  - `time`: 应用内部时间戳
  - `level`: 日志级别 (可能为null)
  - `traceId`/`spanId`: 分布式追踪标识
  - `logCategory`: 日志分类 (WORKFLOW)
  - `workflowId`: 工作流ID
  - `roundId`: 轮次ID (可能为null)
  - `grainId`: Orleans Grain实例ID
  - `sourceContext`: 日志来源类名
  - `application`: 应用程序名称
  - `environment`: 运行环境
  - `methodName`: 方法名称

**示例响应**:
```json
{
  "data": [
    {
      "timestamp": "2025-09-23T07:32:41.446Z",
      "appLog": {
        "message": "\"WORKFLOW\": ENTER \"ChatAsync\"(\"{\\\"messages\\\":[]}\") \"3537305b-f922-413e-a4e2-21146079bce3\"",
        "logId": "f87c5d6c",
        "time": "2025-09-23T07:32:36.9759486Z",
        "level": null,
        "exception": null,
        "traceId": "74b1868aa8c8a60f3d7a369f0260060d",
        "spanId": "66a22eb39863e686",
        "logCategory": "WORKFLOW",
        "workflowId": "3537305b-f922-413e-a4e2-21146079bce3",
        "roundId": null,
        "grainId": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent/702452ef96f34b93a88efe81784d5ccb",
        "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
        "hostId": null,
        "version": null,
        "application": "Aevatar.defaultproject3d95fc.Host",
        "environment": "Staging",
        "methodName": "ChatAsync"
      }
    },
    {
      "timestamp": "2025-09-23T07:32:41.446Z",
      "appLog": {
        "message": "\"WORKFLOW\": EXIT \"ChatAsync\" \"3537305b-f922-413e-a4e2-21146079bce3\"",
        "logId": "edce4f4b",
        "time": "2025-09-23T07:32:36.9761951Z",
        "level": null,
        "exception": null,
        "traceId": "74b1868aa8c8a60f3d7a369f0260060d",
        "spanId": "66a22eb39863e686",
        "logCategory": "WORKFLOW",
        "workflowId": "3537305b-f922-413e-a4e2-21146079bce3",
        "roundId": null,
        "grainId": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent/702452ef96f34b93a88efe81784d5ccb",
        "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
        "hostId": null,
        "version": null,
        "application": "Aevatar.defaultproject3d95fc.Host",
        "environment": "Staging",
        "methodName": "ChatAsync"
      }
    }
  ],
  "message": ""
}
```