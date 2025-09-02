# AIGAgentBase 参考文档

## 概述

`AIGAgentBase` 是在 Aevatar 框架中创建 AI 驱动的 GAgent（Grain Agent）的基础抽象类。它通过 Semantic Kernel 提供的 AI 能力扩展了核心 `GAgentBase` 功能，使智能代理能够处理自然语言、使用工具并维护对话状态。

## 架构

### 类层次结构

```
GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    └── AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
```

该类使用泛型类型参数：
- `TState`：状态类型，必须继承自 `AIGAgentStateBase`
- `TStateLogEvent`：状态转换的事件日志类型，必须继承自 `StateLogEventBase<TStateLogEvent>`
- `TEvent`：代理的事件类型，必须继承自 `EventBase`
- `TConfiguration`：配置类型，必须继承自 `ConfigurationBase`

### 核心组件

1. **大脑系统（Brain System）**：由 `IBrain` 接口驱动，管理 AI 模型和 Semantic Kernel 集成
2. **工具系统（Tool System）**：支持 GAgent 工具和 MCP（模型上下文协议）工具
3. **状态管理（State Management）**：基于事件溯源的状态管理，用于持久化和恢复
4. **流式支持（Streaming Support）**：实时流式响应，提供更好的用户体验

## 关键功能

### 1. AI 模型集成

AIGAgentBase 通过 `LLMConfig` 系统支持多个 LLM 提供商：

```csharp
public class LLMConfig
{
    public LLMProviderEnum ProviderEnum { get; set; }
    public ModelIdEnum ModelIdEnum { get; set; }
    public string ModelName { get; set; }
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
    public Dictionary<string, object>? Memo { get; set; }
}
```

配置可以是：
- **集中式**：通过键引用系统范围的 LLM 配置
- **自提供**：直接指定 LLM 配置

### 2. 工具系统

#### GAgent 工具

GAgent 工具允许 AI 代理调用系统中的其他 GAgent，实现复杂的多代理交互。

**关键组件：**
- `IGAgentService`：发现可用的 GAgent 及其事件处理程序
- `IGAgentExecutor`：执行 GAgent 事件处理程序
- `GAgentToolPlugin`：提供实用函数的基础插件

**注册流程：**
1. 发现：`IGAgentService.GetAllAvailableGAgentInformation()` 查找所有 GAgent
2. 函数创建：每个 GAgent 事件成为一个 Semantic Kernel 函数
3. 参数映射：事件属性映射到函数参数
4. 执行跟踪：所有工具调用都会被跟踪，包括时间和结果

**GAgent 工具使用示例：**
```csharp
// 初始化期间
var initDto = new InitializeDto
{
    EnableGAgentTools = true,
    AllowedGAgentTypes = new List<string> { "Calculator", "DataProcessor" },
    // ... 其他配置
};

// AI 然后可以自然地调用这些 GAgent：
// "请使用 Calculator 计算 5 和 10 的和"
```

#### MCP 工具

MCP（模型上下文协议）工具通过标准化协议实现与外部服务和工具的集成。

**关键组件：**
- `IMCPGAgent`：MCP 服务器代理接口
- `MCPToolInfo`：包含参数的工具元数据
- `MCPParameterInfo`：带有类型和要求的参数定义

**MCP 工具结构：**
```csharp
public class MCPToolInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, MCPParameterInfo> Parameters { get; set; }
    public string ServerName { get; set; }
}

public class MCPParameterInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
}
```

**配置：**
```csharp
var initDto = new InitializeDto
{
    EnableMCPTools = true,
    MCPServers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Arguments = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
        }
    }
};
```

### 3. 状态管理

`AIGAgentStateBase` 维护以下状态：

```csharp
public abstract class AIGAgentStateBase : StateBase
{
    // LLM 配置
    public LLMConfig? LLM { get; set; }
    public string? SystemLLM { get; set; }
    public string? LLMConfigKey { get; set; }
    
    // AI 设置
    public string PromptTemplate { get; set; }
    public bool IfUpsertKnowledge { get; set; }
    
    // Token 使用跟踪
    public int InputTokenUsage { get; set; }
    public int OutTokenUsage { get; set; }
    public int TotalTokenUsage { get; set; }
    
    // 流式配置
    public bool StreamingModeEnabled { get; set; }
    public StreamingConfig StreamingConfig { get; set; }
    
    // 工具配置
    public bool EnableGAgentTools { get; set; }
    public List<string> RegisteredGAgentFunctions { get; set; }
    public List<string>? AllowedGAgentTypes { get; set; }
    public bool EnableMCPTools { get; set; }
    public List<string> RegisteredMCPFunctions { get; set; }
    
    // 工具状态
    public Dictionary<string, MCPGAgentReference> MCPAgents { get; set; }
    public List<GrainType> SelectedGAgents { get; set; }
}
```

