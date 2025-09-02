# MCP 服务器白名单配置指南

## 概述

此指南介绍如何在 Aevatar MCPGAgent 项目中配置和管理 MCP 服务器白名单。新的统一配置系统支持从 `appsettings.json` 读取配置，同时保持对 `DefaultMCPServer` 枚举的向后兼容性。

## 架构说明

### 双重配置源

1. **DefaultMCPServers.cs** - 核心服务器的硬编码配置（保留用于常用服务器）
2. **appsettings.json** - 可扩展的配置文件（支持 200+ 服务器）

### 统一管理

所有配置通过 `MCPServerRegistry` 统一管理，自动加载两种配置源，并在应用启动时初始化白名单。

## 配置方法

### 1. appsettings.json 配置

```json
{
  "MCPServerOptions": {
    "EnableAllMCPServers": false,
    "ResetWhitelistEverytime": true,
    "MCPServers": {
      "filesystem": {
        "ServerName": "filesystem",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp", "/Users"],
        "Description": "File system operations",
        "Type": "Stdio"
      },
      "memory": {
        "ServerName": "memory",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-memory"],
        "Description": "Knowledge graph memory system",
        "Type": "Stdio"
      },
      "github": {
        "ServerName": "github",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-github"],
        "Description": "GitHub integration",
        "Type": "Stdio",
        "Env": {
          "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}"
        }
      },
      "custom-api-server": {
        "ServerName": "custom-api-server",
        "Url": "https://api.example.com/mcp",
        "Description": "Custom API server",
        "Type": "StreamableHttp",
        "Env": {
          "API_KEY": "${CUSTOM_API_KEY}"
        }
      }
    }
  }
}
```

### 2. 配置选项说明

#### MCPServerOptions 属性

- **EnableAllMCPServers**: `bool` - 是否允许所有 MCP 服务器（绕过白名单检查）
- **ResetWhitelistEverytime**: `bool` - 是否在每次启动时重置白名单
- **MCPServers**: `Dictionary<string, MCPServerConfig>` - 服务器配置字典

#### MCPServerConfig 属性

- **ServerName**: `string` - 服务器唯一名称
- **Command**: `string` - 启动命令（仅用于 Stdio 传输）
- **Args**: `List<string>` - 命令参数（仅用于 Stdio 传输）
- **Url**: `string` - 服务器 URL（仅用于 StreamableHttp 传输）
- **Headers**: `Dictionary<string, string>` - HTTP 头部（仅用于 StreamableHttp 传输）
- **Description**: `string` - 服务器描述
- **Type**: `MCPServerType` - 传输类型（`Stdio` 或 `StreamableHttp`）
- **Env**: `Dictionary<string, string>` - 环境变量（主要用于 Stdio 传输）

#### 传输类型说明

**Stdio 传输**：
- 通过标准输入/输出与本地进程通信
- 需要配置：`Command`、`Args`、`Env`（可选）
- 适用于：本地MCP服务器、npm包、可执行文件

**StreamableHttp 传输**：
- 通过 Server-Sent Events (SSE) 与远程HTTP服务器通信
- 需要配置：`Url`、`Headers`（可选）
- 适用于：云服务API、实时数据流、企业内部服务

### 3. 传输类型配置示例

#### Stdio 传输示例

```json
{
  "filesystem": {
    "ServerName": "filesystem",
    "Command": "npx",
    "Args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
    "Description": "File system operations",
    "Type": "Stdio"
  },
  "github": {
    "ServerName": "github",
    "Command": "npx",
    "Args": ["-y", "@modelcontextprotocol/server-github"],
    "Description": "GitHub integration",
    "Type": "Stdio",
    "Env": {
      "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}"
    }
  }
}
```

#### StreamableHttp 传输示例

```json
{
  "company-api": {
    "ServerName": "company-api",
    "Url": "https://api.company.com/mcp/stream",
    "Description": "Company internal API via SSE",
    "Type": "StreamableHttp",
    "Headers": {
      "Authorization": "Bearer ${COMPANY_API_TOKEN}",
      "Accept": "text/event-stream",
      "X-Client-Version": "1.0"
    }
  },
  "realtime-data": {
    "ServerName": "realtime-data",
    "Url": "https://data.example.com/mcp/events",
    "Description": "Real-time data streaming",
    "Type": "StreamableHttp",
    "Headers": {
      "X-API-Key": "${DATA_API_KEY}",
      "Content-Type": "application/json"
    }
  }
}
```

