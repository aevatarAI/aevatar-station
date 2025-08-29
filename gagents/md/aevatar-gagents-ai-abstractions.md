# Aevatar.GAgents.AI.Abstractions 组件详解

## 引言

**Aevatar.GAgents.AI.Abstractions** 是 Aevatar GAgents 生态系统中的核心抽象层，为构建 AI 代理提供了统一的接口和数据模型。这个组件作为一个桥接层，使不同 AI 提供商（如 OpenAI、Azure、Google 等）的实现能够通过一致的接口进行交互，简化了开发过程并提高了代码的可维护性和可扩展性。

该抽象层专注于定义 AI "大脑"（Brain）的能力和行为，包括文本处理、聊天对话、图像生成等功能，同时提供灵活的配置选项以适应不同场景的需求。

## 核心接口

### IBrain

`IBrain` 是最基础的 AI 大脑接口，定义了所有 AI 大脑实现必须提供的核心功能：

```csharp
public interface IBrain : ITransientDependency
{
    LLMProviderEnum ProviderEnum { get; }
    ModelIdEnum ModelIdEnum { get; }

    Task InitializeAsync(LLMConfig llmConfig, string id, string description);
    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);
}
```

- **ProviderEnum**: 指定 AI 提供商（如 OpenAI、Azure 等）
- **ModelIdEnum**: 指定使用的模型类型
- **InitializeAsync**: 初始化 AI 大脑，设置必要的配置和标识
- **UpsertKnowledgeAsync**: 向 AI 大脑添加或更新知识内容，用于检索增强生成（RAG）等功能

### IChatBrain

`IChatBrain` 扩展了 `IBrain` 接口，添加了处理对话和生成回复的能力：

```csharp
public interface IChatBrain : IBrain
{
    Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList);
}
```

- **InvokePromptAsync**: 处理用户输入并生成回复
- **InvokePromptStreamingAsync**: 以流式方式处理用户输入并生成回复，适用于需要实时响应的场景
- **GetStreamingTokenUsage**: 计算流式处理中的令牌使用情况

### ITextToImageBrain

`ITextToImageBrain` 扩展了 `IBrain` 接口，添加了文本到图像生成的能力：

```csharp
public interface ITextToImageBrain : IBrain
{
    Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option,
        CancellationToken cancellationToken = default);
}
```

- **GenerateTextToImageAsync**: 根据文本提示生成图像

### IBrainFactory

`IBrainFactory` 是一个工厂接口，用于创建不同类型的 Brain 实例：

```csharp
public interface IBrainFactory
{
    IBrain? CreateBrain(LLMProviderConfig llmProviderConfig);
    IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig);
    ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig);
}
```

- **CreateBrain**: 创建基础 Brain 实例
- **GetChatBrain**: 获取支持聊天功能的 Brain 实例
- **GetTextToImageBrain**: 获取支持文本到图像生成的 Brain 实例

## 数据模型

### BrainContent

`BrainContent` 表示存储在 AI 大脑中的内容，可以是文本或其他格式：

```csharp
public class BrainContent
{
    public byte[] Content { get; }
    public BrainContentType Type { get; }
    public string Name { get; } = string.Empty;

    // 构造函数和辅助方法...
}
```

### ChatMessage

`ChatMessage` 表示聊天对话中的单条消息：

```csharp
[GenerateSerializer]
public class ChatMessage
{
    [Id(0)] public ChatRole ChatRole { get; set; }
    [Id(1)] public string? Content { get; set; }
}
```

### InvokePromptResponse

`InvokePromptResponse` 表示调用提示后的响应结果：

```csharp
public class InvokePromptResponse
{
    public List<ChatMessage> ChatReponseList { get; set; }
    public TokenUsageStatistics TokenUsageStatistics { get; set; }
}
```

### TokenUsageStatistics

`TokenUsageStatistics` 用于跟踪令牌使用情况的统计信息，对于监控和计费非常重要。

## 配置选项

组件提供了丰富的配置选项，以适应不同 AI 提供商和使用场景的需求：

### LLMProviderConfig 和 LLMConfig

这些类定义了 LLM（大型语言模型）的配置参数，包括：

```csharp
[GenerateSerializer]
public class LLMConfig : LLMProviderConfig
{
    [Id(0)] public string ModelName { get; set; } = string.Empty;
    [Id(1)] public string Endpoint { get; set; } = string.Empty;
    [Id(2)] public string ApiKey { get; set; } = string.Empty;
    [Id(3)] public Dictionary<string, object>? Memo { get; set; } = null;
    [Id(4)] public int NetworkTimeoutInSeconds { get; set; } = 100;
    
    // 其他方法...
}
```

