# AevatarStation API 接口设计文档

## 概述

AevatarStation是一个基于.NET的微服务平台，提供了专业的RESTful API接口用于代理系统、插件管理、通知系统、Webhook管理等核心业务功能。

### 基础信息
- **基础URL**: `/api`
- **认证方式**: Bearer Token (JWT)
- **内容类型**: `application/json`
- **API版本**: v1

### 通用响应格式
所有接口都遵循标准的RESTful响应格式，包含标准的错误处理和分页支持。

---

## 1. 代理系统模块 (Agent)

### 基础路径: `/api/agent`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| GET | `/agent-type-info-list` | 获取所有代理类型 | 无 | Authorize |
| GET | `/agent-list` | 获取代理实例列表 | `pageIndex, pageSize` | Authorize |
| POST | `/` | 创建代理 | `CreateAgentInputDto` | Authorize |
| GET | `/{guid}` | 获取代理详情 | `guid: Guid` | Authorize |
| GET | `/{guid}/relationship` | 获取代理关系 | `guid: Guid` | Authorize |
| POST | `/{guid}/add-subagent` | 添加子代理 | `guid: Guid, AddSubAgentDto` | Authorize |
| POST | `/{guid}/remove-subagent` | 移除子代理 | `guid: Guid, RemoveSubAgentDto` | Authorize |
| POST | `/{guid}/remove-all-subagent` | 移除所有子代理 | `guid: Guid` | Authorize |
| PUT | `/{guid}` | 更新代理 | `guid: Guid, UpdateAgentInputDto` | Authorize |
| DELETE | `/{guid}` | 删除代理 | `guid: Guid` | Authorize |
| POST | `/publishEvent` | 发布事件 | `PublishEventDto` | Authorize |

#### 参数说明
- **CreateAgentInputDto**: 代理创建信息
- **UpdateAgentInputDto**: 代理更新信息
- **AddSubAgentDto**: 子代理添加信息
- **RemoveSubAgentDto**: 子代理移除信息
- **PublishEventDto**: 事件发布信息

---

## 2. 插件管理模块 (Plugin)

### 基础路径: `/api/plugins`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| GET | `/` | 获取插件列表 | `GetPluginDto` | Authorize |
| POST | `/` | 创建插件 | `CreatePluginDto` (Form) | Authorize |
| PUT | `/{id}` | 更新插件 | `id: Guid, UpdatePluginDto` (Form) | Authorize |
| DELETE | `/{id}` | 删除插件 | `id: Guid` | Authorize |

#### 参数说明
- **GetPluginDto**: 插件查询条件
- **CreatePluginDto**: 插件创建信息（包含文件上传，最大15MB）
- **UpdatePluginDto**: 插件更新信息（包含文件上传）

---

## 3. 通知管理模块 (Notification)

### 基础路径: `/api/notification`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| POST | `/` | 创建通知 | `CreateNotificationDto` | Authorize |
| POST | `/withdraw/{guid}` | 撤回通知 | `guid: Guid` | Authorize |
| POST | `/response` | 响应通知 | `NotificationResponseDto` | Authorize |
| GET | `/` | 获取通知列表 | `pageIndex, pageSize` | Authorize |
| GET | `/organization` | 获取组织访问信息 | `pageIndex, pageSize` | Authorize |
| GET | `/unread-count` | 获取未读通知数量 | 无 | Authorize |
| POST | `/read` | 标记已读 | `ReadNotificationDto` | Authorize |

#### 参数说明
- **CreateNotificationDto**: 通知创建信息（包含目标用户、内容等）
- **NotificationResponseDto**: 通知响应信息
- **ReadNotificationDto**: 标记已读信息

---

## 4. Webhook管理模块 (Admin)

### 基础路径: `/api/webhook`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| PUT | `/code/{webhookId}/{version}` | 上传Webhook代码 | `webhookId, version, CreateWebhookDto` (Form) | AdminPolicy |
| PUT | `/updateCode` | 更新代码 | `CreateWebhookDto` (Form) | Authorize |
| GET | `/code` | 获取Webhook代码 | `webhookId, version` | 无 |
| POST | `/destroy` | 销毁Webhook | `DestroyWebhookDto` | AdminPolicy |

#### 参数说明
- **CreateWebhookDto**: Webhook创建信息（包含代码文件，最大200MB）
- **DestroyWebhookDto**: Webhook销毁信息

---

## 5. 订阅管理模块 (Subscription)

### 基础路径: `/api/subscription`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| GET | `/events/{guid}` | 获取可用事件列表 | `guid: Guid` | EventManagement.View |
| POST | `/` | 创建订阅 | `CreateSubscriptionDto` | SubscriptionManagement.CreateSubscription |
| DELETE | `/{subscriptionId}` | 取消订阅 | `subscriptionId: Guid` | SubscriptionManagement.CancelSubscription |
| GET | `/{subscriptionId}` | 获取订阅状态 | `subscriptionId: Guid` | SubscriptionManagement.ViewSubscriptionStatus |

#### 参数说明
- **CreateSubscriptionDto**: 订阅创建信息

---

## 6. API密钥管理模块 (AppId)

### 基础路径: `/api/appId`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| POST | `/` | 创建API密钥 | `CreateAppIdDto` | ApiKeys.Create |
| GET | `/{guid}` | 获取API密钥列表 | `guid: Guid` | ApiKeys.Default |
| DELETE | `/{guid}` | 删除API密钥 | `guid: Guid` | Authorize |
| PUT | `/{guid}` | 修改API密钥名称 | `guid: Guid, ModifyAppNameDto` | Authorize |

#### 参数说明
- **CreateAppIdDto**: API密钥创建信息（包含ProjectId、Name）
- **ModifyAppNameDto**: API密钥名称修改信息

---

## 7. API请求统计模块 (ApiRequest)

### 基础路径: `/api/api-requests`

| 方法 | URL | 描述 | 参数 | 权限要求 |
|------|-----|------|------|----------|
| GET | `/` | 获取API请求统计 | `GetApiRequestDto` | ApiRequests.Default |

#### 参数说明
- **GetApiRequestDto**: API请求查询条件（OrganizationId或ProjectId）

---

## 权限系统说明

### 权限层级
1. **AdminPolicy**: 管理员权限
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
- **500**: 服务器内部错误

### 错误响应格式
```json
{
  "error": {
    "code": "ErrorCode",
    "message": "Error message",
    "details": "Detailed error information"
  }
}
```

---

## 版本信息
- **文档版本**: v1.0
- **API版本**: v1
- **更新日期**: 2025-01-29
- **生成时间**: 自动生成基于代码分析

---

*此文档基于AevatarStation项目的控制器代码自动生成，包含所有可用的REST API接口信息。* 