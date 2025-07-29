# AevatarStation API 接口设计文档

## 概述

AevatarStation是一个用于开发、管理和部署AI智能体的全栈平台，基于"智能体是语言的自指结构"这一核心理念构建。平台采用分布式、事件驱动和可插拔的设计原则，支持灵活扩展和高可用部署。

### 平台架构特点
- **Orleans分布式系统**: 基于Microsoft Orleans的Actor模型分布式计算
- **事件溯源与CQRS**: 所有智能体状态变更以事件形式存储，命令查询分离
- **多Silo架构**: 三个专门的Orleans Silo（调度器、投影器、用户）处理不同工作负载
- **热插拔扩展**: WebHook.Host支持动态DLL加载和多态IWebhookHandler发现
- **实时通信**: SignalR集成实现智能体与前端的实时交互

### 基础信息
- **基础URL**: `/api`
- **认证方式**: Bearer Token (JWT) 通过AuthServer
- **内容类型**: `application/json`
- **API版本**: v1
- **服务端点**:
  - HttpApi.Host: `http://localhost:7002` (主要API，包含Swagger UI)
  - Developer.Host: `http://localhost:7003` (开发者API，包含Swagger UI)
  - AuthServer: `http://localhost:7001` (认证服务)

### 基础设施组件
- **MongoDB**: 事件溯源和状态持久化
- **Redis**: 分布式缓存和集群协调
- **Kafka**: 事件流和智能体间通信
- **ElasticSearch**: 搜索和分析功能
- **Qdrant**: AI嵌入向量数据库
- **Aspire**: 统一编排和服务管理

### 通用响应格式
所有接口都遵循标准的RESTful响应格式，基于Orleans Grain处理，包含完善的错误处理、分页支持和事件溯源能力。

## 目录 / Table of Contents