### 4. 工具调用跟踪

AIGAgentBase 提供所有工具调用的全面跟踪：

```csharp
public class ToolCallDetail
{
    public string ToolName { get; set; }
    public string ServerName { get; set; }
    public Dictionary<string, object> Arguments { get; set; }
    public string Timestamp { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string Result { get; set; }
}
```

## 初始化流程

1. **基本初始化**
   ```csharp
   await agent.InitializeAsync(new InitializeDto
   {
       Instructions = "你是一个有帮助的助手",
       LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
       StreamingModeEnabled = true,
       EnableGAgentTools = true,
       EnableMCPTools = true
   });
   ```

2. **大脑初始化**
   - 通过 IBrainFactory 创建 IBrain 实例
   - 初始化 Semantic Kernel
   - 设置系统提示词

3. **工具注册**
   - 如果 `EnableGAgentTools`：发现并注册 GAgent 函数
   - 如果 `EnableMCPTools`：连接到 MCP 服务器并注册工具

4. **状态持久化**
   - 所有配置更改通过事件溯源持久化
   - 状态可以在 grain 重新激活时恢复

## 关键方法

### 初始化
- `InitializeAsync(InitializeDto)`：主要初始化方法
- `UploadKnowledge(List<BrainContentDto>)`：上传知识库内容

### 工具管理
- `RegisterGAgentsAsToolsAsync()`：注册所有可用的 GAgent 作为工具
- `ConfigureGAgentToolsAsync(List<GrainType>)`：配置特定的 GAgent 工具
- `ConfigureMCPServersAsync(List<MCPServerConfig>)`：配置 MCP 服务器
- `UpdateKernelWithAllToolsAsync()`：刷新所有注册的工具

### 聊天操作
- `ChatWithHistory(...)`：支持历史记录的主要聊天方法
- `InvokePromptStreamingAsync(...)`：流式聊天响应

### 配置
- `SetLLMConfigKeyAsync(string)`：更新 LLM 配置引用
- `SetSystemLLMAsync(string)`：设置系统 LLM
- `SetLLMAsync(LLMConfig, string?)`：设置自定义 LLM 配置

## 事件系统

AIGAgentBase 使用事件溯源进行状态管理。关键事件包括：

- `SetLLMStateLogEvent`：LLM 配置更改
- `SetPromptTemplateStateLogEvent`：系统提示词更新
- `SetStreamingConfigStateLogEvent`：流式配置
- `SetEnableGAgentToolsStateLogEvent`：GAgent 工具启用
- `SetEnableMCPToolsStateLogEvent`：MCP 工具启用
- `TokenUsageStateLogEvent`：Token 使用跟踪

## 支持服务

### IGAgentService

提供 GAgent 发现和信息：

```csharp
public interface IGAgentService
{
    Task<Dictionary<GrainType, List<Type>>> GetAllAvailableGAgentInformation();
    Task<GAgentDetailInfo> GetGAgentDetailInfoAsync(GrainType grainType);
    Task<List<GrainType>> FindGAgentsByEventTypeAsync(Type eventType);
}
```

### IGAgentExecutor

执行 GAgent 事件处理程序：

```csharp
public interface IGAgentExecutor
{
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event);
}
```

## 最佳实践

1. **工具选择**：使用 `AllowedGAgentTypes` 限制可以调用的 GAgent
2. **错误处理**：在事件处理程序中实现适当的错误处理
3. **Token 管理**：通过状态跟踪监控 token 使用情况
4. **流式传输**：为长响应启用流式传输以获得更好的用户体验
5. **状态持久化**：对所有状态更改使用事件溯源

## 实现示例

```csharp
public class MyAIAgent : AIGAgentBase<MyAIAgentState, MyStateLogEvent>
{
    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // 自定义激活逻辑
        await base.OnAIGAgentActivateAsync(cancellationToken);
    }
    
    protected override void AIGAgentTransitionState(MyAIAgentState state, StateLogEventBase<MyStateLogEvent> @event)
    {
        // 处理自定义状态转换
    }
}

public class MyAIAgentState : AIGAgentStateBase
{
    // 添加自定义状态属性
}
```

