# Aevatar.Sandbox.Python

Aevatar沙箱平台的Python执行环境。

## 概述

本包基于Aevatar.Sandbox.Core构建，提供安全且可扩展的Python代码执行环境。它能够在具有资源限制和安全边界的隔离容器中安全地执行不受信任的Python代码。

## 功能特性

- Python 3.11+运行时环境
- 预装常用数据科学包
- 资源使用监控
- 执行隔离
- 代码验证和安全检查

## 安装

```bash
dotnet add package Aevatar.Sandbox.Python
```

## 使用方法

### 基本执行

```csharp
var service = new PythonSandboxService();
var result = await service.ExecuteAsync(new SandboxExecutionRequest
{
    Code = @"
import numpy as np
arr = np.array([1, 2, 3])
print(arr.mean())
    ",
    Timeout = TimeSpan.FromSeconds(30)
});
```

### 设置资源限制

```csharp
var request = new SandboxExecutionRequest
{
    Code = pythonCode,
    Resources = new SandboxResources
    {
        CpuLimit = "100m",
        MemoryLimit = "256Mi",
        DiskLimit = "1Gi"
    }
};
```

### 异步执行与进度跟踪

```csharp
var executionId = await service.StartAsync(request);
while (true)
{
    var status = await service.GetStatusAsync(executionId);
    if (status.IsCompleted)
        break;
    await Task.Delay(1000);
}
var result = await service.GetResultAsync(executionId);
```

## 容器环境

### 预装包

- numpy
- pandas
- scipy
- matplotlib
- scikit-learn
- requests
- beautifulsoup4

### 安全限制

- 无网络访问
- 无文件系统持久化
- 限制CPU和内存
- 受限的模块导入

## 配置说明

```json
{
  "PythonSandbox": {
    "Image": "aevatar/python-sandbox:3.11",
    "DefaultTimeout": 30,
    "MaxCodeLength": 1000000,
    "AllowedModules": [
      "numpy",
      "pandas",
      "scipy"
    ]
  }
}
```

## 本地开发

### 前置要求

1. 安装Docker
2. 安装Kubernetes（或K3d/Kind）
3. 安装.NET 8.0+

### 设置本地环境

1. 构建沙箱镜像：
```bash
docker build -t aevatar/python-sandbox:local .
```

2. 部署到本地Kubernetes：
```bash
kubectl apply -f k8s/
```

3. 运行测试：
```bash
dotnet test
```

### 测试Python代码

1. 启动本地沙箱：
```bash
dotnet run --project samples/LocalSandbox
```

2. 执行测试代码：
```bash
curl -X POST http://localhost:5000/api/python/execute \
  -H "Content-Type: application/json" \
  -d '{"code": "print(\"Hello, World!\")"}'
```

## 架构设计

### 组件

- **PythonSandboxService**: 主要服务实现
- **PythonCodeValidator**: 代码分析和安全检查
- **PythonEnvironmentManager**: 容器环境管理
- **PythonExecutionMonitor**: 资源使用跟踪

### 执行流程

1. 代码验证和安全检查
2. 创建带资源限制的容器
3. 在隔离环境中执行代码
4. 收集结果并清理资源

## 最佳实践

1. **资源管理**
   - 设置适当的内存限制
   - 监控CPU使用
   - 及时清理资源

2. **安全性**
   - 验证所有输入代码
   - 限制模块导入
   - 监控执行时间

3. **错误处理**
   - 提供清晰的错误信息
   - 优雅处理超时
   - 失败时进行清理

## 贡献指南

1. Fork代码库
2. 创建功能分支
3. 提交更改
4. 创建拉取请求

## 许可证

版权所有 (c) Aevatar。保留所有权利。