### 4. 环境变量配置

支持在配置中使用环境变量：

```json
{
  "Env": {
    "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}",
    "API_KEY": "${MY_CUSTOM_API_KEY}"
  },
  "Headers": {
    "Authorization": "Bearer ${MY_ACCESS_TOKEN}",
    "X-API-Key": "${MY_API_KEY}"
  }
}
```

## 使用方法

### 1. 服务注册

在 `Program.cs` 或 `Startup.cs` 中注册服务：

```csharp
// 完整注册（包含自动白名单初始化）
services.AddMCPServerRegistry();

// 或者仅注册注册表（不包含后台服务）
services.AddMCPServerRegistryOnly();

// 配置选项
services.Configure<MCPServerOptions>(configuration.GetSection("MCPServerOptions"));
```

### 2. 创建 MCPGAgent

#### 使用 DefaultMCPServer 枚举（推荐用于核心服务器）

```csharp
// 无需环境变量的服务器
var filesystemAgent = await gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.Filesystem);

// 需要环境变量的服务器
var githubAgent = await gAgentFactory.GetMCPGAgentAsync(
    DefaultMCPServer.GitHub,
    env: new Dictionary<string, string>
    {
        ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "your_token_here"
    });

// 使用自定义参数
var customFilesystemAgent = await gAgentFactory.GetMCPGAgentAsync(
    DefaultMCPServer.Filesystem,
    customArgs: new[] { "-y", "@modelcontextprotocol/server-filesystem", "/custom/path" });
```

#### 使用服务器名称（支持配置文件中的所有服务器）

```csharp
// Stdio 服务器
var memoryAgent = await gAgentFactory.GetMCPGAgentAsync("memory");

// StreamableHttp 服务器
var companyApiAgent = await gAgentFactory.GetMCPGAgentAsync("company-api");

// 带额外环境变量（主要用于Stdio）
var customStdioAgent = await gAgentFactory.GetMCPGAgentAsync(
    "github",
    env: new Dictionary<string, string>
    {
        ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "your_token"
    });

// 带自定义参数（仅用于Stdio）
var customizedAgent = await gAgentFactory.GetMCPGAgentAsync(
    "memory",
    customArgs: new[] { "custom", "args" });
```

#### 便利方法

```csharp
// Stdio 服务器便利方法
var fsAgent = await gAgentFactory.GetFilesystemMCPGAgentAsync("/tmp", "/Users");

// StreamableHttp 服务器便利方法
// 基本 SSE 服务器
var sseAgent = await gAgentFactory.GetStreamableHttpMCPGAgentAsync(
    "my-sse-server",
    "https://api.example.com/mcp/stream");

// 带 Bearer 认证的 SSE 服务器
var authSseAgent = await gAgentFactory.GetStreamableHttpMCPGAgentWithAuthAsync(
    "auth-sse-server",
    "https://secure-api.example.com/mcp/events",
    "your-bearer-token");

// 带 API Key 认证的 SSE 服务器
var apiKeySseAgent = await gAgentFactory.GetStreamableHttpMCPGAgentWithApiKeyAsync(
    "api-key-server",
    "https://api.third-party.com/mcp/stream",
    "your-api-key",
    "X-API-Key");

// 检查服务器是否已注册
var isRegistered = gAgentFactory.IsMCPServerRegistered("memory");

// 获取所有已注册服务器
var serverNames = gAgentFactory.GetRegisteredMCPServerNames();

// 获取服务器配置
var stdioConfig = gAgentFactory.GetMCPServerConfig("memory");
var sseConfig = gAgentFactory.GetMCPServerConfig("company-api");
```

### 3. 运行时管理