## 性能考虑

1. **工具发现**：GAgent 发现在初始化期间只发生一次
2. **函数缓存**：Kernel 函数创建一次并重复使用
3. **状态更新**：尽可能使用批量事件
4. **流式缓冲**：为流式传输配置适当的缓冲区大小

## 故障排除

1. **工具注册失败**：检查日志中的特定 GAgent/MCP 错误
2. **LLM 连接问题**：验证 API 密钥和端点
3. **状态恢复**：确保所有事件都可正确序列化
4. **工具执行错误**：检查工具参数类型是否与事件属性匹配

## 数据类型详解

### InitializeDto

初始化 DTO，用于配置 AIGAgent：

```csharp
[GenerateSerializer]
public class InitializeDto
{
    [Id(0)] public string Instructions { get; set; }                     // 系统提示词
    [Required][Id(1)] public LLMConfigDto LLMConfig { get; set; }       // LLM 配置
    [Id(2)] public bool StreamingModeEnabled { get; set; }              // 是否启用流式响应
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }        // 流式配置
    [Id(4)] public bool EnableGAgentTools { get; set; } = false;        // 是否启用 GAgent 工具
    [Id(5)] public List<string>? AllowedGAgentTypes { get; set; }       // 允许的 GAgent 类型
    [Id(6)] public bool EnableMCPTools { get; set; } = false;           // 是否启用 MCP 工具
    [Id(7)] public List<MCPServerConfig>? MCPServers { get; set; }      // MCP 服务器配置
    [Id(8)] public List<GrainType>? SelectedGAgents { get; set; }       // 选定的 GAgent
}
```

### LLMConfigDto

LLM 配置 DTO：

```csharp
[GenerateSerializer]
public class LLMConfigDto
{
    [Id(0)] public string? SystemLLM { get; set; }               // 系统 LLM 引用键
    [Id(1)] public SelfLLMConfig? SelfLLMConfig { get; set; }   // 自定义 LLM 配置
}
```

### StreamingConfig

流式配置：

```csharp
public class StreamingConfig
{
    public int BufferingSize { get; set; }      // 缓冲区大小
    public int TimeOutInternal { get; set; }    // 超时时间（毫秒）
}
```

### GAgentDetailInfo

GAgent 详细信息：

```csharp
public class GAgentDetailInfo
{
    public string Name { get; set; }         // GAgent 名称
    public string Description { get; set; }  // GAgent 描述
    public List<string> Tags { get; set; }   // 标签列表
}
```

## 高级功能

### 知识库集成

AIGAgentBase 支持知识库上传和检索：

```csharp
// 上传知识内容
var knowledge = new List<BrainContentDto>
{
    new BrainContentDto
    {
        Name = "产品手册",
        Content = "产品详细说明...",
        ContentType = "text/plain"
    }
};
await agent.UploadKnowledge(knowledge);
```

### 对话历史管理

支持带历史记录的对话：

```csharp
var history = new List<ChatMessage>
{
    new ChatMessage { Role = ChatRole.User, Content = "你好" },
    new ChatMessage { Role = ChatRole.Assistant, Content = "你好！有什么可以帮助你的吗？" }
};

var response = await agent.ChatWithHistory("今天天气如何？", history);
```

### 动态工具配置

运行时动态配置工具：

```csharp
// 配置特定的 GAgent 工具
var selectedGAgents = new List<GrainType>
{
    GrainType.Create("CalculatorGAgent", "v1"),
    GrainType.Create("WeatherGAgent", "v1")
};
await agent.ConfigureGAgentToolsAsync(selectedGAgents);

// 配置 MCP 服务器
var mcpServers = new List<MCPServerConfig>
{
    new MCPServerConfig
    {
        ServerName = "database",
        Command = "npx",
        Arguments = new List<string> { "@modelcontextprotocol/server-sqlite", "mydb.sqlite" }
    }
};
await agent.ConfigureMCPServersAsync(mcpServers);
```

## 总结

AIGAgentBase 提供了一个强大而灵活的框架，用于创建具有 AI 能力的智能代理。通过其全面的工具系统、状态管理和事件驱动架构，开发者可以构建复杂的多代理系统，实现自然语言交互和自动化任务执行。