### 特定提供商配置

为各种 AI 提供商提供了专门的配置类：

- **OpenAIConfig**: OpenAI 特定配置
- **AzureOpenAIEmbeddingsConfig**: Azure OpenAI 嵌入配置
- **GeminiConfig**: Google Gemini 特定配置

### 向量数据库配置

支持多种向量数据库的配置，用于知识检索：

- **QdrantConfig**: Qdrant 向量数据库配置
- **WeaviateConfig**: Weaviate 向量数据库配置
- **RedisConfig**: Redis 配置，可用于缓存或向量存储

### 文本到图像生成配置

`TextToImageOption` 提供了控制图像生成过程的选项，如样式、质量等。

## 集成与扩展

### 与 Orleans 集成

组件与 Microsoft Orleans 分布式框架深度集成，使用 `[GenerateSerializer]` 和 `[Id]` 特性来支持对象序列化和分布式状态管理。这使得 AI 功能可以在分布式环境中高效运行。

### 依赖注入支持

`IBrain` 接口继承自 `ITransientDependency`，这是 ABP 框架中的依赖注入标记接口，表明 Brain 实例应该被注册为瞬态服务。

### 异步操作

所有接口方法都设计为异步操作，确保在处理复杂 AI 请求时不会阻塞应用程序线程，提高系统的响应性和吞吐量。

## 实现示例

### 实现 IBrain

```csharp
public class MyCustomBrain : IBrain
{
    public LLMProviderEnum ProviderEnum => LLMProviderEnum.OpenAI;
    public ModelIdEnum ModelIdEnum => ModelIdEnum.GPT3_5;

    public async Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        // 初始化逻辑
    }

    public async Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        // 更新知识库逻辑
        return true;
    }
}
```

### 实现 IChatBrain

```csharp
public class MyCustomChatBrain : IChatBrain
{
    public LLMProviderEnum ProviderEnum => LLMProviderEnum.OpenAI;
    public ModelIdEnum ModelIdEnum => ModelIdEnum.GPT3_5;

    public async Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        // 初始化逻辑
    }

    public async Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        // 更新知识库逻辑
        return true;
    }

    public async Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        // 处理提示并生成回复
        return new InvokePromptResponse
        {
            ChatReponseList = new List<ChatMessage>
            {
                new ChatMessage { ChatRole = ChatRole.Assistant, Content = "回复内容" }
            },
            TokenUsageStatistics = new TokenUsageStatistics()
        };
    }

    public async Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        // 流式处理逻辑
        return null;
    }

    public TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        // 计算令牌使用情况
        return new TokenUsageStatistics();
    }
}
```

### 实现 IBrainFactory

```csharp
public class MyBrainFactory : IBrainFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MyBrainFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        // 根据配置创建适当的 Brain 实例
        return _serviceProvider.GetRequiredKeyedService<IBrain>(llmProviderConfig.ProviderEnum.ToString());
    }

    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        // 获取聊天 Brain 实例
        return _serviceProvider.GetRequiredKeyedService<IChatBrain>(llmProviderConfig.ProviderEnum.ToString());
    }

    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        // 获取图像生成 Brain 实例
        return _serviceProvider.GetRequiredKeyedService<ITextToImageBrain>(llmProviderConfig.ProviderEnum.ToString());
    }
}
```

## 技术架构特点

1. **接口分离原则**：不同功能（如聊天、图像生成）被分离到各自的接口中，使实现可以专注于特定功能。

2. **工厂模式**：使用 `IBrainFactory` 来创建不同类型的 Brain 实例，隐藏实现细节并提供灵活的创建方式。

3. **依赖注入**：组件设计为在依赖注入容器中使用，简化了服务的注册和解析。

4. **异步设计**：所有操作都是异步的，确保在处理复杂 AI 请求时不会阻塞应用程序线程。

5. **可扩展性**：抽象接口设计使得添加新的 AI 提供商或功能变得简单。

6. **可配置性**：提供了丰富的配置选项，可以根据需要调整 LLM 的行为和性能。

7. **分布式支持**：与 Orleans 集成，支持在分布式环境中运行 AI 功能。

## 总结

Aevatar.GAgents.AI.Abstractions 组件作为 Aevatar GAgents 生态系统中的核心抽象层，为集成不同 AI 提供商和实现多种 AI 功能提供了统一的接口和数据模型。通过接口分离、工厂模式和依赖注入等设计模式，该组件实现了高度的可扩展性和灵活性，使开发者能够轻松地添加新的 AI 提供商或功能。同时，丰富的配置选项和与 Orleans 的集成，使得 AI 功能可以在分布式环境中高效运行，满足不同场景的需求。 