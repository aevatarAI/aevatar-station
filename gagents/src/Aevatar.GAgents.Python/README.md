# Aevatar.GAgents.Python

[English](#english) | [中文](#chinese)

---

## English

### Overview

**Aevatar.GAgents.Python** is a comprehensive Python execution and verification framework built on the Aevatar GAgent architecture. It provides secure, sandboxed Python script execution capabilities with advanced theory verification, dependency management, and AI-enhanced code generation features.

### Key Features

#### 🚀 **Dual GAgent Architecture**
- **PythonExecutionGAgent**: Handles Python environment management and secure script execution
- **PythonVerificationGAgent**: Manages theory verification, code generation, and test case creation

#### 🔒 **Security & Sandboxing**
- Secure sandboxed execution environment
- Script security validation and dangerous operation detection
- Resource limitation (memory, execution time, network access)
- Virtual environment isolation

#### 🧠 **AI-Enhanced Capabilities**
- AI-powered Python code generation
- Intelligent test case creation
- Theory verification workflow automation
- Integration with Semantic Kernel and LLM providers

#### 📦 **Advanced Dependency Management**
- Automatic dependency extraction from Python scripts
- Smart package installation and management
- Built-in module detection and filtering
- Virtual environment support

#### ⚡ **High Performance**
- Concurrent execution support
- Event-driven architecture with Orleans
- Scalable microservice design
- Real-time execution monitoring

### Architecture Components

```
┌─────────────────────────────┐    ┌──────────────────────────────┐
│   PythonVerificationGAgent  │    │    PythonExecutionGAgent     │
│                             │    │                              │
│ • Theory Verification       │◄──►│ • Environment Management     │
│ • Code Generation           │    │ • Script Execution           │
│ • Test Case Creation        │    │ • Security Validation        │
│ • AI Integration            │    │ • Dependency Management      │
└─────────────────────────────┘    └──────────────────────────────┘
               │                                  │
               └──────────────┬───────────────────┘
                              │
                    ┌─────────▼──────────┐
                    │   Event System     │
                    │                    │
                    │ • Script Execution │
                    │ • Environment Mgmt │
                    │ • Security Events  │
                    │ • Verification     │
                    └────────────────────┘
```

### Quick Start

#### 1. **Basic Python Execution**

```csharp
// Get the execution agent
var executionAgent = await gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
await executionAgent.InitializeAsync();

// Execute Python script
var script = @"
import numpy as np
result = np.array([1, 2, 3]).sum()
print(f'Sum: {result}')
";

var result = await executionAgent.ExecutePythonScriptAsync(script);
Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Output: {result.StandardOutput}");
```

#### 2. **Theory Verification with AI**

```csharp
// Get the verification agent
var verificationAgent = await gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
await verificationAgent.InitializeAsync();

// Initialize with AI capabilities
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
    Instructions = "You are a Python verification expert"
};
await verificationAgent.InitializeAsync(initDto);

// Verify mathematical theory
var theory = "The sum of first n natural numbers is n*(n+1)/2";
var testCases = new List<TestCase>
{
    new TestCase
    {
        TestName = "Sum of first 5 numbers",
        InputParameters = new Dictionary<string, object> { ["n"] = 5 },
        ExpectedResult = 15
    }
};

var verificationResult = await verificationAgent.VerifyTheoryAsync(theory, testCases);
```

#### 3. **Environment Management**

```csharp
// Create virtual environment
var config = new PythonEnvironmentConfig
{
    EnvironmentName = "ml-project",
    Dependencies = new List<string> { "numpy", "pandas", "scikit-learn" },
    MaxExecutionTimeSeconds = 60,
    MaxMemoryMB = 1024,
    IsSandboxed = true
};

await executionAgent.CreateEnvironmentAsync("ml-project", config);

// Execute in specific environment
var result = await executionAgent.ExecutePythonScriptAsync(script, config);
```

### Configuration

#### Environment Configuration

```csharp
public class PythonEnvironmentConfig
{
    public string EnvironmentName { get; set; } = "default";
    public string PythonVersion { get; set; } = "3.9";
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public int MaxExecutionTimeSeconds { get; set; } = 30;
    public long MaxMemoryMB { get; set; } = 512;
    public bool EnableNetworkAccess { get; set; } = false;
    public bool IsSandboxed { get; set; } = true;
    public string WorkingDirectory { get; set; } = string.Empty;
}
```

### Event System

The framework provides rich event-driven communication:

#### PythonExecutionGAgent Events
- `ScriptExecutionCompletedEvent`
- `EnvironmentCreatedEvent`
- `DependenciesInstalledEvent`
- `SecurityValidationFailedEvent`

#### PythonVerificationGAgent Events
- `VerificationCompletedEvent`
- `CodeGeneratedEvent`
- `TestCaseCreatedEvent`

### Security Features

- **Script Validation**: Detects dangerous operations like file access, subprocess execution
- **Sandboxed Execution**: Isolated environment with resource limits
- **Network Restrictions**: Configurable network access control
- **Memory Limits**: Prevents memory exhaustion attacks
- **Timeout Protection**: Prevents infinite loops and hanging scripts

### Testing

Run the test suite:

```bash
dotnet test test/Aevatar.GAgents.Python.Test/
```

Key test categories:
- Environment management tests
- Script execution tests
- Security validation tests
- Dependency extraction tests
- Integration tests

### Contributing

1. Follow the Aevatar GAgent implementation guidelines
2. Ensure all tests pass
3. Add appropriate logging and error handling
4. Use Orleans event sourcing patterns
5. Document public APIs

---

## Chinese

### 概述

**Aevatar.GAgents.Python** 是基于 Aevatar GAgent 架构构建的综合性 Python 执行和验证框架。它提供安全的沙箱化 Python 脚本执行能力，具备先进的理论验证、依赖管理和 AI 增强的代码生成功能。

### 核心特性

#### 🚀 **双 GAgent 架构**
- **PythonExecutionGAgent**: 处理 Python 环境管理和安全脚本执行
- **PythonVerificationGAgent**: 管理理论验证、代码生成和测试用例创建

#### 🔒 **安全与沙箱**
- 安全的沙箱执行环境
- 脚本安全验证和危险操作检测
- 资源限制（内存、执行时间、网络访问）
- 虚拟环境隔离

#### 🧠 **AI 增强能力**
- AI 驱动的 Python 代码生成
- 智能测试用例创建
- 理论验证工作流自动化
- 与 Semantic Kernel 和 LLM 提供商集成

#### 📦 **高级依赖管理**
- 从 Python 脚本自动提取依赖
- 智能包安装和管理
- 内置模块检测和过滤
- 虚拟环境支持

#### ⚡ **高性能**
- 并发执行支持
- 基于 Orleans 的事件驱动架构
- 可扩展的微服务设计
- 实时执行监控

### 架构组件

```
┌─────────────────────────────┐    ┌──────────────────────────────┐
│   PythonVerificationGAgent  │    │    PythonExecutionGAgent     │
│                             │    │                              │
│ • 理论验证                   │◄──►│ • 环境管理                   │
│ • 代码生成                   │    │ • 脚本执行                   │
│ • 测试用例创建               │    │ • 安全验证                   │
│ • AI 集成                   │    │ • 依赖管理                   │
└─────────────────────────────┘    └──────────────────────────────┘
               │                                  │
               └──────────────┬───────────────────┘
                              │
                    ┌─────────▼──────────┐
                    │   事件系统          │
                    │                    │
                    │ • 脚本执行事件      │
                    │ • 环境管理事件      │
                    │ • 安全事件          │
                    │ • 验证事件          │
                    └────────────────────┘
```

### 快速开始

#### 1. **基本 Python 执行**

```csharp
// 获取执行代理
var executionAgent = await gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
await executionAgent.InitializeAsync();

// 执行 Python 脚本
var script = @"
import numpy as np
result = np.array([1, 2, 3]).sum()
print(f'Sum: {result}')
";

var result = await executionAgent.ExecutePythonScriptAsync(script);
Console.WriteLine($"成功: {result.Success}");
Console.WriteLine($"输出: {result.StandardOutput}");
```

#### 2. **AI 理论验证**

```csharp
// 获取验证代理
var verificationAgent = await gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
await verificationAgent.InitializeAsync();

// 初始化 AI 能力
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
    Instructions = "你是一个 Python 验证专家"
};
await verificationAgent.InitializeAsync(initDto);

// 验证数学理论
var theory = "前 n 个自然数的和为 n*(n+1)/2";
var testCases = new List<TestCase>
{
    new TestCase
    {
        TestName = "前5个数字的和",
        InputParameters = new Dictionary<string, object> { ["n"] = 5 },
        ExpectedResult = 15
    }
};

var verificationResult = await verificationAgent.VerifyTheoryAsync(theory, testCases);
```

#### 3. **环境管理**

```csharp
// 创建虚拟环境
var config = new PythonEnvironmentConfig
{
    EnvironmentName = "ml-project",
    Dependencies = new List<string> { "numpy", "pandas", "scikit-learn" },
    MaxExecutionTimeSeconds = 60,
    MaxMemoryMB = 1024,
    IsSandboxed = true
};

await executionAgent.CreateEnvironmentAsync("ml-project", config);

// 在特定环境中执行
var result = await executionAgent.ExecutePythonScriptAsync(script, config);
```

### 配置

#### 环境配置

```csharp
public class PythonEnvironmentConfig
{
    public string EnvironmentName { get; set; } = "default";
    public string PythonVersion { get; set; } = "3.9";
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public int MaxExecutionTimeSeconds { get; set; } = 30;
    public long MaxMemoryMB { get; set; } = 512;
    public bool EnableNetworkAccess { get; set; } = false;
    public bool IsSandboxed { get; set; } = true;
    public string WorkingDirectory { get; set; } = string.Empty;
}
```

### 事件系统

框架提供丰富的事件驱动通信：

#### PythonExecutionGAgent 事件
- `ScriptExecutionCompletedEvent` - 脚本执行完成事件
- `EnvironmentCreatedEvent` - 环境创建事件
- `DependenciesInstalledEvent` - 依赖安装事件
- `SecurityValidationFailedEvent` - 安全验证失败事件

#### PythonVerificationGAgent 事件
- `VerificationCompletedEvent` - 验证完成事件
- `CodeGeneratedEvent` - 代码生成事件
- `TestCaseCreatedEvent` - 测试用例创建事件

### 安全特性

- **脚本验证**: 检测危险操作如文件访问、子进程执行
- **沙箱执行**: 具有资源限制的隔离环境
- **网络限制**: 可配置的网络访问控制
- **内存限制**: 防止内存耗尽攻击
- **超时保护**: 防止无限循环和挂起脚本

### 测试

运行测试套件：

```bash
dotnet test test/Aevatar.GAgents.Python.Test/
```

主要测试类别：
- 环境管理测试
- 脚本执行测试
- 安全验证测试
- 依赖提取测试
- 集成测试

### 贡献指南

1. 遵循 Aevatar GAgent 实现指南
2. 确保所有测试通过
3. 添加适当的日志记录和错误处理
4. 使用 Orleans 事件溯源模式
5. 为公共 API 编写文档

### 许可证

本项目遵循 Aevatar 项目的许可证条款。

### 支持

如有问题或建议，请通过以下方式联系：
- 创建 GitHub Issue
- 查阅项目文档
- 参与社区讨论

---

**Aevatar.GAgents.Python** - 让 Python 执行更安全、更智能、更强大！ 🐍✨ 