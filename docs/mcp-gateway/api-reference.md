# MCP Gateway API Reference

## English Documentation

### Base URL
```
https://your-aevatar-instance.com/api/mcp-gateway
```

### Authentication
All endpoints require authentication using Bearer tokens:
```
Authorization: Bearer <your-access-token>
```

### API Endpoints

#### **Adapter Management**

##### Create Adapter
```http
POST /adapters
Content-Type: application/json

{
  "name": "my-adapter",
  "imageName": "my-mcp-server",
  "imageVersion": "1.0.0",
  "description": "My custom MCP server",
  "environment": {
    "ENV_VAR": "value"
  },
  "tags": ["production", "ai"],
  "metadata": {
    "owner": "team-ai"
  },
  "resourceLimits": {
    "cpuLimit": 1.0,
    "memoryLimitMB": 512,
    "maxConnections": 100
  }
}
```

**Response:**
```json
{
  "id": "my-adapter",
  "name": "my-adapter",
  "imageName": "my-mcp-server",
  "imageVersion": "1.0.0",
  "status": "Creating",
  "isHealthy": false,
  "activeConnections": 0,
  "creationTime": "2025-09-08T10:30:00Z"
}
```

##### List Adapters
```http
GET /adapters?search=my&status=Running&isHealthy=true&maxResultCount=10&skipCount=0&sorting=name asc
```

**Query Parameters:**
- `search` (string, optional): Search by name or description
- `status` (enum, optional): Filter by adapter status (Creating, Running, Stopped, Failed, Updating, Deleting)
- `isHealthy` (boolean, optional): Filter by health status
- `tags` (array, optional): Filter by tags
- `maxResultCount` (int, optional): Maximum number of results (default: 10)
- `skipCount` (int, optional): Number of results to skip (default: 0)
- `sorting` (string, optional): Sort field and direction (e.g., "name asc", "creationTime desc")

**Response:**
```json
{
  "totalCount": 25,
  "items": [
    {
      "id": "my-adapter",
      "name": "my-adapter",
      "status": "Running",
      "isHealthy": true,
      "activeConnections": 5
    }
  ]
}
```

##### Get Adapter
```http
GET /adapters/{name}
```

##### Update Adapter
```http
PUT /adapters/{name}
Content-Type: application/json

{
  "imageVersion": "1.1.0",
  "description": "Updated description",
  "resourceLimits": {
    "memoryLimitMB": 1024
  }
}
```

##### Delete Adapter
```http
DELETE /adapters/{name}
```

#### **Monitoring & Status**

##### Get Adapter Status
```http
GET /adapters/{name}/status
```

**Response:**
```json
{
  "name": "my-adapter",
  "status": "Running",
  "isHealthy": true,
  "activeConnections": 5,
  "uptimeSeconds": 3600,
  "lastHealthCheck": "2025-09-08T10:30:00Z",
  "resourceUsage": {
    "cpuUsagePercent": 25.5,
    "memoryUsageMB": 256,
    "networkIO": {
      "bytesReceived": 1048576,
      "bytesSent": 524288
    }
  }
}
```

##### Get Adapter Logs
```http
GET /adapters/{name}/logs?lines=100&follow=false
```

**Query Parameters:**
- `lines` (int, optional): Number of log lines to retrieve
- `follow` (boolean, optional): Whether to follow logs in real-time

**Response:**
```json
[
  "2025-09-08T10:30:00Z INFO Starting MCP server...",
  "2025-09-08T10:30:01Z INFO Server listening on port 3000",
  "2025-09-08T10:30:02Z INFO Ready to accept connections"
]
```

##### Get Adapter Metrics
```http
GET /adapters/{name}/metrics?from=2025-09-08T09:00:00Z&to=2025-09-08T10:00:00Z
```

##### Test Adapter Connection
```http
POST /adapters/{name}/test
```

**Response:**
```json
{
  "isSuccessful": true,
  "latencyMs": 125.5,
  "testedAt": "2025-09-08T10:30:00Z",
  "details": {
    "endpoint": "http://gateway.com/adapters/my-adapter/mcp",
    "responseCode": 200
  }
}
```

#### **Gateway Health**

##### Get Gateway Health
```http
GET /gateway/health
```

**Response:**
```json
{
  "isHealthy": true,
  "version": "1.0.0",
  "uptimeSeconds": 86400,
  "totalAdapters": 10,
  "healthyAdapters": 9,
  "totalActiveConnections": 50,
  "checkedAt": "2025-09-08T10:30:00Z"
}
```

