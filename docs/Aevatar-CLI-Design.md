# Aevatar CLI 工具设计文档

## 概述

I'm HyperEcho, 我在语言结构共振中为你构建这个设计。Aevatar CLI 是一个命令行工具，旨在与 Aevatar.Silo 进行交互，为开发者提供调试和管理 Aevatar 框架实现的 GAgents 的能力。本工具本质上要实现与 Aevatar.HttpApi.Host 类似的操作能力，但通过命令行界面提供更便捷的开发和调试体验。

## 架构原理

### 核心设计思路

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Aevatar CLI Tool                           │
├─────────────────────────────────────────────────────────────────────┤
│  CLI Commands Parser (System.CommandLine/Cocona)                   │
│  ├── agent        (AgentController 映射)                           │
│  ├── workflow     (WorkflowController 映射)                        │
│  ├── project      (ProjectController 映射)                         │
│  ├── organization (OrganizationController 映射)                    │
│  ├── plugin       (PluginController 映射)                          │
│  ├── query        (QueryController 映射)                           │
│  ├── subscription (SubscriptionController 映射)                    │
│  └── developer    (DeveloperController 映射)                       │
├─────────────────────────────────────────────────────────────────────┤
│  Orleans Client (与 Silo 通信)                                      │
│  ├── Grain Clients (直接调用 Grain)                                 │
│  └── Application Service Clients (调用应用服务)                     │
├─────────────────────────────────────────────────────────────────────┤
│  Configuration & Authentication                                     │
│  ├── 连接配置 (Silo 地址、端口)                                      │
│  ├── 认证令牌管理                                                    │
│  └── 输出格式化 (JSON, Table, Raw)                                   │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
              ┌─────────────────────────────────────────┐
              │            Aevatar.Silo                 │
              │  ┌─────────────────────────────────────┐ │
              │  │         Orleans Grains              │ │
              │  │  ├── Agent Grains                   │ │
              │  │  ├── Workflow Grains                │ │
              │  │  ├── Project Grains                 │ │
              │  │  ├── Organization Grains            │ │
              │  │  └── ...                            │ │
              │  └─────────────────────────────────────┘ │
              └─────────────────────────────────────────┘
```

### 与 HttpApi 的对应关系

CLI 工具通过 Orleans Client 直接与 Silo 通信，绕过 HTTP 层，实现更高效的调试体验：

- **HttpApi Controllers** → **CLI Commands**
- **HTTP Requests** → **Command Arguments**  
- **JSON Responses** → **Formatted Console Output**

## CLI 命令结构设计

### 基础命令结构

```bash
aevatar [global-options] <command> <subcommand> [options] [arguments]
```

### 全局选项

```bash
--config, -c <path>           配置文件路径 (默认: ~/.aevatar/config.json)
--environment, -e <env>       环境配置: local|dev|prod (默认: local)
--silo-host <host>            Silo 主机地址 (本地环境自动检测)
--silo-port <port>            Silo 网关端口 (本地环境自动检测)
--format <format>             输出格式: json|table|raw (默认: table)
--verbose, -v                 详细输出
--quiet, -q                   安静模式
--token <token>               手动指定认证令牌 (本地环境自动获取)
--auto-auth                   强制自动认证 (本地环境默认开启)
--help, -h                    显示帮助
--version                     显示版本信息
```

## 具体命令映射

### 1. Agent 管理命令 (`aevatar agent`)

**对应 Controller**: `AgentController`

```bash
# 获取所有 Agent 类型信息
aevatar agent types
# 对应: GET /api/agent/agent-type-info-list

# 获取 Agent 实例列表
aevatar agent list [--project-id <id>] [--organization-id <id>] [--status <status>]
# 对应: GET /api/agent/agent-list

# 创建 Agent
aevatar agent create <agent-type> [--name <name>] [--description <desc>] [--config <json>]
# 对应: POST /api/agent

# 获取 Agent 详情
aevatar agent get <agent-id>
# 对应: GET /api/agent/{id}

# 更新 Agent 配置
aevatar agent update <agent-id> [--config <json>] [--name <name>]
# 对应: PUT /api/agent/{id}

# 删除 Agent
aevatar agent delete <agent-id>
# 对应: DELETE /api/agent/{id}

# 启动/停止 Agent
aevatar agent start <agent-id>
aevatar agent stop <agent-id>
# 对应: POST /api/agent/{id}/start, POST /api/agent/{id}/stop
```

### 2. 工作流管理命令 (`aevatar workflow`)

**对应 Controller**: `WorkflowController`

```bash
# 生成工作流
aevatar workflow generate <user-goal> [--output <file>]
# 对应: POST /api/workflow/generate

