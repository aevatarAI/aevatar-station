# 🎯 Workflow日志查询API文档

## 概述

本文档介绍如何使用新增的Workflow日志查询API来检索和分析基于WorkflowId的结构化日志数据。

这些API专门用于查询我们在InterceptorAttribute中配置的workflow日志，支持复杂的过滤条件。

## API端点

### 1. 基础WorkflowId查询

**端点**: `GET /api/host/workflow-log`

**描述**: 根据WorkflowId获取所有相关的workflow日志

**参数**:
- `appId` (string, required): 应用程序ID
- `hostType` (HostTypeEnum, required): 主机类型 (如: api, worker)
- `workflowId` (string, required): 要查询的WorkflowId
- `pageSize` (int, optional): 返回的日志条数，默认100

**示例请求**:
```http
GET /api/host/workflow-log?appId=aevatar-gagents&hostType=api&workflowId=workflow-12345678-1234-1234-1234-123456789012&pageSize=50
```

**示例响应**:
```json
[
  {
    "timestamp": "2025-01-15T10:30:00Z",
    "app_log": {
      "@m": "WORKFLOW: [WorkflowId=workflow-12345678-1234-1234-1234-123456789012] [Step=get-interest-value] [Type=WorkflowExecution] ENTER: GetInterestValueAsync",
      "@i": "workflow-step-001",
      "@t": "2025-01-15T10:30:00.123Z",
      "@l": "Information",
      "@x": null,
      "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
      "hostId": "pod-123",
      "version": "1.0.0",
      "application": "aevatar-station",
      "environment": "production"
    }
  },
  {
    "timestamp": "2025-01-15T10:30:00Z",
    "app_log": {
      "@m": "WORKFLOW: [WorkflowId=workflow-12345678-1234-1234-1234-123456789012] [Step=get-interest-value] [Type=WorkflowExecution] INPUT: {\"blackboardId\":\"12345678-1234-1234-1234-123456789012\"}",
      "@i": "workflow-step-002",
      "@t": "2025-01-15T10:30:00.125Z",
      "@l": "Information",
      "@x": null,
      "sourceContext": "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent",
      "hostId": "pod-123",
      "version": "1.0.0",
      "application": "aevatar-station",
      "environment": "production"
    }
  }
]
```

### 2. 高级过滤查询

**端点**: `GET /api/host/workflow-log/filter`

**描述**: 根据WorkflowId和额外过滤条件获取workflow日志

**参数**:
- `appId` (string, required): 应用程序ID  
- `hostType` (HostTypeEnum, required): 主机类型
- `workflowId` (string, required): 要查询的WorkflowId
- `workflowStep` (string, optional): 工作流步骤过滤 (如: get-interest-value, chat, create-user-account)
- `workflowAction` (string, optional): 工作流动作过滤 (ENTER, INPUT, OUTPUT, EXIT, EXCEPTION)
- `pageSize` (int, optional): 返回的日志条数，默认100

**示例请求**:

1. **查询特定步骤的所有日志**:
```http
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=order-proc-ORDER-123&workflowStep=validate-order
```

2. **查询所有输入日志**:
```http
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=user-onboard-john.doe@example.com&workflowAction=INPUT
```

3. **查询特定步骤的异常日志**:
```http
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=data-validation-workflow&workflowStep=process-data-with-validation&workflowAction=EXCEPTION
```

## WorkflowId格式说明

根据我们的InterceptorAttribute实现，WorkflowId有以下几种格式：

### 1. GAgent工作流
- **格式**: `workflow-{BlackboardId}`
- **示例**: `workflow-12345678-1234-1234-1234-123456789012`
- **说明**: 由MemberGAgentBase自动生成，基于ChatEvent中的BlackboardId

### 2. 业务工作流
- **格式**: `{业务前缀}-{标识符}`
- **示例**: 
  - `order-proc-ORDER-123` (订单处理)
  - `user-onboard-john.doe@example.com` (用户入职)
  - `data-validation-workflow` (数据验证)

### 3. 嵌套工作流
- **格式**: `{父工作流名称}-workflow`
- **示例**: `nested-parent-workflow`

## 常见查询场景

### 1. 调试特定InputGAgent执行

**查询InputGAgent在特定工作流中的所有日志**:
```http
GET /api/host/workflow-log?appId=aevatar-gagents&hostType=api&workflowId=workflow-12345678-1234-1234-1234-123456789012&pageSize=100
```

