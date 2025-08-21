# Aevatar.Sandbox.Core

Aevatar沙箱执行平台的核心抽象和基础实现。

## 概述

本库提供了构建安全、可扩展的代码执行环境所需的基础组件。它定义了支撑Aevatar多语言沙箱执行平台的核心接口和基类。

## 核心组件

### ISandboxService

定义沙箱操作的主要接口：

```csharp
public interface ISandboxService
{
    Task<string> ExecuteAsync(SandboxExecutionRequest request);
    Task<SandboxExecutionStatus> GetStatusAsync(string executionId);
    Task<string> GetLogsAsync(string executionId);
    Task CancelAsync(string executionId);
}
```

### SandboxServiceBase

实现通用沙箱功能的抽象基类：

- 资源管理
- 网络策略
- 安全边界
- Kubernetes集成
- 执行生命周期

### 核心模型

- `SandboxExecutionRequest`
- `SandboxExecutionStatus`
- `SandboxResources`
- `SandboxSecurityPolicy`

## 功能特性

### 资源管理

- CPU限制和请求
- 内存约束
- 执行超时
- 磁盘空间配额

### 安全特性

- 进程隔离
- 网络策略
- 文件系统限制
- 资源边界

### 可扩展性

- 特定语言实现
- 自定义资源策略
- 安全策略扩展
- 执行环境定制

## 使用方法

1. 实现特定语言服务：

```csharp
public class PythonSandboxService : SandboxServiceBase
{
    protected override Task<string> ExecuteInternalAsync(
        SandboxExecutionRequest request)
    {
        // Python特定实现
    }
}
```

2. 配置服务：

```csharp
services.AddSandboxCore(options =>
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.MaxConcurrentExecutions = 100;
});
```

## 集成

### Kubernetes

```csharp
public class KubernetesSandboxService : SandboxServiceBase
{
    private readonly IKubernetesHostManager _k8s;

    protected override async Task<string> ExecuteInternalAsync(
        SandboxExecutionRequest request)
    {
        var job = await _k8s.CreateJobAsync(
            new JobSpecification
            {
                Image = request.Image,
                Command = request.Command,
                Resources = request.Resources
            });

        return await WaitForCompletionAsync(job);
    }
}
```

### Orleans集成

```csharp
public class SandboxExecutionGrain : Grain, ISandboxExecutionGrain
{
    private readonly ISandboxService _sandboxService;

    public async Task<string> ExecuteAsync(
        SandboxExecutionRequest request)
    {
        return await _sandboxService.ExecuteAsync(request);
    }
}
```

## 最佳实践

1. **资源管理**
   - 始终设置适当的限制
   - 监控资源使用
   - 实现优雅降级

2. **安全性**
   - 遵循最小权限原则
   - 验证所有输入
   - 隔离执行环境

3. **错误处理**
   - 提供详细的错误信息
   - 实现正确的清理
   - 优雅处理超时

## 开发指南

### 前置要求

- .NET 8.0+
- Kubernetes 1.25+
- Docker

### 构建

```bash
dotnet restore
dotnet build
```

### 测试

```bash
dotnet test
```

## 许可证

版权所有 (c) Aevatar。保留所有权利。