- [概述](#概述)
- [1. 代理系统模块 (Agent) - 12 APIs](#1-代理系统模块-agent)  
- [2. 插件管理模块 (Plugin) - 4 APIs](#2-插件管理模块-plugin)
- [3. 通知管理模块 (Notification) - 7 APIs](#3-通知管理模块-notification)
- [4. Webhook管理模块 (Webhook) - 4 APIs](#4-webhook管理模块-webhook)
- [5. 订阅管理模块 (Subscription) - 4 APIs](#5-订阅管理模块-subscription)
- [6. API密钥管理模块 (API Key) - 4 APIs](#6-api密钥管理模块-api-key)
- [7. API请求统计模块 (Statistics) - 1 API](#7-api请求统计模块-statistics)
- [8. 查询服务模块 (Query) - 1 API](#8-查询服务模块-query)

---

## 1. 代理系统模块 (GAgent)

### 基础路径: `/api/agent`

GAgent（智能代理）系统是AevatarStation的核心，实现了"智能体是语言的自指结构"这一理念。每个智能体作为Orleans Grain运行，具有以下特征：

- **事件溯源**: 所有状态变更都作为事件存储在MongoDB事件存储中
- **状态管理**: 通过重放事件重建当前状态，持久化在状态存储中
- **流通信**: 使用Kafka流提供者进行智能体间消息广播和订阅
- **层次关系**: 支持智能体注册、订阅和组合，形成智能体网络
- **可扩展性**: GAgent是抽象基类，支持各种智能体类型和自定义扩展

### 1.1 获取所有代理类型
**方法**: GET
**URL**: `/agent-type-info-list`

**功能说明**: 获取系统中所有可用的代理类型信息，包括类型名称、参数定义等，用于创建代理时选择合适的类型。

**请求参数**: 无

**权限要求**: Authorize

**响应示例**:
```json
[
  {
    "id": "OpenAIAgent",
    "name": "OpenAI代理",
    "description": "基于OpenAI的智能代理",
    "parameters": [
      {
        "name": "apiKey",
        "type": "string",
        "required": true,
        "description": "OpenAI API密钥"
      },
      {
        "name": "model",
        "type": "string", 
        "required": false,
        "description": "使用的模型名称"
      }
    ]
  }
]
```

### 1.2 获取代理实例列表
**方法**: GET
**URL**: `/agent-list`

**功能说明**: 分页获取当前用户的代理实例列表，支持查看所有已创建的代理及其状态。

**请求参数**:
| 参数名 | 类型 | 必须 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | ❌ | 0 | 页码，从0开始 |
| pageSize | int | ❌ | 20 | 每页大小，最大100 |

**权限要求**: Authorize

**请求示例**: `GET /api/agent/agent-list?pageIndex=0&pageSize=10`

**响应示例**:
```json
[
  {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "agentType": "OpenAIAgent",
    "name": "My AI Assistant",
    "properties": {
      "apiKey": "sk-***",
      "model": "gpt-4"
    },
    "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
    "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
    "businessAgentGrainId": "business_agent_123"
  }
]
```

### 1.3 创建代理
**方法**: POST
**URL**: `/`

**功能说明**: 创建一个新的AI代理实例，需要指定代理类型和相关配置参数。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| agentId | Guid? | ❌ | 指定代理ID，不提供则自动生成 |
| agentType | string | ✅ | 代理类型，从代理类型列表中选择 |
| name | string | ✅ | 代理名称 |
| properties | Dictionary<string, object>? | ❌ | 代理配置参数，根据代理类型要求提供 |

**权限要求**: Authorize

**请求示例 (OpenAI智能体)**:
```json
{
  "agentType": "OpenAIAgent",
  "name": "My AI Assistant",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4",
    "temperature": 0.7
  }
}
```

**请求示例 (工作流协调器)**:
```json
{
  "agentType": "Aevatar.GAgents.GroupChat.WorkflowCoordinator.WorkflowCoordinatorGAgent",
  "name": "Data Processing Workflow",
  "properties": {
    "workflowUnitList": [
      {
        "grainId": "grain1",
        "nextGrainId": "grain2",
        "extendedData": {
          "taskType": "dataValidation",
          "timeout": 30
        }
      },
      {
        "grainId": "grain2",
        "nextGrainId": "grain3",
        "extendedData": {
          "taskType": "dataTransformation",
          "timeout": 60
        }
      },
      {
        "grainId": "grain3",
        "nextGrainId": null,
        "extendedData": {
          "taskType": "dataOutput",
          "timeout": 15
        }
      }
    ]
  }
}
```

**响应示例**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "OpenAIAgent",
  "name": "My AI Assistant",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4",
    "temperature": 0.7
  },
  "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
  "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
  "propertyJsonSchema": "{...}",
  "businessAgentGrainId": "business_agent_123"
}
```

### 1.4 获取代理详情
**方法**: GET
**URL**: `/{guid}`

**功能说明**: 根据代理ID获取特定代理的详细信息，包括配置参数和运行状态。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 代理唯一标识符 |

**权限要求**: Authorize

**请求示例**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000`

**响应示例**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "OpenAIAgent",
  "name": "My AI Assistant",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4",
    "temperature": 0.7
  },
  "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
  "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
  "propertyJsonSchema": "{...}",
  "businessAgentGrainId": "business_agent_123"
}
```

### 1.5 获取代理关系
**方法**: GET
**URL**: `/{guid}/relationship`

**功能说明**: 获取指定代理的关系图，包括父代理、子代理等层级关系信息。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 代理唯一标识符 |

**权限要求**: Authorize

**请求示例**: `GET /api/agent/123e4567-e89b-12d3-a456-426614174000/relationship`

**响应示例**:
```json
{
  "agentId": "123e4567-e89b-12d3-a456-426614174000",
  "parentAgents": [
    {
      "id": "parent-agent-id",
      "name": "Parent Agent",
      "agentType": "ManagerAgent"
    }
  ],
  "subAgents": [
    {
      "id": "sub-agent-id-1",
      "name": "Sub Agent 1",
      "agentType": "WorkerAgent"
    }
  ]
}
```

### 1.6 添加子代理
**方法**: POST
**URL**: `/{guid}/add-subagent`

**功能说明**: 为指定代理添加子代理，建立层级关系。子代理可以接收父代理的任务分配。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 父代理唯一标识符 |
| subAgentId | Guid | ✅ | 子代理唯一标识符 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "subAgentId": "456e7890-e89b-12d3-a456-426614174000"
}
```

**响应示例**:
```json
{
  "id": "456e7890-e89b-12d3-a456-426614174000",
  "parentAgentId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "子代理",
  "agentType": "WorkerAgent",
  "relationshipType": "SubAgent"
}
```

### 1.7 移除子代理
**方法**: POST
**URL**: `/{guid}/remove-subagent`

**功能说明**: 从指定代理中移除子代理关系，解除层级关系但不删除代理本身。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 父代理唯一标识符 |
| subAgentId | Guid | ✅ | 要移除的子代理唯一标识符 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "subAgentId": "456e7890-e89b-12d3-a456-426614174000"
}
```

**响应示例**:
```json
{
  "id": "456e7890-e89b-12d3-a456-426614174000",
  "message": "子代理关系已成功移除"
}
```

### 1.8 移除所有子代理
**方法**: POST
**URL**: `/{guid}/remove-all-subagent`

**功能说明**: 移除指定代理的所有子代理关系，批量清理层级关系。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 父代理唯一标识符 |

**权限要求**: Authorize

**请求示例**: `POST /api/agent/123e4567-e89b-12d3-a456-426614174000/remove-all-subagent`

**响应示例**:
```json
{
  "message": "所有子代理关系已成功移除",
  "removedCount": 3
}
```

### 1.9 更新代理
**方法**: PUT
**URL**: `/{guid}`

**功能说明**: 更新指定代理的配置信息，包括名称和属性参数。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 代理唯一标识符 |
| name | string | ✅ | 新的代理名称 |
| properties | Dictionary<string, object>? | ❌ | 更新的配置参数 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "name": "更新后的AI助手",
  "properties": {
    "model": "gpt-4-turbo",
    "temperature": 0.8
  }
}
```

**响应示例**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "agentType": "OpenAIAgent",
  "name": "更新后的AI助手",
  "properties": {
    "apiKey": "sk-proj-xxxxxxxxxxxx",
    "model": "gpt-4-turbo",
    "temperature": 0.8
  },
  "grainId": "agent_123e4567-e89b-12d3-a456-426614174000",
  "agentGuid": "123e4567-e89b-12d3-a456-426614174000",
  "propertyJsonSchema": "{...}",
  "businessAgentGrainId": "business_agent_123"
}
```

### 1.10 删除代理
**方法**: DELETE
**URL**: `/{guid}`

**功能说明**: 永久删除指定的代理实例，包括其所有配置和关系。删除后无法恢复。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 要删除的代理唯一标识符 |

**权限要求**: Authorize

**请求示例**: `DELETE /api/agent/123e4567-e89b-12d3-a456-426614174000`

**响应示例**:
```json
{
  "success": true,
  "message": "代理已成功删除"
}
```

### 1.11 发布事件
**方法**: POST
**URL**: `/publishEvent`

**功能说明**: 向事件系统发布一个事件，可以被订阅该事件的代理或系统接收和处理。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| eventType | string | ✅ | 事件类型 |
| payload | object | ✅ | 事件载荷数据 |
| targetAgentId | Guid? | ❌ | 目标代理ID，不指定则广播 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "eventType": "TaskCompleted",
  "payload": {
    "taskId": "task-123",
    "result": "success",
    "data": {
      "output": "任务执行结果"
    }
  },
  "targetAgentId": "456e7890-e89b-12d3-a456-426614174000"
}
```

**响应示例**:
```json
{
  "eventId": "event-789",
  "publishedAt": "2025-01-29T10:30:00Z",
  "status": "published"
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 2. 插件管理模块 (Plugin)

### 基础路径: `/api/plugins`

插件管理系统通过WebHook.Host实现热插拔扩展，支持：

- **动态DLL加载**: 远程注入和加载插件程序集，无需系统重启
- **多态发现**: 自动发现和注册IWebhookHandler实现
- **多租户隔离**: 安全的插件执行环境，支持租户特定上下文
- **热插拔架构**: 添加、更新或移除插件而不影响系统可用性

### 2.1 获取插件列表
**方法**: GET
**URL**: `/`

**功能说明**: 获取当前项目的插件列表，支持分页和筛选。插件是扩展系统功能的代码包。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| projectId | Guid? | ❌ | 项目ID，筛选指定项目的插件 |
| pageIndex | int | ❌ | 页码，从0开始 |
| pageSize | int | ❌ | 每页大小 |

**权限要求**: Authorize

**请求示例**: `GET /api/plugins?projectId=123e4567-e89b-12d3-a456-426614174000&pageIndex=0&pageSize=10`

**响应示例**:
```json
{
  "items": [
    {
      "id": "plugin-123",
      "name": "数据处理插件",
      "version": "1.0.0",
      "projectId": "123e4567-e89b-12d3-a456-426614174000",
      "createdAt": "2025-01-29T10:00:00Z",
      "status": "Active"
    }
  ],
  "totalCount": 1
}
```

### 2.2 创建插件
**方法**: POST
**URL**: `/`

**功能说明**: 上传并创建新的插件。支持多种代码文件格式，最大文件大小15MB。

**Content-Type**: `multipart/form-data`

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| projectId | Guid | ✅ | 目标项目ID |
| code | IFormFile | ✅ | 插件代码文件，支持.zip, .dll等格式 |

**权限要求**: Authorize

**请求示例**:
```
POST /api/plugins
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="projectId"

123e4567-e89b-12d3-a456-426614174000
--boundary
Content-Disposition: form-data; name="code"; filename="plugin.zip"
Content-Type: application/zip

[插件文件二进制数据]
--boundary--
```

**响应示例**:
```json
{
  "id": "plugin-456",
  "name": "plugin.zip",
  "version": "1.0.0",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "createdAt": "2025-01-29T10:30:00Z",
  "status": "Deployed"
}
```

### 2.3 更新插件
**方法**: PUT
**URL**: `/{id}`

**功能说明**: 更新已存在的插件代码，上传新版本的插件文件。

**Content-Type**: `multipart/form-data`

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| id | Guid | ✅ | 插件ID |
| code | IFormFile | ✅ | 新的插件代码文件 |

**权限要求**: Authorize

**请求示例**:
```
PUT /api/plugins/plugin-456
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="code"; filename="plugin-v2.zip"
Content-Type: application/zip

[新版本插件文件二进制数据]
--boundary--
```

**响应示例**:
```json
{
  "id": "plugin-456",
  "name": "plugin-v2.zip",
  "version": "2.0.0",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "updatedAt": "2025-01-29T11:00:00Z",
  "status": "Deployed"
}
```

### 2.4 删除插件
**方法**: DELETE
**URL**: `/{id}`

**功能说明**: 删除指定的插件，包括其所有版本和相关数据。删除后无法恢复。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| id | Guid | ✅ | 要删除的插件ID |

**权限要求**: Authorize

**请求示例**: `DELETE /api/plugins/plugin-456`

**响应示例**:
```json
{
  "success": true,
  "message": "插件已成功删除"
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 3. 通知管理模块 (Notification)

### 基础路径: `/api/notification`

### 3.1 创建通知
**方法**: POST
**URL**: `/`

**功能说明**: 向指定用户发送通知消息，支持多种通知类型如邀请、提醒等。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| type | NotificationTypeEnum | ✅ | 通知类型 (Invite=1, Reminder=2, Alert=3) |
| target | Guid | 条件必须 | 目标用户ID，与targetEmail二选一 |
| targetEmail | string? | 条件必须 | 目标用户邮箱，与target二选一 |
| content | string | ✅ | 通知内容 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "type": 1,
  "target": "456e7890-e89b-12d3-a456-426614174000",
  "content": "您被邀请加入项目开发团队"
}
```

**响应示例**:
```json
{
  "success": true,
  "notificationId": "notif-123",
  "message": "通知已成功发送"
}
```

### 3.2 撤回通知
**方法**: POST
**URL**: `/withdraw/{guid}`

**功能说明**: 撤回已发送的通知，只有通知发送者可以执行此操作。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 通知ID |

**权限要求**: Authorize

**请求示例**: `POST /api/notification/withdraw/notif-123`

**响应示例**:
```json
{
  "success": true,
  "message": "通知已成功撤回"
}
```

### 3.3 响应通知
**方法**: POST
**URL**: `/response`

**功能说明**: 对接收到的通知进行响应，如接受邀请、确认提醒等。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| id | Guid | ✅ | 通知ID |
| status | int | ✅ | 响应状态 (1=接受, 2=拒绝, 3=忽略) |

**权限要求**: Authorize

**请求示例**:
```json
{
  "id": "notif-123",
  "status": 1
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "通知响应已处理"
}
```

### 3.4 获取通知列表
**方法**: GET
**URL**: `/`

**功能说明**: 分页获取当前用户的通知列表，包括已读和未读通知。

**请求参数**:
| 参数名 | 类型 | 必须 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | ❌ | 0 | 页码 |
| pageSize | int | ❌ | 20 | 每页大小 |

**权限要求**: Authorize

**请求示例**: `GET /api/notification?pageIndex=0&pageSize=10`

**响应示例**:
```json
[
  {
    "id": "notif-123",
    "type": 1,
    "content": "您被邀请加入项目开发团队",
    "senderId": "sender-456",
    "senderName": "张三",
    "createdAt": "2025-01-29T10:00:00Z",
    "isRead": false,
    "status": "Pending"
  }
]
```

### 3.5 获取组织访问信息
**方法**: GET
**URL**: `/organization`

**功能说明**: 获取用户访问过的组织信息，用于快速访问常用组织。

**请求参数**:
| 参数名 | 类型 | 必须 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | ❌ | 0 | 页码 |
| pageSize | int | ❌ | 20 | 每页大小 |

**权限要求**: Authorize

**请求示例**: `GET /api/notification/organization?pageIndex=0&pageSize=5`

**响应示例**:
```json
[
  {
    "organizationId": "org-123",
    "organizationName": "技术开发部",
    "lastVisitTime": "2025-01-29T09:30:00Z",
    "visitCount": 15
  }
]
```

### 3.6 获取未读通知数量
**方法**: GET
**URL**: `/unread-count`

**功能说明**: 获取当前用户的未读通知数量，常用于显示通知徽章。

**请求参数**: 无

**权限要求**: Authorize

**请求示例**: `GET /api/notification/unread-count`

**响应示例**:
```json
{
  "count": 5
}
```

### 3.7 标记已读
**方法**: POST
**URL**: `/read`

**功能说明**: 将指定的通知标记为已读状态。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| notificationIds | Guid[] | ✅ | 要标记已读的通知ID列表 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "notificationIds": [
    "notif-123",
    "notif-456"
  ]
}
```

**响应示例**:
```json
{
  "success": true,
  "markedCount": 2,
  "message": "通知已标记为已读"
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 4. Webhook管理模块 (Admin)

### 基础路径: `/api/webhook`

### 4.1 上传Webhook代码
**方法**: PUT
**URL**: `/code/{webhookId}/{version}`

**功能说明**: 上传指定版本的Webhook代码，支持大文件上传(最大200MB)。只有管理员可以执行此操作。

**Content-Type**: `multipart/form-data`

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| webhookId | string | ✅ | Webhook唯一标识符 |
| version | string | ✅ | 版本号 |
| code | IFormFileCollection | ✅ | 代码文件集合 |

**权限要求**: AdminPolicy

**请求示例**:
```
PUT /api/webhook/code/my-webhook/v1.0.0
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="code"; filename="webhook.zip"
Content-Type: application/zip

[Webhook代码文件二进制数据]
--boundary--
```

**响应示例**:
```json
{
  "success": true,
  "webhookId": "my-webhook",
  "version": "v1.0.0",
  "uploadedAt": "2025-01-29T10:30:00Z",
  "message": "Webhook代码上传成功"
}
```

### 4.2 更新代码
**方法**: PUT
**URL**: `/updateCode`

**功能说明**: 更新当前客户端的Webhook代码，适用于客户端应用的代码更新。

**Content-Type**: `multipart/form-data`

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| code | IFormFileCollection | ✅ | 新的代码文件集合 |

**权限要求**: Authorize

**请求示例**:
```
PUT /api/webhook/updateCode
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="code"; filename="updated-webhook.zip"
Content-Type: application/zip

[更新的Webhook代码文件二进制数据]
--boundary--
```

**响应示例**:
```json
{
  "success": true,
  "clientId": "client-123",
  "version": "1",
  "updatedAt": "2025-01-29T11:00:00Z",
  "message": "代码更新成功"
}
```

### 4.3 获取Webhook代码
**方法**: GET
**URL**: `/code`

**功能说明**: 获取指定Webhook的代码内容，以字符串格式返回。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| webhookId | string | ✅ | Webhook唯一标识符 |
| version | string | ✅ | 版本号 |

**权限要求**: 无

**请求示例**: `GET /api/webhook/code?webhookId=my-webhook&version=v1.0.0`

**响应示例**:
```json
{
  "main.js": "function handler(req, res) { ... }",
  "package.json": "{ \"name\": \"my-webhook\", ... }",
  "config.json": "{ \"timeout\": 30000 }"
}
```

### 4.4 销毁Webhook
**方法**: POST
**URL**: `/destroy`

**功能说明**: 永久销毁指定的Webhook及其所有版本，包括代码和配置。只有管理员可以执行。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| webhookId | string | ✅ | 要销毁的Webhook ID |
| version | string | ✅ | 要销毁的版本号 |

**权限要求**: AdminPolicy

**请求示例**:
```json
{
  "webhookId": "my-webhook",
  "version": "v1.0.0"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "Webhook已成功销毁"
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 5. 订阅管理模块 (Subscription)

### 基础路径: `/api/subscription`

### 5.1 获取可用事件列表
**方法**: GET
**URL**: `/events/{guid}`

**功能说明**: 获取指定代理或系统可以订阅的所有事件类型列表。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 代理或系统标识符 |

**权限要求**: EventManagement.View

**请求示例**: `GET /api/subscription/events/123e4567-e89b-12d3-a456-426614174000`

**响应示例**:
```json
[
  {
    "eventType": "TaskCompleted",
    "description": "任务完成事件",
    "schema": {
      "taskId": "string",
      "result": "string",
      "timestamp": "datetime"
    }
  },
  {
    "eventType": "AgentStatusChanged",
    "description": "代理状态变化事件",
    "schema": {
      "agentId": "guid",
      "oldStatus": "string",
      "newStatus": "string"
    }
  }
]
```

### 5.2 创建订阅
**方法**: POST
**URL**: `/`

**功能说明**: 创建一个新的事件订阅，当指定事件发生时将通知订阅者。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| eventType | string | ✅ | 要订阅的事件类型 |
| subscriberId | Guid | ✅ | 订阅者ID |
| webhookUrl | string? | ❌ | 事件发生时的回调URL |
| filterConditions | object? | ❌ | 事件过滤条件 |

**权限要求**: SubscriptionManagement.CreateSubscription

**请求示例**:
```json
{
  "eventType": "TaskCompleted",
  "subscriberId": "456e7890-e89b-12d3-a456-426614174000",
  "webhookUrl": "https://api.example.com/webhook/task-completed",
  "filterConditions": {
    "taskType": "DataProcessing"
  }
}
```

**响应示例**:
```json
{
  "id": "sub-123",
  "eventType": "TaskCompleted",
  "subscriberId": "456e7890-e89b-12d3-a456-426614174000",
  "webhookUrl": "https://api.example.com/webhook/task-completed",
  "status": "Active",
  "createdAt": "2025-01-29T10:30:00Z"
}
```

### 5.3 取消订阅
**方法**: DELETE
**URL**: `/{subscriptionId}`

**功能说明**: 取消指定的事件订阅，停止接收相关事件通知。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| subscriptionId | Guid | ✅ | 订阅ID |

**权限要求**: SubscriptionManagement.CancelSubscription

**请求示例**: `DELETE /api/subscription/sub-123`

**响应示例**:
```json
{
  "success": true,
  "message": "订阅已成功取消"
}
```

### 5.4 获取订阅状态
**方法**: GET
**URL**: `/{subscriptionId}`

**功能说明**: 获取指定订阅的详细状态信息，包括订阅配置和统计数据。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| subscriptionId | Guid | ✅ | 订阅ID |

**权限要求**: SubscriptionManagement.ViewSubscriptionStatus

**请求示例**: `GET /api/subscription/sub-123`

**响应示例**:
```json
{
  "id": "sub-123",
  "eventType": "TaskCompleted",
  "subscriberId": "456e7890-e89b-12d3-a456-426614174000",
  "webhookUrl": "https://api.example.com/webhook/task-completed",
  "status": "Active",
  "createdAt": "2025-01-29T10:30:00Z",
  "lastEventAt": "2025-01-29T14:20:00Z",
  "eventCount": 25,
  "failureCount": 1
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 6. API密钥管理模块 (AppId)

### 基础路径: `/api/appId`

### 6.1 创建API密钥
**方法**: POST
**URL**: `/`

**功能说明**: 为指定项目创建新的API密钥，用于访问项目相关的API服务。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| projectId | Guid | ✅ | 目标项目ID |
| name | string | ✅ | API密钥名称 |

**权限要求**: ApiKeys.Create

**请求示例**:
```json
{
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "生产环境API密钥"
}
```

**响应示例**:
```json
{
  "id": "appid-123",
  "name": "生产环境API密钥",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "apiKey": "ak-prod-xxxxxxxxxxxxxxxxxxxx",
  "createdAt": "2025-01-29T10:30:00Z",
  "status": "Active"
}
```

### 6.2 获取API密钥列表
**方法**: GET
**URL**: `/{guid}`

**功能说明**: 获取指定项目的所有API密钥列表，不包含密钥的具体值。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | 项目ID |

**权限要求**: ApiKeys.Default

**请求示例**: `GET /api/appId/123e4567-e89b-12d3-a456-426614174000`

**响应示例**:
```json
[
  {
    "id": "appid-123",
    "name": "生产环境API密钥",
    "projectId": "123e4567-e89b-12d3-a456-426614174000",
    "keyPreview": "ak-prod-xxxx...xxxx",
    "createdAt": "2025-01-29T10:30:00Z",
    "lastUsedAt": "2025-01-29T14:20:00Z",
    "status": "Active"
  },
  {
    "id": "appid-456",
    "name": "测试环境API密钥",
    "projectId": "123e4567-e89b-12d3-a456-426614174000",
    "keyPreview": "ak-test-xxxx...xxxx",
    "createdAt": "2025-01-28T16:15:00Z",
    "lastUsedAt": null,
    "status": "Active"
  }
]
```

### 6.3 删除API密钥
**方法**: DELETE
**URL**: `/{guid}`

**功能说明**: 删除指定的API密钥，删除后该密钥将无法再用于API访问。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | API密钥ID |

**权限要求**: Authorize

**请求示例**: `DELETE /api/appId/appid-456`

**响应示例**:
```json
{
  "success": true,
  "message": "API密钥已成功删除"
}
```

### 6.4 修改API密钥名称
**方法**: PUT
**URL**: `/{guid}`

**功能说明**: 修改指定API密钥的显示名称，不影响密钥值本身。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| guid | Guid | ✅ | API密钥ID |
| appName | string | ✅ | 新的名称 |

**权限要求**: Authorize

**请求示例**:
```json
{
  "appName": "更新后的API密钥名称"
}
```

**响应示例**:
```json
{
  "id": "appid-123",
  "name": "更新后的API密钥名称",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "updatedAt": "2025-01-29T15:30:00Z"
}
```

---

[⬆️ 返回目录](#目录--table-of-contents)

## 7. API请求统计模块 (ApiRequest)

### 基础路径: `/api/api-requests`

### 7.1 获取API请求统计
**方法**: GET
**URL**: `/`

**功能说明**: 获取API请求的统计数据，支持按组织或项目维度查看API使用情况。

**请求参数**:
| 参数名 | 类型 | 必须 | 说明 |
|--------|------|------|------|
| organizationId | Guid? | 条件必须 | 组织ID，与projectId二选一 |
| projectId | Guid? | 条件必须 | 项目ID，与organizationId二选一 |
| startDate | DateTime? | ❌ | 统计开始时间 |
| endDate | DateTime? | ❌ | 统计结束时间 |

**权限要求**: ApiRequests.Default

**请求示例**: `GET /api/api-requests?projectId=123e4567-e89b-12d3-a456-426614174000&startDate=2025-01-01&endDate=2025-01-31`

**响应示例**:
```json
{
  "totalRequests": 1250,
  "requests": [
    {
      "endpoint": "/api/agent",
      "method": "POST",
      "count": 450,
      "avgResponseTime": 125.5,
      "errorRate": 0.02
    },
    {
      "endpoint": "/api/plugins",
      "method": "GET",
      "count": 380,
      "avgResponseTime": 88.2,
      "errorRate": 0.01
    },
    {
      "endpoint": "/api/notification",
      "method": "POST",
      "count": 420,
      "avgResponseTime": 156.8,
      "errorRate": 0.03
    }
  ],
  "timeRange": {
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-01-31T23:59:59Z"
  }
}
```

---

## 权限系统说明

### 权限层级
1. **AdminPolicy**: 管理员权限
   - 完全访问系统管理功能
2. **EventManagement**: 事件管理权限
   - View: 查看事件
3. **SubscriptionManagement**: 订阅管理权限
   - CreateSubscription: 创建订阅
   - CancelSubscription: 取消订阅
   - ViewSubscriptionStatus: 查看订阅状态
4. **ApiKeys**: API密钥权限
   - Default: 查看密钥
   - Create: 创建密钥
5. **ApiRequests**: API请求统计权限
   - Default: 查看统计

### 认证方式
- 所有需要权限的接口都需要在请求头中包含 `Authorization: Bearer {token}`
- 某些特殊权限需要额外的角色或权限验证

---

## 错误处理

### 常见HTTP状态码
- **200**: 请求成功
- **400**: 请求参数错误
- **401**: 未认证
- **403**: 权限不足
- **404**: 资源不存在
- **413**: 文件过大
- **422**: 参数验证失败
- **500**: 服务器内部错误

### 错误响应格式
```json
{
  "error": {
    "code": "ValidationError",
    "message": "请求参数验证失败",
    "details": "AgentType is required",
    "validationErrors": [
      {
        "field": "agentType",
        "message": "代理类型不能为空"
      }
    ]
  }
}
```

---

## 版本信息
- **文档版本**: v2.0
- **API版本**: v1
- **更新日期**: 2025-01-29
- **生成时间**: 自动生成基于代码分析

---

[⬆️ 返回目录](#目录--table-of-contents)

*此文档基于AevatarStation项目的控制器代码自动生成，包含7个核心业务模块的详细API接口信息。每个接口都包含完整的功能说明、参数定义、请求示例和响应格式。* 