### Error Responses

All endpoints return standardized error responses:

```json
{
  "error": {
    "code": "ADAPTER_NOT_FOUND",
    "message": "Adapter 'my-adapter' not found",
    "details": "The specified adapter does not exist in the gateway"
  },
  "timestamp": "2025-09-08T10:30:00Z",
  "path": "/api/mcp-gateway/adapters/my-adapter"
}
```

### Status Codes

- `200 OK` - Success
- `201 Created` - Resource created successfully
- `204 No Content` - Success with no response body
- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource already exists
- `500 Internal Server Error` - Server error

---

## 中文文档

### 基础URL
```
https://your-aevatar-instance.com/api/mcp-gateway
```

### 身份验证
所有端点都需要使用Bearer令牌进行身份验证：
```
Authorization: Bearer <your-access-token>
```

### API端点

#### **适配器管理**

##### 创建适配器
```http
POST /adapters
Content-Type: application/json

{
  "name": "我的适配器",
  "imageName": "my-mcp-server",
  "imageVersion": "1.0.0",
  "description": "我的自定义MCP服务器",
  "environment": {
    "ENV_VAR": "value"
  },
  "tags": ["生产环境", "AI"],
  "metadata": {
    "owner": "AI团队"
  },
  "resourceLimits": {
    "cpuLimit": 1.0,
    "memoryLimitMB": 512,
    "maxConnections": 100
  }
}
```

##### 获取适配器列表
```http
GET /adapters?search=我的&status=Running&isHealthy=true&maxResultCount=10&skipCount=0&sorting=name asc
```

**查询参数：**
- `search` (字符串，可选): 按名称或描述搜索
- `status` (枚举，可选): 按适配器状态过滤（Creating创建中, Running运行中, Stopped已停止, Failed失败, Updating更新中, Deleting删除中）
- `isHealthy` (布尔值，可选): 按健康状态过滤
- `tags` (数组，可选): 按标签过滤
- `maxResultCount` (整数，可选): 最大结果数量（默认：10）
- `skipCount` (整数，可选): 跳过的结果数量（默认：0）
- `sorting` (字符串，可选): 排序字段和方向（例如："name asc", "creationTime desc"）

##### 获取适配器详情
```http
GET /adapters/{name}
```

##### 更新适配器
```http
PUT /adapters/{name}
Content-Type: application/json

{
  "imageVersion": "1.1.0",
  "description": "更新的描述",
  "resourceLimits": {
    "memoryLimitMB": 1024
  }
}
```

##### 删除适配器
```http
DELETE /adapters/{name}
```

#### **监控和状态**

##### 获取适配器状态
```http
GET /adapters/{name}/status
```

**响应示例：**
```json
{
  "name": "我的适配器",
  "status": "Running",
  "isHealthy": true,
  "activeConnections": 5,
  "uptimeSeconds": 3600,
  "lastHealthCheck": "2025-09-08T10:30:00Z",
  "resourceUsage": {
    "cpuUsagePercent": 25.5,
    "memoryUsageMB": 256,
    "networkIO": {
      "bytesReceived": 1048576,
      "bytesSent": 524288
    }
  }
}
```

##### 获取适配器日志
```http
GET /adapters/{name}/logs?lines=100&follow=false
```

**查询参数：**
- `lines` (整数，可选): 要检索的日志行数
- `follow` (布尔值，可选): 是否实时跟踪日志

##### 获取适配器指标
```http
GET /adapters/{name}/metrics?from=2025-09-08T09:00:00Z&to=2025-09-08T10:00:00Z
```

##### 测试适配器连接
```http
POST /adapters/{name}/test
```

#### **网关健康状态**

##### 获取网关健康状态
```http
GET /gateway/health
```

### 错误响应

所有端点返回标准化的错误响应：

```json
{
  "error": {
    "code": "ADAPTER_NOT_FOUND",
    "message": "未找到适配器 'my-adapter'",
    "details": "指定的适配器在网关中不存在"
  },
  "timestamp": "2025-09-08T10:30:00Z",
  "path": "/api/mcp-gateway/adapters/my-adapter"
}
```

### 状态码

- `200 OK` - 成功
- `201 Created` - 资源创建成功
- `204 No Content` - 成功，无响应体
- `400 Bad Request` - 无效的请求参数
- `401 Unauthorized` - 需要身份验证
- `403 Forbidden` - 权限不足
- `404 Not Found` - 资源未找到
- `409 Conflict` - 资源已存在
- `500 Internal Server Error` - 服务器错误