```csharp
// 获取注册表服务
var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();

// 动态注册新服务器
registry.RegisterServer("new-server", new MCPServerConfig
{
    ServerName = "new-server",
    Command = "npx",
    Args = new List<string> { "-y", "@company/custom-mcp-server" },
    Description = "Custom company server",
    Type = MCPServerType.Stdio
});

// 检查服务器注册状态
var isRegistered = registry.IsServerRegistered("new-server");

// 获取所有配置
var allConfigs = registry.GetAllServerConfigs();

// 移除服务器
registry.UnregisterServer("old-server");
```

## 最佳实践

### 1. 配置组织

建议按功能分组组织服务器配置：

```json
{
  "MCPServerOptions": {
    "MCPServers": {
      // 核心工具
      "filesystem": { ... },
      "memory": { ... },
      
      // 开发工具
      "github": { ... },
      "gitlab": { ... },
      
      // 数据库
      "postgres": { ... },
      "mongodb": { ... },
      
      // 自定义服务
      "company-api": { ... },
      "internal-tool": { ... }
    }
  }
}
```

### 2. 环境配置分离

不同环境使用不同的配置文件：

- `appsettings.json` - 默认配置
- `appsettings.Development.json` - 开发环境
- `appsettings.Production.json` - 生产环境
- `appsettings.secrets.json` - 敏感信息（不提交到版本控制）

### 3. 安全考虑

- 敏感信息使用环境变量
- 使用 User Secrets 在开发环境管理密钥
- 生产环境使用安全的密钥管理服务

```bash
# 设置 User Secrets
dotnet user-secrets set "GITHUB_PERSONAL_ACCESS_TOKEN" "your_token_here"
```

### 4. 配置验证

系统会自动验证配置，建议在添加新服务器时检查日志：

```csharp
// 手动验证配置
var validation = config.Validate();
if (!validation.IsValid)
{
    Console.WriteLine($"Configuration errors: {validation.GetErrorSummary()}");
}
```

## 扩展场景

### 1. 大规模服务器管理（200+ 服务器）

```json
{
  "MCPServerOptions": {
    "MCPServers": {
      // 可以配置数百个服务器
      "server-001": { ... },
      "server-002": { ... },
      // ... 继续添加
      "server-200": { ... }
    }
  }
}
```

### 2. 动态配置加载

```csharp
// 从外部源加载配置
var externalConfigs = await LoadConfigsFromDatabase();
registry.RegisterServers(externalConfigs);

// 更新白名单
var configManager = await gAgentFactory.GetMCPServerConfigGAgent();
await configManager.ConfigMCPWhitelistAsync(registry.GetAllServerConfigs());
```

### 3. 服务器组和标签

通过命名约定或描述实现分组：

```json
{
  "database-postgres-prod": {
    "ServerName": "database-postgres-prod",
    "Description": "Production PostgreSQL server [group:database] [env:production]",
    ...
  }
}
```

## 故障排除

### 常见问题

1. **服务器未注册**
   - 检查 `appsettings.json` 配置
   - 确认服务注册是否正确
   - 查看启动日志

2. **环境变量未设置**
   - 检查环境变量是否正确设置
   - 验证配置中的环境变量名称

3. **白名单验证失败**
   - 确认 `EnableAllMCPServers` 设置
   - 检查服务器是否在白名单中

### 调试技巧

```csharp
// 查看所有已注册服务器
var serverNames = gAgentFactory.GetRegisteredMCPServerNames();
foreach (var name in serverNames)
{
    Console.WriteLine($"Registered server: {name}");
}

// 查看配置（不包含敏感信息）
var config = gAgentFactory.GetMCPServerConfig("github");
var sanitized = config?.WithoutSensitiveInfo();
Console.WriteLine($"GitHub config: {JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = true })}");
```

## 迁移指南

### 从硬编码到配置文件

1. 将现有的 `DefaultMCPServers.Configs` 中的配置复制到 `appsettings.json`
2. 逐步将代码中的硬编码引用改为配置引用
3. 测试确保所有功能正常工作
4. 可选择性地移除不再需要的硬编码配置

### 向后兼容性

现有代码无需修改即可继续工作：

```csharp
// 这些调用仍然有效
var agent1 = await gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.Memory);
var agent2 = await gAgentFactory.GetFilesystemMCPGAgent("/tmp");
```

通过这套配置系统，你可以轻松管理 200+ MCP 服务器，同时保持代码的简洁性和可维护性。
