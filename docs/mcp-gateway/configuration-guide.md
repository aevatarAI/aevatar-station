# MCP Gateway Configuration Guide

## English Documentation

### Overview

This guide covers all configuration options for integrating Aevatar with Microsoft MCP Gateway. The configuration enables seamless connection management, session-aware routing, and comprehensive adapter lifecycle management.

### Configuration Structure

#### 1. **Gateway Configuration (appsettings.json)**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://your-mcp-gateway.com",
    "AuthToken": "Bearer your-gateway-auth-token",
    "RequestTimeout": "00:00:30",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "EnableDetailedLogging": false,
    "HealthCheckPath": "/health",
    "DefaultHeaders": {
      "X-Client-Name": "Aevatar",
      "X-Environment": "production"
    }
  }
}
```

#### 2. **MCPGAgent Configuration**

```csharp
var config = new MCPGAgentConfig
{
    RequestTimeout = TimeSpan.FromSeconds(30),
    ServerConfig = new MCPServerConfig
    {
        ServerName = "filesystem-server",
        UseGateway = true,
        GatewayAdapterName = "filesystem-adapter",
        SessionId = "user-session-123", // Optional, auto-generated if not provided
        Priority = 75,
        Tags = new List<string> { "filesystem", "tools" },
        Metadata = new Dictionary<string, string>
        {
            ["category"] = "file-operations",
            ["version"] = "1.0.0"
        }
    }
};
```

#### 3. **Environment Variables**

```bash
# Gateway Configuration
MCP_GATEWAY_BASE_URL=https://your-mcp-gateway.com
MCP_GATEWAY_AUTH_TOKEN=Bearer your-gateway-auth-token
MCP_GATEWAY_REQUEST_TIMEOUT=30
MCP_GATEWAY_MAX_RETRY_ATTEMPTS=3
MCP_GATEWAY_ENABLE_SESSION_AFFINITY=true
MCP_GATEWAY_ENABLE_DETAILED_LOGGING=false

# Adapter Configuration
MCP_ADAPTER_USE_GATEWAY=true
MCP_ADAPTER_PRIORITY=50
```

### Configuration Options

#### **MCPGatewayConfig Properties**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GatewayBaseUrl` | string | "" | Base URL of the MCP Gateway service |
| `AuthToken` | string | "" | Authentication token for gateway API access |
| `RequestTimeout` | TimeSpan | 30s | Timeout for gateway requests |
| `EnableSessionAffinity` | bool | true | Enable session-aware routing |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts for failed requests |
| `RetryDelay` | TimeSpan | 1s | Delay between retry attempts |
| `EnableDetailedLogging` | bool | false | Enable detailed logging for debugging |
| `HealthCheckPath` | string | "/health" | Health check endpoint path |
| `DefaultHeaders` | Dictionary | {} | Default headers for all requests |

#### **MCPServerConfig Properties**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServerName` | string | "" | Unique server identifier |
| `UseGateway` | bool | true | Whether to use gateway routing |
| `GatewayAdapterName` | string | null | Gateway adapter name for routing |
| `SessionId` | string | null | Session ID (auto-generated if null) |
| `Priority` | int | 50 | Load balancing priority (1-100) |
| `Tags` | List<string> | [] | Tags for categorization and filtering |
| `Metadata` | Dictionary | {} | Additional metadata |
| `Command` | string | "" | Command for direct connections (fallback) |
| `Args` | List<string> | [] | Arguments for direct connections |
| `Url` | string | null | URL for direct SSE connections |

### Configuration Examples

#### **Development Environment**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "http://localhost:8000",
    "AuthToken": "dev-token-12345",
    "RequestTimeout": "00:00:10",
    "EnableDetailedLogging": true,
    "MaxRetryAttempts": 1
  }
}
```

#### **Production Environment**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://mcp-gateway.prod.company.com",
    "AuthToken": "Bearer prod-jwt-token-xyz",
    "RequestTimeout": "00:01:00",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:02",
    "DefaultHeaders": {
      "X-Environment": "production",
      "X-Team": "ai-platform"
    }
  }
}
```

#### **Hybrid Configuration (Gateway + Direct Fallback)**

```csharp
var hybridConfig = new MCPServerConfig
{
    ServerName = "hybrid-server",
    UseGateway = true,
    GatewayAdapterName = "hybrid-adapter",
    
    // Fallback configuration for direct connection
    Command = "node",
    Args = new List<string> { "mcp-server.js", "--port", "3000" },
    Url = "http://localhost:3000"
};
```

### Validation Rules

#### **MCPGatewayConfig Validation**
- `GatewayBaseUrl`: Must be a valid HTTP/HTTPS URL
- `AuthToken`: Must be 10-1000 characters long
- `RequestTimeout`: Must be between 1 second and 10 minutes
- `MaxRetryAttempts`: Must be between 0 and 10
- `RetryDelay`: Must be between 100ms and 1 minute