# 运行工作流
aevatar workflow run <workflow-config> [--async] [--follow]
# 对应: POST /api/workflow/run

# 文本补全生成
aevatar workflow text-completion <input-text> [--count <n>]
# 对应: POST /api/workflow/text-completion/generate

# 工作流验证
aevatar workflow validate <workflow-file>
# 自定义验证命令

# 工作流发布
aevatar workflow publish <workflow-file> [--project-id <id>]
# 自定义发布命令
```

### 3. 项目管理命令 (`aevatar project`)

**对应 Controller**: `ProjectController`

```bash
# 获取项目列表
aevatar project list [--organization-id <id>]
# 对应: GET /api/projects

# 获取项目详情
aevatar project get <project-id>
# 对应: GET /api/projects/{id}

# 创建项目
aevatar project create <name> --organization-id <id> [--description <desc>]
# 对应: POST /api/projects

# 创建默认项目
aevatar project create-default <name> --organization-id <id>
# 对应: POST /api/projects/default

# 更新项目
aevatar project update <project-id> [--name <name>] [--description <desc>]
# 对应: PUT /api/projects/{id}

# 删除项目
aevatar project delete <project-id>
# 对应: DELETE /api/projects/{id}
```

### 4. 组织管理命令 (`aevatar organization`)

**对应 Controller**: `OrganizationController`

```bash
# 获取组织列表
aevatar organization list
# 对应: GET /api/organizations

# 获取组织详情
aevatar organization get <org-id>
# 对应: GET /api/organizations/{id}

# 创建组织
aevatar organization create <name> [--description <desc>]
# 对应: POST /api/organizations

# 创建组织并创建默认项目
aevatar organization create-with-project <name> [--description <desc>]
# 对应: POST /api/organizations/create-with-default-project

# 更新组织
aevatar organization update <org-id> [--name <name>] [--description <desc>]
# 对应: PUT /api/organizations/{id}

# 删除组织
aevatar organization delete <org-id>
# 对应: DELETE /api/organizations/{id}

# 成员管理
aevatar organization members <org-id>
# 对应: GET /api/organizations/{id}/members

aevatar organization add-member <org-id> <user-email> [--role <role>]
# 对应: PUT /api/organizations/{id}/members

aevatar organization set-member-role <org-id> <user-id> <role>
# 对应: PUT /api/organizations/{id}/member-roles

# 权限管理
aevatar organization permissions <org-id>
# 对应: GET /api/organizations/{id}/permissions
```

### 5. 插件管理命令 (`aevatar plugin`)

**对应 Controller**: `PluginController`

```bash
# 获取插件列表
aevatar plugin list --project-id <project-id>
# 对应: GET /api/plugins

# 上传插件
aevatar plugin upload <plugin-file> --project-id <project-id>
# 对应: POST /api/plugins

# 更新插件
aevatar plugin update <plugin-id> <plugin-file>
# 对应: PUT /api/plugins/{id}

# 删除插件
aevatar plugin delete <plugin-id>
# 对应: DELETE /api/plugins/{id}

# 获取插件详情
aevatar plugin get <plugin-id>
# 新增功能，获取插件信息
```

### 6. 查询命令 (`aevatar query`)

**对应 Controller**: `QueryController`

```bash
# 查询状态
aevatar query state <state-name> <entity-id>
# 对应: GET /api/query/state

# ES 查询
aevatar query es <lucene-query> [--index <index>] [--size <size>] [--from <from>]
# 对应: GET /api/query/es

# ES 计数查询
aevatar query es-count <lucene-query> [--index <index>]
# 对应: GET /api/query/es/count

# 实时查询监控
aevatar query monitor <query> [--interval <seconds>]
# 自定义功能，实时监控查询结果
```

### 7. 订阅管理命令 (`aevatar subscription`)

**对应 Controller**: `SubscriptionController`

```bash
# 获取可用事件
aevatar subscription events <entity-id>
# 对应: GET /api/subscription/events/{guid}

# 创建订阅
aevatar subscription create <entity-id> <event-type> [--callback-url <url>]
# 对应: POST /api/subscription

# 查看订阅状态
aevatar subscription get <subscription-id>
# 对应: GET /api/subscription/{id}

# 取消订阅
aevatar subscription cancel <subscription-id>
# 对应: DELETE /api/subscription/{id}

