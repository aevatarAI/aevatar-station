# Aevatar.GAgents.AIGAgent 模块详解

## 引言

**Aevatar.GAgents.AIGAgent** 是 Aevatar GAgents 生态系统中的核心实现模块，为开发 AI 代理提供了基础框架和实现。这个模块作为连接抽象层和具体 AI 功能的桥梁，使开发者能够轻松创建具有 AI 能力的代理，而无需深入了解底层的 AI 提供商接口和复杂实现细节。

该模块基于 Aevatar.GAgents.AI.Abstractions 定义的接口，提供了完整的 AI 代理基类实现，支持文本处理、聊天对话、知识管理和图像生成等多种 AI 功能，同时提供了状态管理、事件处理和异步通信机制，使 AI 代理能够在分布式环境中高效运行。

## 核心组件

### IAIGAgent

`IAIGAgent` 是 AI 代理的基础接口，定义了 AI 代理必须提供的核心功能：

```csharp
public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);
    Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList);
}
```

- **InitializeAsync**: 初始化 AI 代理，设置 LLM 配置和系统提示
- **UploadKnowledge**: 上传知识内容到 AI 代理，用于增强 AI 的回答能力

### AIGAgentBase

`AIGAgentBase` 是模块的核心类，提供了 AI 代理的基本实现：

```csharp
public abstract partial class AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
```

这个类被分成多个部分以实现不同的功能：

1. **基本功能** (AIGAgentBase.cs):
   - 初始化 AI 大脑
   - 管理 LLM 配置
   - 处理聊天对话
   - 管理状态转换

2. **流式处理** (AIGAgentBase.Stream.cs):
   - 支持流式聊天响应
   - 处理长时间运行的异步任务
   - 流式结果的回调处理

3. **图像生成** (AIGAgentBase.TextToImage.cs):
   - 文本到图像的转换功能
   - 图像生成选项配置
   - 异步图像生成处理

4. **HTTP 请求** (AIGAgentBase.HttpAsync.cs):
   - 处理 HTTP 请求
   - 与外部 API 集成

### AIGAgentStateBase

`AIGAgentStateBase` 是 AI 代理状态的基类，用于管理 AI 代理的状态信息：

```csharp
[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(1)] public string? SystemLLM { get; set; } = null;
    [Id(2)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(3)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(4)] public int InputTokenUsage { get; set; } = 0;
    [Id(5)] public int OutTokenUsage { get; set; } = 0;
    [Id(6)] public int TotalTokenUsage { get; set; } = 0;
    [Id(7)] public bool StreamingModeEnabled { get; set; }
    [Id(8)] public StreamingConfig StreamingConfig { get; set; }
    [Id(9)] public int LastInputTokenUsage { get; set; } = 0;
    [Id(10)] public int LastOutTokenUsage { get; set; } = 0;
    [Id(11)] public int LastTotalTokenUsage { get; set; } = 0;
}
```

- 存储 LLM 配置信息
- 跟踪令牌使用情况
- 管理流式处理配置
- 存储提示模板和知识状态

## 功能特性

### 1. AI 聊天能力

模块提供了强大的聊天功能，支持：

- **基础聊天**：使用 `ChatWithHistory` 方法发送提示并获取回复
- **流式聊天**：支持实时流式响应，适用于需要即时反馈的场景
- **历史记录管理**：维护对话历史记录以提供上下文感知的回复
- **令牌使用跟踪**：记录输入、输出和总令牌使用情况

### 2. 知识管理

支持向 AI 代理添加知识库内容：

- **多种内容格式**：支持文本和 PDF 文件
- **知识检索**：在生成回复时可以利用上传的知识
- **增强生成**：实现检索增强生成 (RAG) 功能

### 3. 图像生成

提供文本到图像的生成能力：

- **自定义选项**：控制图像风格、质量和其他参数
- **异步处理**：支持长时间运行的图像生成任务
- **流式回调**：通过事件回调处理生成结果

### 4. 流式响应

为实时交互提供流式处理支持：

- **缓冲配置**：可配置的流式处理缓冲大小
- **超时控制**：自动处理长时间运行的请求
- **错误处理**：优雅处理流式处理中的错误

### 5. 状态管理