#### **MCPServerConfig Validation**
- `ServerName`: Required, 1-100 characters
- `GatewayAdapterName`: Required when `UseGateway=true`, max 100 characters
- `SessionId`: Optional, max 200 characters
- `Priority`: Must be between 1 and 100
- For direct connections: Either `Command` or `Url` is required

---

## 中文文档

### 概述

本指南涵盖了将Aevatar与Microsoft MCP Gateway集成的所有配置选项。该配置支持无缝连接管理、会话感知路由和全面的适配器生命周期管理。

### 配置结构

#### 1. **网关配置 (appsettings.json)**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://your-mcp-gateway.com",
    "AuthToken": "Bearer your-gateway-auth-token",
    "RequestTimeout": "00:00:30",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "EnableDetailedLogging": false,
    "HealthCheckPath": "/health",
    "DefaultHeaders": {
      "X-Client-Name": "Aevatar",
      "X-Environment": "production"
    }
  }
}
```

#### 2. **MCPGAgent配置**

```csharp
var config = new MCPGAgentConfig
{
    RequestTimeout = TimeSpan.FromSeconds(30),
    ServerConfig = new MCPServerConfig
    {
        ServerName = "文件系统服务器",
        UseGateway = true,
        GatewayAdapterName = "filesystem-adapter",
        SessionId = "用户会话-123", // 可选，如果未提供则自动生成
        Priority = 75,
        Tags = new List<string> { "文件系统", "工具" },
        Metadata = new Dictionary<string, string>
        {
            ["category"] = "文件操作",
            ["version"] = "1.0.0"
        }
    }
};
```

#### 3. **环境变量**

```bash
# 网关配置
MCP_GATEWAY_BASE_URL=https://your-mcp-gateway.com
MCP_GATEWAY_AUTH_TOKEN=Bearer your-gateway-auth-token
MCP_GATEWAY_REQUEST_TIMEOUT=30
MCP_GATEWAY_MAX_RETRY_ATTEMPTS=3
MCP_GATEWAY_ENABLE_SESSION_AFFINITY=true
MCP_GATEWAY_ENABLE_DETAILED_LOGGING=false

# 适配器配置
MCP_ADAPTER_USE_GATEWAY=true
MCP_ADAPTER_PRIORITY=50
```

### 配置选项

#### **MCPGatewayConfig属性**

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `GatewayBaseUrl` | string | "" | MCP Gateway服务的基础URL |
| `AuthToken` | string | "" | 网关API访问的身份验证令牌 |
| `RequestTimeout` | TimeSpan | 30s | 网关请求的超时时间 |
| `EnableSessionAffinity` | bool | true | 启用会话感知路由 |
| `MaxRetryAttempts` | int | 3 | 失败请求的最大重试次数 |
| `RetryDelay` | TimeSpan | 1s | 重试尝试之间的延迟 |
| `EnableDetailedLogging` | bool | false | 启用详细日志记录用于调试 |
| `HealthCheckPath` | string | "/health" | 健康检查端点路径 |
| `DefaultHeaders` | Dictionary | {} | 所有请求的默认头部 |

#### **MCPServerConfig属性**

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `ServerName` | string | "" | 唯一的服务器标识符 |
| `UseGateway` | bool | true | 是否使用网关路由 |
| `GatewayAdapterName` | string | null | 用于路由的网关适配器名称 |
| `SessionId` | string | null | 会话ID（如果为null则自动生成） |
| `Priority` | int | 50 | 负载均衡优先级（1-100） |
| `Tags` | List<string> | [] | 用于分类和过滤的标签 |
| `Metadata` | Dictionary | {} | 附加元数据 |
| `Command` | string | "" | 直连命令（备用） |
| `Args` | List<string> | [] | 直连参数 |
| `Url` | string | null | 直连SSE的URL |

### 配置示例

#### **开发环境**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "http://localhost:8000",
    "AuthToken": "dev-token-12345",
    "RequestTimeout": "00:00:10",
    "EnableDetailedLogging": true,
    "MaxRetryAttempts": 1
  }
}
```

#### **生产环境**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://mcp-gateway.prod.company.com",
    "AuthToken": "Bearer prod-jwt-token-xyz",
    "RequestTimeout": "00:01:00",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:02",
    "DefaultHeaders": {
      "X-Environment": "production",
      "X-Team": "ai-platform"
    }
  }
}
```

### 验证规则

#### **MCPGatewayConfig验证**
- `GatewayBaseUrl`: 必须是有效的HTTP/HTTPS URL
- `AuthToken`: 必须是10-1000字符长
- `RequestTimeout`: 必须在1秒到10分钟之间
- `MaxRetryAttempts`: 必须在0到10之间
- `RetryDelay`: 必须在100ms到1分钟之间

#### **MCPServerConfig验证**
- `ServerName`: 必需，1-100字符
- `GatewayAdapterName`: 当`UseGateway=true`时必需，最多100字符
- `SessionId`: 可选，最多200字符
- `Priority`: 必须在1到100之间
- 对于直连：`Command`或`Url`二选一必需