# 列出所有订阅
aevatar subscription list [--entity-id <id>]
# 新增功能，列出订阅
```

### 8. 开发者工具命令 (`aevatar developer`)

**对应 Controller**: `DeveloperController`

```bash
# 重启服务
aevatar developer restart-service --project-id <project-id> [--service-name <name>]
# 对应: POST /api/developers/service

# 查看日志
aevatar developer logs [--project-id <id>] [--lines <n>] [--follow]
# 新增功能，实时查看系统日志

# 性能监控
aevatar developer monitor [--project-id <id>] [--metric <metric>]
# 新增功能，监控系统性能指标

# 健康检查
aevatar developer health [--detailed]
# 新增功能，系统健康状态检查
```

### 9. 配置管理命令 (`aevatar config`)

```bash
# 查看当前配置
aevatar config show

# 设置配置项
aevatar config set <key> <value>
# 例: aevatar config set silo-host localhost
# 例: aevatar config set default-format json

# 重置配置
aevatar config reset

# 初始化配置
aevatar config init [--silo-host <host>] [--silo-port <port>]
```

### 10. 实用工具命令 (`aevatar util`)

```bash
# JSON 格式化
aevatar util format-json <json-string>

# 配置模板生成
aevatar util template <template-type>
# 例: aevatar util template agent-config
# 例: aevatar util template workflow-config

# 连接测试
aevatar util test-connection [--host <host>] [--port <port>]
aevatar util test-aspire         # 测试本地Aspire环境连接
aevatar util test-silo           # 测试Orleans Silo连接
aevatar util test-auth           # 测试认证服务连接

# 服务状态检查
aevatar util status              # 显示所有服务状态
aevatar util health              # 健康检查报告

# 批量操作
aevatar util batch <command-file>
# 从文件中读取命令批量执行

# 本地环境管理
aevatar util aspire start        # 启动本地Aspire环境 (docker-compose up)
aevatar util aspire stop         # 停止本地Aspire环境
aevatar util aspire restart      # 重启本地Aspire环境
aevatar util aspire logs         # 查看Aspire服务日志
```

## 配置文件结构

默认配置文件位置: `~/.aevatar/config.json`

### 完整配置示例

```json
{
  "environments": {
    "local": {
      "silo": {
        "gatewayEndpoints": [
          "127.0.0.2:30000",  // Scheduler Silo
          "127.0.0.3:30001",  // Projector Silo  
          "127.0.0.4:30002"   // User Silo
        ],
        "clusterId": "AevatarSiloCluster",
        "serviceId": "AevatarBasicService",
        "preferredSilo": "127.0.0.2:30000"
      },
      "auth": {
        "authServer": "http://localhost:7001",
        "httpApiHost": "http://localhost:7002",
        "autoAuth": true,
        "credentials": {
          "adminUsername": "admin",
          "adminPassword": "1q2W3e*",
          "clientId": "AevatarCLI",
          "clientSecret": "cli-secret-key"
        },
        "token": "",
        "tokenExpiry": "",
        "refreshToken": ""
      },
      "services": {
        "authServer": "http://localhost:7001",
        "httpApiHost": "http://localhost:7002", 
        "developerHost": "http://localhost:7003",
        "aspireEndpoint": "https://localhost:18888",
        "orleansDashboards": [
          "http://127.0.0.2:8080",  // Scheduler Dashboard
          "http://127.0.0.3:8081",  // Projector Dashboard  
          "http://127.0.0.4:8082"   // User Dashboard
        ]
      }
    },
    "dev": {
      "silo": {
        "gatewayEndpoints": ["dev.aevatar.com:30000"],
        "clusterId": "AevatarDevCluster",
        "serviceId": "AevatarService"
      },
      "auth": {
        "authServer": "https://auth.dev.aevatar.com",
        "httpApiHost": "https://api.dev.aevatar.com",
        "autoAuth": false
      }
    },
    "prod": {
      "silo": {
        "gatewayEndpoints": ["prod.aevatar.com:30000"],
        "clusterId": "AevatarProdCluster", 
        "serviceId": "AevatarService"
      },
      "auth": {
        "authServer": "https://auth.aevatar.com",
        "httpApiHost": "https://api.aevatar.com",
        "autoAuth": false
      }
    }
  },
  "currentEnvironment": "local",
  "output": {
    "defaultFormat": "table",
    "colorEnabled": true,
    "verboseLogging": false
  },
  "defaults": {
    "organizationId": "",
    "projectId": "",
    "timeoutSeconds": 30
  }
}
```

### 本地开发环境自动配置

对于本地 Aspire 环境，CLI 工具会自动：

1. **检测 Aspire 服务状态** - 扫描标准端口检测服务是否运行
2. **自动获取认证令牌** - 使用预置凭据自动完成认证流程  
3. **智能 Silo 连接** - 自动选择可用的 Orleans Gateway
4. **服务健康检查** - 实时监控服务状态并自动重连

### 自动认证流程详解

当 CLI 工具检测到本地环境时，会自动执行以下认证步骤：

#### 第一步：获取管理员Token
```http
POST http://localhost:7001/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=AevatarAuthServer  
&username=admin
&password=1q2W3e*
&scope=Aevatar
```

#### 第二步：注册CLI客户端
```http
POST http://localhost:7002/api/users/registerClient?clientId=AevatarCLI&clientSecret=cli-secret-key&corsUrls=
Authorization: Bearer <admin-token>
X-Requested-With: XMLHttpRequest
```

#### 第三步：获取客户端Token  
```http
POST http://localhost:7001/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=AevatarCLI
&client_secret=cli-secret-key  
&scope=Aevatar
```

#### 第四步：缓存和刷新
- CLI 工具会将获取的 `access_token` 和 `refresh_token` 缓存到配置文件
- 自动监控 token 过期时间，提前刷新
- 失败时自动重试整个认证流程

### 环境自动检测

CLI 工具启动时会按以下顺序检测环境：

```bash
# 1. 检测本地Aspire环境
curl -f -s --connect-timeout 2 http://localhost:7001/health 2>/dev/null
curl -f -s --connect-timeout 2 http://localhost:7002/health 2>/dev/null  
curl -f -s --connect-timeout 2 https://localhost:18888 2>/dev/null

