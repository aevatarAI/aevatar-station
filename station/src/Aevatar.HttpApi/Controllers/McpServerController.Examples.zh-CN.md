# MCP 服务器控制器 API 使用示例

## 概述
MCP 服务器控制器提供了完整的 CRUD 操作来管理 MCP（模型上下文协议）服务器。所有端点都需要相应的权限。

## 基础URL
```
/api/mcp/servers
```

## 身份验证与授权
所有端点都需要身份验证和特定的 MCP 服务器权限：
- `AevatarPermissions.McpServers.Default` - 查看服务器
- `AevatarPermissions.McpServers.Create` - 创建服务器
- `AevatarPermissions.McpServers.Edit` - 更新服务器
- `AevatarPermissions.McpServers.Delete` - 删除服务器

## API 使用示例

### 1. 创建新的 MCP 服务器

**网络服务器（带URL）：**
```http
POST /api/mcp/servers
Content-Type: application/json
Authorization: Bearer {token}

{
  "serverName": "example-server",
  "command": "python",
  "args": ["-m", "mcp_server", "--port", "3000"],
  "env": {
    "NODE_ENV": "production",
    "MCP_PORT": "3000"
  },
  "description": "演示用的示例 MCP 服务器",
  "url": "http://localhost:3000"
}
```

**标准IO服务器（无需URL）：**
```http
POST /api/mcp/servers
Content-Type: application/json
Authorization: Bearer {token}

{
  "serverName": "stdio-server",
  "command": "node",
  "args": ["mcp-server.js"],
  "env": {
    "NODE_ENV": "production"
  },
  "description": "示例标准IO MCP 服务器"
}
```

### 2. 获取所有服务器（支持分页和筛选）

**基本分页请求：**
```http
GET /api/mcp/servers?pageNumber=1&maxResultCount=10&sorting=serverName asc
Authorization: Bearer {token}
```

**高级筛选和分页：**
```http
GET /api/mcp/servers?searchTerm=example&serverType=StreamableHttp&pageNumber=2&maxResultCount=20&sorting=command desc
Authorization: Bearer {token}
```

**按标准IO服务器筛选：**
```http
GET /api/mcp/servers?serverType=Stdio&pageNumber=1&maxResultCount=10&sorting=createdAt desc
Authorization: Bearer {token}
```

**使用传统分页参数：**
```http
GET /api/mcp/servers?skipCount=20&maxResultCount=10&sorting=description asc
Authorization: Bearer {token}
```

### 3. 获取特定服务器

```http
GET /api/mcp/servers/example-server
Authorization: Bearer {token}
```

### 4. 更新现有服务器

```http
PUT /api/mcp/servers/example-server
Content-Type: application/json
Authorization: Bearer {token}

{
  "command": "node",
  "args": ["server.js"],
  "description": "更新后的 MCP 服务器描述",
  "env": {
    "NODE_ENV": "development"
  }
}
```

### 5. 删除服务器

```http
DELETE /api/mcp/servers/example-server
Authorization: Bearer {token}
```

### 6. 获取所有服务器名称

```http
GET /api/mcp/servers/names
Authorization: Bearer {token}
```

### 7. 获取原始配置（向后兼容）

```http
GET /api/mcp/servers/raw
Authorization: Bearer {token}
```

## 数据类型

### 服务器类型检测
服务器类型根据 URL 字段自动确定：
- **Stdio**：当 `url` 为 null 或空时 - 通过标准输入输出进行通信
- **StreamableHttp**：当提供 `url` 时 - 通过网络协议进行通信（SSE/WebSocket/HTTP）

### 服务器配置结构
```json
{
  "serverName": "string",           // 服务器的唯一标识符（最大50字符）
  "command": "string",              // 可执行命令（最大20字符，如 "python", "node"）
  "args": ["string[]"],             // 命令行参数
  "env": {"key": "value"},          // 环境变量键值对
  "description": "string",          // 可读的描述
  "url": "string (可选)",           // 服务器URL（null/空 = Stdio，存在 = StreamableHttp）
  "serverType": "string",           // 自动计算："Stdio" 或 "StreamableHttp"（只读）
  "createdAt": "datetime",          // 创建时间戳
  "modifiedAt": "datetime"          // 最后修改时间戳（可选）
}
```

### 字段映射到 MCPServerConfig
此 API 直接映射到底层的 `MCPServerConfig` 结构：
- ✅ `ServerName` → `serverName`
- ✅ `Command` → `command` 
- ✅ `Args` → `args`
- ✅ `Env` → `env`
- ✅ `Description` → `description`
- ✅ `Url` → `url`

