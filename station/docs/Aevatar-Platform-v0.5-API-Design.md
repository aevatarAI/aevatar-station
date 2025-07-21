# Aevatar Platform v0.5 接口设计文档

## 1. 背景

v0.5 版本聚焦于 **AI 工作流编排能力** 的核心实现，旨在提供直观的工作流生成接口，支持自然语言到可视化工作流的智能转换，建立标准化的 Agent 发现和索引机制。

---

## 2. 目标

### 2.1 核心目标

1. **简化工作流创建**: 用户通过自然语言描述即可生成复杂的 AI 工作流
2. **智能 Agent 编排**: 自动发现和配置最适合的 AI Agent 组合
3. **可视化工作流**: 生成前端可直接渲染的工作流配置
4. **用户体验优化**: 提供响应式、高性能的 API 服务

### 2.2 技术目标

- **高可用性**: 99.9% 服务可用时间
- **低延迟**: API 响应时间 < 500ms (P95)
- **高并发**: 支持 1000+ 并发用户
- **可扩展性**: 支持横向扩展

---

## 3. 接口设计

### 3.1 工作流生成接口

#### 接口介绍

工作流生成接口是 v0.5 版本的核心功能，它接收用户的自然语言目标描述，通过 AI 智能分析和 Agent 发现机制，自动生成可视化的工作流配置。该接口整合了多个内部服务：

- **WorkflowComposerGAgent**: 核心 AI 工作流生成引擎
- **AgentIndexService**: Agent 发现和索引服务  
- **WorkflowJsonValidatorService**: 工作流配置验证服务

#### API 规范

**接口路径**: `/api/workflow/generate`

**HTTP 方法**: `POST`

**认证方式**: Bearer Token (JWT)

**内容类型**: `application/json`

#### 请求参数

##### 请求体结构

```json
{
  "userGoal": "string"
}
```

##### 参数说明

| 参数名称 | 类型 | 必填 | 描述 | 示例 |
|---------|------|------|------|------|
| `userGoal` | string | ✅ | 用户目标的自然语言描述，支持中英文 | "分析销售数据并生成可视化报告" |

#### 返回体

##### 成功响应 (200 OK)

```json
{
  "success": true,
  "data": {
    "name": "string",
    "workflowNodeList": [
      {
        "nodeId": "string",
        "agentType": "string", 
        "name": "string",
        "properties": {
          "key": "value"
        },
        "extendedData": {
          "positionX": "string",
          "positionY": "string",
          "width": "string",
          "height": "string"
        }
      }
    ],
    "workflowNodeUnitList": [
      {
        "nodeId": "string",
        "nextnodeId": "string"
      }
    ]
  },
  "message": "Workflow generated successfully",
  "timestamp": "2025-01-29T10:30:00Z"
}
```

##### 响应字段说明

| 字段名称 | 类型 | 描述 |
|---------|------|------|
| `success` | boolean | 请求是否成功 |
| `data` | object | 工作流配置数据 |
| `data.name` | string | 工作流名称 |
| `data.workflowNodeList` | array | 工作流节点列表 |
| `data.workflowNodeUnitList` | array | 节点连接关系列表 |
| `message` | string | 响应消息 |
| `timestamp` | string | 响应时间戳 (ISO 8601) |

##### WorkflowNode 字段详情

| 字段名称 | 类型 | 描述 | 示例 |
|---------|------|------|------|
| `nodeId` | string | 节点唯一标识符 | `"node_001"` |
| `agentType` | string | Agent 类型名称 | `"DataProcessorAgent"` |
| `name` | string | 节点显示名称 | `"数据处理器"` |
| `properties` | object | Agent 配置参数 | `{"inputFormat": "csv"}` |
| `extendedData` | object | 前端渲染所需的扩展信息 | 位置、尺寸等 | 