使用事件源模式管理 AI 代理状态：

- **状态转换**：通过事件驱动的状态变更
- **事件日志**：记录状态变更事件
- **持久化**：支持状态的持久化和恢复

## 架构设计

### 设计模式

模块采用了多种设计模式：

1. **模板方法模式**：基类定义算法骨架，子类实现具体步骤
2. **工厂模式**：使用 BrainFactory 创建适当的 Brain 实例
3. **事件源模式**：通过事件序列管理和记录状态变更
4. **策略模式**：支持不同的 LLM 提供商和配置策略
5. **观察者模式**：通过事件处理机制实现组件间通信

### 分布式架构

基于 Orleans 虚拟 Actor 模型：

- **Grain 实现**：每个 AI 代理作为独立的 Grain 运行
- **异步通信**：所有操作都是异步的，确保高性能
- **分区容错**：支持在分布式环境中的弹性运行
- **长时间运行任务**：使用专用工作者处理耗时操作

### 与抽象层的关系

模块依赖 Aevatar.GAgents.AI.Abstractions：

- 实现抽象层定义的接口
- 使用抽象层的数据模型
- 通过 BrainFactory 创建具体的 Brain 实现

## 使用指南

### 1. 创建自定义状态类

首先，创建 AI 代理的状态类，继承自 AIGAgentStateBase：

```csharp
[GenerateSerializer]
public class MyAIState : AIGAgentStateBase
{
    [Id(0)] public string CustomField { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, string> ContextData { get; set; } = new();
}
```

### 2. 创建状态日志事件类

创建记录状态变更的事件类：

```csharp
[GenerateSerializer]
public class MyAIStateLogEvent : StateLogEventBase<MyAIStateLogEvent>
{
    // 自定义属性
}
```

### 3. 定义代理接口

创建 AI 代理的接口，继承自 IAIGAgent：

```csharp
public interface IMyAIGAgent : IAIGAgent, IGAgent
{
    Task<string> ChatAsync(string message);
    Task<bool> UploadCustomKnowledgeAsync(string content);
}
```

### 4. 创建事件类

定义代理将处理的事件：

```csharp
[GenerateSerializer]
public class ChatMessageEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
    [Id(1)] public string SessionId { get; set; } = string.Empty;
}
```

### 5. 实现代理类

创建 AI 代理的实现，继承自 AIGAgentBase：

```csharp
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class MyAIGAgent : AIGAgentBase<MyAIState, MyAIStateLogEvent>, IMyAIGAgent
{
    private readonly ILogger<MyAIGAgent> _logger;

    public MyAIGAgent(ILogger<MyAIGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatMessageEvent @event)
    {
        var result = await base.ChatWithHistory(@event.Message);
        if (result != null && result.Any())
        {
            await PublishAsync(new ChatResponseEvent
            {
                Response = result.First().Content,
                SessionId = @event.SessionId
            });
        }
    }

    public async Task<string> ChatAsync(string message)
    {
        var result = await base.ChatWithHistory(message);
        return result?.FirstOrDefault()?.Content ?? string.Empty;
    }

    public async Task<bool> UploadCustomKnowledgeAsync(string content)
    {
        var knowledge = new List<BrainContentDto>
        {
            new BrainContentDto("custom_knowledge", content)
        };
        return await base.UploadKnowledge(knowledge);
    }

    protected override void AIGAgentTransitionState(MyAIState state, StateLogEventBase<MyAIStateLogEvent> @event)
    {
        // 处理自定义事件的状态转换
    }
}
```

### 6. 初始化和使用代理

```csharp
// 获取代理实例
var client = host.Services.GetRequiredService<IClusterClient>();
var myAIGAgent = client.GetGrain<IMyAIGAgent>(Guid.NewGuid());

// 初始化代理
await myAIGAgent.InitializeAsync(new InitializeDto
{
    Instructions = "You are a helpful AI assistant.",
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "AzureOpenAI"
    },
    StreamingModeEnabled = true,
    StreamingConfig = new StreamingConfig
    {
        BufferingSize = 20,
        TimeOutInternal = 30000
    }
});

// 上传知识
await myAIGAgent.UploadCustomKnowledgeAsync("This is some domain-specific knowledge.");

// 发送聊天消息
var response = await myAIGAgent.ChatAsync("Hello, can you help me?");
```