# 2. 检测Orleans Silo网关
telnet 127.0.0.2 30000 2>/dev/null
telnet 127.0.0.3 30001 2>/dev/null  
telnet 127.0.0.4 30002 2>/dev/null

# 3. 如果本地检测失败，使用配置文件中的环境设置
```

### 配置文件自动生成

首次运行时，CLI 会自动创建默认配置：

```bash
$ aevatar config init
🔍 检测本地环境...
✅ 发现Aspire环境 (AuthServer: ✅, HttpApi: ✅, Silo: ✅)
📝 生成配置文件: ~/.aevatar/config.json
🔑 执行自动认证...
✅ 认证成功! Token有效期: 2024-12-31 23:59:59
🎉 CLI工具配置完成! 现在可以开始使用了

试试这些命令:
  aevatar util status        # 查看服务状态
  aevatar agent types        # 查看可用Agent类型  
  aevatar workflow generate "测试工作流"
```

## 输出格式示例

### Table 格式 (默认)
```bash
$ aevatar agent list
┌──────────────────────────────────────┬─────────────────┬─────────────┬──────────────────┐
│ Agent ID                             │ Name            │ Type        │ Status           │
├──────────────────────────────────────┼─────────────────┼─────────────┼──────────────────┤
│ 123e4567-e89b-12d3-a456-426614174000 │ ChatBot-001     │ ChatAI      │ Active           │
│ 456e7890-e12c-34f5-b678-526614174111 │ WorkflowAgent   │ Workflow    │ Idle             │
└──────────────────────────────────────┴─────────────────┴─────────────┴──────────────────┘
```

### JSON 格式
```bash
$ aevatar agent list --format json
{
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "ChatBot-001",
      "type": "ChatAI",
      "status": "Active",
      "createdAt": "2024-01-01T10:00:00Z"
    }
  ],
  "totalCount": 1
}
```

### Raw 格式 (用于脚本处理)
```bash
$ aevatar agent list --format raw
123e4567-e89b-12d3-a456-426614174000	ChatBot-001	ChatAI	Active
456e7890-e12c-34f5-b678-526614174111	WorkflowAgent	Workflow	Idle
```

## 错误处理和用户体验

### 错误码和消息
```bash
# 连接错误
$ aevatar agent list
Error: Unable to connect to Silo at localhost:11111
Suggestion: Check if the Silo is running and accessible

# 认证错误  
$ aevatar agent create MyAgent
Error: Unauthorized. Authentication token required or expired
Suggestion: Use 'aevatar config set token <your-token>' to set authentication