**只查看InputGAgent的聊天步骤**:
```http
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=workflow-12345678-1234-1234-1234-123456789012&workflowStep=chat
```

### 2. 性能分析

**查看工作流的所有ENTER/EXIT日志来分析执行时间**:
```http
# 入口日志
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=order-proc-ORDER-123&workflowAction=ENTER

# 出口日志
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=order-proc-ORDER-123&workflowAction=EXIT
```

### 3. 错误排查

**查找工作流中的所有异常**:
```http
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=data-validation-workflow&workflowAction=EXCEPTION
```

### 4. 数据流追踪

**查看特定步骤的输入输出数据**:
```http
# 输入数据
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=order-proc-ORDER-123&workflowStep=process-payment&workflowAction=INPUT

# 输出数据
GET /api/host/workflow-log/filter?appId=aevatar-gagents&hostType=api&workflowId=order-proc-ORDER-123&workflowStep=process-payment&workflowAction=OUTPUT
```

## Elasticsearch直接查询

如果需要更复杂的查询，可以直接使用Elasticsearch：

### 1. 基础WorkflowId查询
```json
GET /logs-*/_search
{
  "query": {
    "wildcard": {
      "app_log.@m": "*WORKFLOW:*[WorkflowId=workflow-12345678-1234-1234-1234-123456789012]*"
    }
  },
  "sort": [
    {
      "app_log.@t": {
        "order": "asc"
      }
    }
  ],
  "size": 100
}
```

### 2. 复合条件查询
```json
GET /logs-*/_search
{
  "query": {
    "bool": {
      "must": [
        {
          "wildcard": {
            "app_log.@m": "*WORKFLOW:*[WorkflowId=order-proc-ORDER-123]*"
          }
        },
        {
          "wildcard": {
            "app_log.@m": "*[Step=validate-order]*"
          }
        },
        {
          "wildcard": {
            "app_log.@m": "*ENTER:*"
          }
        }
      ]
    }
  },
  "sort": [
    {
      "@timestamp": {
        "order": "asc"
      }
    }
  ]
}
```

### 3. 聚合分析
```json
GET /logs-*/_search
{
  "size": 0,
  "query": {
    "wildcard": {
      "app_log.@m": "*WORKFLOW:*"
    }
  },
  "aggs": {
    "workflows": {
      "terms": {
        "script": {
          "source": "def m = ctx._source.app_log['@m']; if (m != null && m.contains('WorkflowId=')) { def start = m.indexOf('WorkflowId=') + 11; def end = m.indexOf(']', start); if (end > start) { return m.substring(start, end); } } return 'unknown';"
        },
        "size": 10
      }
    }
  }
}
```

## 错误处理

API可能返回的错误：

### 1. 400 Bad Request
```json
{
  "error": {
    "code": "400",
    "message": "Invalid parameters: workflowId is required"
  }
}
```

### 2. 500 Internal Server Error
```json
{
  "error": {
    "code": "500", 
    "message": "查询WorkflowId日志失败: Elasticsearch connection timeout"
  }
}
```

## 最佳实践

### 1. 性能优化
- 使用合适的`pageSize`，避免一次查询过多数据
- 优先使用更精确的过滤条件（如`workflowStep`和`workflowAction`）
- 对于大量数据的分析，考虑使用Elasticsearch聚合查询

### 2. 日志分析
- 结合`ENTER`和`EXIT`日志分析方法执行时间
- 使用`INPUT`和`OUTPUT`日志追踪数据流转
- 通过`EXCEPTION`日志快速定位错误

### 3. 监控告警
- 可以基于`workflowAction=EXCEPTION`设置告警规则
- 监控特定WorkflowId的执行频率和成功率
- 分析workflow步骤的平均执行时间

## 总结

这套API为WorkflowId日志查询提供了完整的解决方案：

✅ **基础查询**: 快速获取特定workflow的所有日志  
✅ **高级过滤**: 支持步骤和动作级别的精确过滤  
✅ **性能优化**: 基于ES wildcard查询，支持大规模数据  
✅ **易于集成**: RESTful API设计，支持各种客户端调用  
✅ **调试友好**: 结构化日志格式，易于分析和排查问题

通过这些API，可以轻松实现workflow执行的完整可观测性，支持性能分析、错误排查、数据流追踪等各种场景。