## 示例：流式聊天实现

以下是一个实现流式聊天功能的例子：

```csharp
public class StreamingChatGAgent : AIGAgentBase<MyAIState, MyAIStateLogEvent>, IStreamingChatGAgent
{
    [EventHandler]
    public async Task HandleEventAsync(StreamChatEvent @event)
    {
        var context = new AIChatContextDto
        {
            ChatId = @event.ChatId,
            RequestId = Guid.NewGuid()
        };

        // 使用流式处理方法
        await base.PromptWithStreamAsync(@event.Message, null, null, context);
    }

    // 重写流式回调处理方法
    protected override async Task AIChatHandleStreamAsync(
        AIChatContextDto context, 
        AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        if (errorEnum != AIExceptionEnum.None)
        {
            // 处理错误
            await PublishAsync(new ChatErrorEvent
            {
                ChatId = context.ChatId,
                ErrorMessage = errorMessage ?? "Unknown error"
            });
            return;
        }

        // 发布流式内容
        await PublishAsync(new StreamContentEvent
        {
            ChatId = context.ChatId,
            Content = content?.Content ?? string.Empty,
            IsComplete = content?.IsComplete ?? false
        });
    }
}
```

## 与其他模块的集成

### 与 AI.Abstractions 的集成

Aevatar.GAgents.AIGAgent 依赖 AI.Abstractions 中定义的接口和模型：

- 使用 IBrain、IChatBrain、ITextToImageBrain 等接口
- 使用 BrainFactory 创建具体的 Brain 实现
- 遵循 AI.Abstractions 定义的配置模型

### 与 SemanticKernel 的集成

模块依赖 Aevatar.GAgents.SemanticKernel：

- 使用 SemanticKernel 提供的语义理解功能
- 集成 SemanticKernel 的自定义技能和插件

### 与事件源模式的集成

基于 Aevatar 的事件源框架：

- 使用事件来驱动状态变更
- 通过事件日志记录代理状态历史
- 支持事件重放和状态恢复

## 扩展方式

### 1. 自定义处理逻辑

通过重写基类方法自定义行为：

```csharp
protected override async Task AIChatHandleStreamAsync(
    AIChatContextDto context, 
    AIExceptionEnum errorEnum,
    string? errorMessage,
    AIStreamChatContent? content)
{
    // 自定义流式响应处理逻辑
}

protected override void AIGAgentTransitionState(
    MyAIState state, 
    StateLogEventBase<MyAIStateLogEvent> @event)
{
    // 自定义状态转换逻辑
}
```

### 2. 添加新功能

通过继承基类并添加新方法扩展功能：

```csharp
public class EnhancedAIGAgent : AIGAgentBase<MyAIState, MyAIStateLogEvent>
{
    public async Task<string> SummarizeContentAsync(string content)
    {
        // 实现内容总结功能
        var result = await base.ChatWithHistory($"Summarize the following content: {content}");
        return result?.FirstOrDefault()?.Content ?? string.Empty;
    }
}
```

### 3. 集成外部服务

使用 HttpAsync 方法集成外部 API：

```csharp
public async Task<WeatherInfo> GetWeatherInfoAsync(string location)
{
    var response = await HttpGetAsync($"https://weather-api.example.com/forecast?location={location}");
    // 处理响应
    return JsonSerializer.Deserialize<WeatherInfo>(response);
}
```

## 总结

Aevatar.GAgents.AIGAgent 模块是 Aevatar GAgents 生态系统中的核心实现组件，为开发者提供了构建 AI 代理的完整框架。通过这个模块：

1. **简化开发**：开发者可以专注于业务逻辑而不是底层 AI 集成细节
2. **统一接口**：为不同 AI 提供商提供统一的交互方式
3. **分布式支持**：基于 Orleans 的高性能分布式架构
4. **状态管理**：基于事件源的可靠状态管理
5. **灵活扩展**：易于扩展和定制的设计

该模块适用于构建各种 AI 驱动的应用场景，如智能客服、内容生成、知识管理系统等，为开发者提供了将 AI 能力快速集成到应用中的强大工具。 