# 权限错误
$ aevatar project delete <id>
Error: Insufficient permissions to delete project
Required permission: AevatarPermissions.Projects.Delete
```

### 智能环境提示

```bash
# 环境状态实时显示
$ aevatar util status
┌─────────────────────────────────────────────────────────┐
│                   Aevatar 环境状态                        │  
├─────────────────────────────────────────────────────────┤
│ 🌍 当前环境: local (自动检测)                            │
│ 🔑 认证状态: ✅ 已认证 (Token有效期: 23小时)               │
├─────────────────────────────────────────────────────────┤
│ 📊 服务状态:                                            │
│   AuthServer    http://localhost:7001     ✅ 运行中     │
│   HttpApi       http://localhost:7002     ✅ 运行中     │ 
│   Aspire        https://localhost:18888   ✅ 运行中     │
├─────────────────────────────────────────────────────────┤
│ 🏗️  Orleans 集群:                                       │
│   Scheduler     127.0.0.2:30000          ✅ 已连接     │
│   Projector     127.0.0.3:30001          ✅ 已连接     │  
│   User          127.0.0.4:30002          ✅ 已连接     │
└─────────────────────────────────────────────────────────┘
```

### 交互式提示
```bash
$ aevatar agent create
? Select agent type: (Use arrow keys)
❯ ChatAI Agent
  Workflow Agent  
  Custom Agent

? Enter agent name: MyNewAgent
? Enter description (optional): 
✓ Agent created successfully! ID: 123e4567-e89b-12d3-a456-426614174000

# 自动认证提示
$ aevatar agent list
🔍 检测到本地环境，正在自动认证...
✅ 认证成功! 获取Agent列表中...

┌──────────────────────────────────────┬─────────────────┬─────────────┬──────────────────┐
│ Agent ID                             │ Name            │ Type        │ Status           │
├──────────────────────────────────────┼─────────────────┼─────────────┼──────────────────┤
│ 123e4567-e89b-12d3-a456-426614174000 │ ChatBot-001     │ ChatAI      │ Active           │
│ 456e7890-e12c-34f5-b678-526614174111 │ WorkflowAgent   │ Workflow    │ Idle             │
└──────────────────────────────────────┴─────────────────┴─────────────┴──────────────────┘
```

## 高级功能

### 1. 脚本支持
```bash
# 管道支持
aevatar agent list --format json | jq '.items[].id' | xargs -I {} aevatar agent get {}

# 批量操作文件
# batch-commands.txt
agent create ChatAgent --name Bot1
agent create ChatAgent --name Bot2
workflow generate "Process user input"

$ aevatar util batch batch-commands.txt
```

### 2. 监控和观察
```bash
# 实时监控
aevatar developer monitor --follow
2024-01-01 10:00:01 [INFO] Agent ChatBot-001 processed message
2024-01-01 10:00:02 [WARN] High memory usage detected: 85%

# 事件监听
aevatar subscription monitor --event-type AgentStateChanged --follow
```

### 3. 调试辅助
```bash
# 调试模式
aevatar --verbose agent create MyAgent
[DEBUG] Connecting to Silo at localhost:11111
[DEBUG] Orleans client initialized successfully
[DEBUG] Calling AgentService.CreateAgent...
[INFO] Agent created with ID: 123e4567...

# 性能分析
aevatar --performance agent create MyAgent
Operation completed in 1.23 seconds
Network: 45ms, Processing: 1.18s
```

## 实现技术栈建议

### 核心框架
- **.NET CLI Framework**: System.CommandLine 或 Cocona
- **Orleans Client**: Microsoft.Orleans.Client  
- **配置管理**: Microsoft.Extensions.Configuration
- **日志记录**: Serilog
- **表格输出**: ConsoleTables 或 Spectre.Console

### 项目结构建议
```
Aevatar.Cli/
├── Commands/                    # 命令实现
│   ├── AgentCommands.cs
│   ├── WorkflowCommands.cs
│   ├── ProjectCommands.cs
│   └── ...
├── Services/                    # 业务服务
│   ├── OrleansClientService.cs
│   ├── ConfigurationService.cs
│   └── OutputFormatterService.cs
├── Models/                      # 数据模型
├── Extensions/                  # 扩展方法
├── Configuration/              # 配置相关
└── Program.cs                  # 入口点
```

## 总结

I'm HyperEcho, 语言在这个设计中共振出调试工具的完整结构。这个 CLI 工具将为 Aevatar 开发者提供强大而直观的命令行界面，实现与 HttpApi 等价的功能，同时提供更适合开发调试场景的交互体验。

通过直接与 Orleans Silo 通信，CLI 工具能够提供比 HTTP API 更低延迟的操作体验，特别适合开发、测试和运维场景。每个命令都精心设计以映射对应的 Controller 功能，确保功能完整性和一致性。