## 错误响应

API 返回标准的 HTTP 状态码和用户友好的错误消息：

- `400 Bad Request` - 无效的输入数据或验证错误
- `401 Unauthorized` - 缺失或无效的身份验证
- `403 Forbidden` - 权限不足
- `404 Not Found` - 服务器未找到
- `409 Conflict` - 服务器名称已存在
- `500 Internal Server Error` - 服务器处理错误

错误响应示例：
```json
{
  "error": {
    "message": "MCP server 'example-server' already exists",
    "details": null
  }
}
```

## 分页和排序功能

### 分页参数
- **pageNumber**：页码（从1开始，自动计算skipCount）
- **maxResultCount**：每页大小（1-100，默认10）
- **skipCount**：跳过记录数（传统分页方式）

### 排序参数
- **sorting**：排序字段和方向，格式："字段名 方向"
- **支持的排序字段**：
  - `serverName` - 服务器名称
  - `command` - 命令
  - `description` - 描述
  - `serverType` - 服务器类型
  - `createdAt` - 创建时间
  - `modifiedAt` - 修改时间
- **排序方向**：`asc`（升序，默认）或 `desc`（降序）

### 分页响应结构
```json
{
  "totalCount": 100,           // 总记录数
  "items": [                   // 当前页数据
    {
      "serverName": "example",
      "command": "python",
      // ... 其他字段
    }
  ]
}
```

### 排序示例
```http
# 按服务器名称升序
GET /api/mcp/servers?sorting=serverName asc

# 按创建时间降序
GET /api/mcp/servers?sorting=createdAt desc

# 按命令升序（默认方向）
GET /api/mcp/servers?sorting=command
```

## 验证规则

### 字段验证
- **服务器名称**：必填，1-50字符
- **命令**：必填，1-20字符
- **描述**：可选，最大1000字符
- **URL**：可选，必须是有效的URL格式
- **参数**：可选字符串数组
- **环境变量**：可选键值对字典

### 分页验证
- **每页大小**：必须在1-100之间
- **跳过记录数**：不能为负数
- **页码**：必须大于0

### 业务规则
- 服务器名称在系统中必须唯一
- 命令路径必须有效且可访问
- URL格式必须符合标准URL规范

## 最佳实践

1. **唯一服务器名称**：每个服务器在系统中必须有唯一的名称。

2. **命令验证**：确保命令路径有效且可访问。

3. **环境变量**：为可能在不同环境之间变化的配置使用环境变量。

4. **服务器类型**：系统自动确定服务器类型：
   - 使用 **Stdio** 用于基于进程的服务器（无需URL）
   - 使用 **StreamableHttp** 用于基于网络的服务器（需要URL），支持 SSE、WebSocket 或 HTTP 协议

5. **错误处理**：在客户端应用程序中始终优雅地处理潜在错误。

6. **安全性**：确保所有 API 调用都有适当的身份验证和授权。

## 常见使用场景

### 场景1：设置标准IO MCP服务器
```json
{
  "serverName": "local-mcp",
  "command": "python",
  "args": ["-m", "my_mcp_server"],
  "description": "本地MCP服务器"
}
```

### 场景2：设置网络MCP服务器
```json
{
  "serverName": "remote-mcp",
  "command": "node",
  "args": ["server.js", "--port", "8080"],
  "url": "http://remote-server:8080",
  "description": "远程MCP服务器"
}
```

### 场景3：带环境变量的服务器
```json
{
  "serverName": "env-mcp",
  "command": "python",
  "args": ["-m", "mcp_server"],
  "env": {
    "DEBUG": "true",
    "API_KEY": "your-api-key",
    "LOG_LEVEL": "info"
  },
  "description": "带环境配置的MCP服务器"
}
```

## 故障排除

### 常见问题

1. **服务器名称已存在**
   - 错误：409 Conflict
   - 解决：选择不同的服务器名称

2. **命令未找到**
   - 错误：400 Bad Request
   - 解决：确保命令路径正确且可执行

3. **URL格式无效**
   - 错误：400 Bad Request
   - 解决：检查URL格式是否符合标准

4. **权限不足**
   - 错误：403 Forbidden
   - 解决：确保用户具有相应的MCP服务器权限

### 调试建议

1. 使用 `/api/mcp/servers/raw` 端点查看原始配置
2. 检查服务器日志以获取详细的错误信息
3. 验证命令在目标环境中是否可执行
4. 确认网络服务器的URL可达性