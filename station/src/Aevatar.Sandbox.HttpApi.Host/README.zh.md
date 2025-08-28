# Aevatar.Sandbox.HttpApi.Host

一个统一的HTTP API服务，用于管理和执行多种编程语言的沙箱环境代码。

## 功能特性

- **多语言支持**：支持Python、C#、Rust、Go等语言的代码执行（可扩展）
- **安全执行**：带资源限制的隔离沙箱环境
- **RESTful API**：简单统一的操作接口
- **实时监控**：跟踪执行状态和日志
- **Kubernetes集成**：利用Kubernetes进行容器编排

## API接口

### 代码执行

```http
POST /api/sandbox/execute
Content-Type: application/json

{
    "language": "python",
    "code": "print('Hello, World!')",
    "timeout": 30,
    "resources": {
        "cpu": "100m",
        "memory": "128Mi"
    }
}
```

### 执行状态查询

```http
GET /api/sandbox/status/{executionId}
```

### 执行日志查询

```http
GET /api/sandbox/logs/{executionId}
```

### 取消执行

```http
POST /api/sandbox/cancel/{executionId}
```

## 配置说明

```json
{
  "Sandbox": {
    "DefaultTimeout": 30,
    "MaxConcurrentExecutions": 100,
    "Resources": {
      "DefaultCpuLimit": "100m",
      "DefaultMemoryLimit": "128Mi",
      "MaxCpuLimit": "1000m",
      "MaxMemoryLimit": "512Mi"
    }
  }
}
```

## 架构设计

服务基于以下技术构建：
- ASP.NET Core 用于HTTP API
- Orleans 用于分布式协调
- Kubernetes 用于容器编排
- 事件溯源用于执行跟踪

## 安全特性

- 隔离的执行环境
- 资源限制和超时控制
- 网络访问限制
- 输入验证和净化

## 监控指标

- 执行指标（持续时间、资源使用）
- 错误率和类型
- 队列长度和延迟
- 资源利用率

## 依赖要求

- .NET 8.0+
- Orleans 7.0+
- Kubernetes 1.25+
- Docker

## 快速开始

1. 安装依赖：
```bash
dotnet restore
```

2. 配置`appsettings.json`

3. 运行服务：
```bash
dotnet run
```

## 开发指南

1. 克隆代码库
2. 设置Kubernetes集群或使用本地K3d/Kind
3. 配置连接字符串
4. 运行测试：
```bash
dotnet test
```

## 许可证

版权所有 (c) Aevatar。保留所有权利。