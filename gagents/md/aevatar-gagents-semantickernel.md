# Aevatar.GAgents.SemanticKernel 模块详解

## 引言

**Aevatar.GAgents.SemanticKernel** 模块是 Aevatar GAgents 生态系统中的关键组件，为 Aevatar.GAgents.AI.Abstractions 提供具体实现。该模块基于 Microsoft 的 Semantic Kernel 技术，实现了与多种先进 AI 服务的无缝集成，包括 Azure OpenAI、Azure AI Inference 和 Google Gemini 等。

通过这个模块，开发者可以轻松创建具有语义理解、上下文感知和动态提示生成等高级能力的 AI 代理，而无需深入了解不同 AI 提供商的底层 API 细节。该模块不仅提供了统一的接口，还支持检索增强生成（RAG）、流式处理和文本到图像生成等现代 AI 功能。

## 核心组件

### Brain 实现

#### BrainBase

`BrainBase` 是所有 Brain 实现的基类，定义了通用接口和行为：

```csharp
public abstract class BrainBase : IChatBrain
{
    public abstract LLMProviderEnum ProviderEnum { get; }
    public abstract ModelIdEnum ModelIdEnum { get; }
    
    public async Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        // 初始化大脑
    }
    
    public async Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files)
    {
        // 更新知识库
    }
    
    public async Task<InvokePromptResponse?> InvokePromptAsync(string content, 
        List<ChatMessage>? history, bool ifUseKnowledge = false,
        ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        // 处理提示并生成响应
    }
    
    public async Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, 
        List<ChatMessage>? history, bool ifUseKnowledge,
        ExecutionPromptSettings? promptSettings, 
        CancellationToken cancellationToken)
    {
        // 处理提示并生成流式响应
    }
    
    // 其他辅助方法
}
```

#### 特定提供商实现

模块提供了多种 AI 服务的具体实现：

- **AzureOpenAIBrain**：Azure OpenAI 服务实现
- **AzureAIInferenceBrain**：Azure AI Inference 服务实现
- **GeminiBrain**：Google Gemini 服务实现
- **DeepSeekBrain**：DeepSeek 模型实现
- **OpenAIBrain**：OpenAI 直接服务实现

每个实现都针对特定 AI 提供商的特性进行了优化，同时保持了统一的接口和行为。

### BrainFactory

`BrainFactory` 是一个工厂类，用于动态创建和返回不同类型的 Brain 实例：

```csharp
public class BrainFactory : IBrainFactory
{
    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        // 创建合适的 Brain 实例
    }
    
    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        // 获取聊天 Brain 实例
    }
    
    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        // 获取文本到图像 Brain 实例
    }
}
```

这个工厂根据配置动态选择和实例化适当的 Brain 实现，简化了开发者使用不同 AI 提供商的过程。

### KernelBuilderFactory

`KernelBuilderFactory` 负责创建和配置 Semantic Kernel 构建器：

```csharp
public sealed class KernelBuilderFactory : IKernelBuilderFactory
{
    public IKernelBuilder GetKernelBuilder(string id)
    {
        // 创建和配置 Kernel 构建器
    }
}
```

该工厂为每个 Brain 实例创建一个配置好的 Semantic Kernel 构建器，处理向量存储、嵌入服务等组件的集成。

### 内容处理

模块提供了从不同来源提取和处理内容的功能：

- **IExtractContent**：内容提取接口
- **ExtractPdf**：从 PDF 文件提取文本
- **ExtractString**：处理字符串内容

这些组件负责将不同格式的内容转换为可由 AI 处理的文本。

### 向量存储

向量存储组件负责管理嵌入式数据：

- **IVectorStore**：向量存储接口
- **QdrantVectorStore**：Qdrant 向量数据库实现

这些组件使 RAG（检索增强生成）功能成为可能，允许 AI 从存储的知识中检索相关信息。

### 嵌入服务

嵌入服务负责将文本转换为向量表示：

- **IEmbedding**：嵌入服务接口
- **AzureOpenAITextEmbedding**：Azure OpenAI 嵌入实现

这些组件为向量存储和检索提供支持，使语义搜索成为可能。

## 功能特性

### 1. AI 聊天对话

模块提供了强大的聊天功能，支持：

- **同步对话**：通过 `InvokePromptAsync` 方法处理聊天请求
- **流式对话**：通过 `InvokePromptStreamingAsync` 提供实时响应
- **历史上下文**：支持对话历史，保持上下文连贯性
- **令牌跟踪**：监控和报告令牌使用情况

### 2. 检索增强生成 (RAG)

通过集成向量存储和嵌入服务，实现了 RAG 功能：

- **知识存储**：将内容转换为向量并存储
- **语义检索**：根据用户查询检索相关信息
- **增强生成**：将检索到的信息融入生成过程
- **多格式支持**：处理文本和 PDF 等不同格式

RAG 功能极大提升了 AI 回答的准确性和相关性，特别是在特定领域知识方面。

### 3. 文本到图像生成

模块支持将文本描述转换为图像：

- **AzureOpenAITextToImage**：使用 Azure OpenAI 的图像生成能力
- **自定义选项**：控制图像风格、质量和其他参数
- **异步处理**：支持长时间运行的图像生成任务

### 4. 多提供商支持

模块支持多种 AI 服务提供商：

- **Azure OpenAI**：微软的 OpenAI 服务
- **Azure AI Inference**：微软的 AI 推理服务
- **Google Gemini**：Google 的大型语言模型
- **DeepSeek**：另一种语言模型实现
- **OpenAI**：直接的 OpenAI 服务

这种多提供商支持使开发者可以选择最适合其需求和预算的 AI 服务。

## 架构设计

### 技术堆栈

- **Microsoft.SemanticKernel**：核心 AI 功能
- **Microsoft.SemanticKernel.Connectors.Qdrant**：向量数据库集成
- **Microsoft.SemanticKernel.PromptTemplates.Handlebars**：提示模板
- **Microsoft.SemanticKernel.Connectors.AzureAIInference**：Azure AI 集成
- **Microsoft.SemanticKernel.Connectors.Google**：Google AI 集成
- **PdfPig**：PDF 文档处理

### 设计模式

模块采用了多种设计模式：

1. **工厂模式**：使用 BrainFactory 和 KernelBuilderFactory 创建和配置组件
2. **策略模式**：通过不同 Brain 实现支持不同 AI 服务提供商
3. **模板方法模式**：BrainBase 定义通用算法，子类实现特定步骤
4. **依赖注入**：使用服务容器管理组件依赖

### 扩展性设计

模块设计注重扩展性：

- **接口分离**：清晰的接口定义使添加新实现变得简单
- **抽象基类**：提供通用功能的同时允许特定定制
- **配置驱动**：通过配置而非硬编码控制行为

## 集成与配置

### 服务注册

在启动代码中注册服务：

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AzureOpenAIConfig>(context.Configuration.GetSection("AIServices:AzureOpenAI"));
        services.Configure<AzureAIInferenceConfig>(context.Configuration.GetSection("AIServices:AzureAIInference"));
        services.Configure<AzureOpenAIEmbeddingsConfig>(context.Configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
        services.Configure<GeminiConfig>(context.Configuration.GetSection("AIServices:Gemini"));
        services.Configure<QdrantConfig>(context.Configuration.GetSection("VectorStores:Qdrant"));
        services.Configure<RagConfig>(context.Configuration.GetSection("Rag"));
        
        services.AddSemanticKernel()
            .AddAzureOpenAI()
            .AddQdrantVectorStore()
            .AddAzureOpenAITextEmbedding()
            .AddAzureAIInference()
            .AddGemini();
    });
```

### 配置选项

在 appsettings.json 中配置 AI 服务和向量存储：

```json
{
  "AIServices": {
    "AzureOpenAI": {
      "Endpoint": "https://your-endpoint.openai.azure.com/",
      "ChatDeploymentName": "your-deployment",
      "ApiKey": "your-api-key"
    },
    "AzureAIInference": {
      "Endpoint": "https://your-endpoint.inference.azure.com/",
      "ChatDeploymentName": "your-deployment",
      "ApiKey": "your-api-key"
    },
    "AzureOpenAIEmbeddings": {
      "Endpoint": "https://your-endpoint.openai.azure.com/",
      "DeploymentName": "your-embedding-deployment",
      "ApiKey": "your-api-key"
    },
    "Gemini": {
      "ModelId": "gemini-pro",
      "ApiKey": "your-api-key"
    }
  },
  "VectorStores": {
    "Qdrant": {
      "Host": "localhost",
      "Port": 6334,
      "Https": false,
      "ApiKey": ""
    }
  },
  "Rag": {
    "AIEmbeddingService": "AzureOpenAIEmbeddings",
    "VectorStoreType": "Qdrant",
    "DataLoadingBatchSize": 10,
    "DataLoadingBetweenBatchDelayInMilliseconds": 1000,
    "MaxChunkCount": 500
  }
}
```

## 使用示例

### 在代理中使用 Brain

以下示例演示了如何在 GAgent 中使用 Brain 进行交互：

```csharp
public class AIGAgent : GAgentBase<AIState, AIStateLogEvent>, IAIGAgent
{
    private readonly IBrainFactory _brainFactory;
   
    public AIGAgent()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }
   
    public async Task<List<ChatMessage>?> ChatAsync(string LLM, string prompt, List<ChatMessage>? history = null)
    {
        var brain = _brainFactory.GetChatBrain(new LLMProviderConfig 
        { 
            ProviderEnum = LLMProviderEnum.Azure, 
            ModelIdEnum = ModelIdEnum.OpenAI 
        });
        
        await brain.InitializeAsync(State.LLM, this.GetGrainId().ToString(), "You are a helpful assistant.");
        
        var invokeResponse = await brain.InvokePromptAsync(prompt, history, State.IfUpsertKnowledge);
        return invokeResponse?.ChatReponseList;
    }
    
    public async Task<bool> UploadKnowledgeAsync(List<BrainContentDto> knowledgeList)
    {
        var brain = _brainFactory.CreateBrain(new LLMProviderConfig 
        { 
            ProviderEnum = LLMProviderEnum.Azure, 
            ModelIdEnum = ModelIdEnum.OpenAI 
        });
        
        await brain.InitializeAsync(State.LLM, this.GetGrainId().ToString(), "Knowledge Processing Agent");
        
        List<BrainContent> contentList = knowledgeList.Select(k => k.ConvertToBrainContent()).ToList();
        return await brain.UpsertKnowledgeAsync(contentList);
    }
}
```

### 使用流式处理

以下示例展示如何使用流式处理功能：

```csharp
public class StreamingChatGAgent : GAgentBase<AIState, AIStateLogEvent>, IStreamingChatGAgent
{
    private readonly IBrainFactory _brainFactory;
   
    public StreamingChatGAgent()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }
   
    public async Task HandleStreamChatAsync(string message, string chatId)
    {
        var brain = _brainFactory.GetChatBrain(new LLMProviderConfig 
        { 
            ProviderEnum = LLMProviderEnum.Azure, 
            ModelIdEnum = ModelIdEnum.OpenAI 
        });
        
        await brain.InitializeAsync(State.LLM, this.GetGrainId().ToString(), "You are a helpful assistant.");
        
        var streaming = brain.InvokePromptStreamingAsync(message, null, State.IfUpsertKnowledge, null, CancellationToken.None);
        
        await foreach (var item in streaming)
        {
            if (item is StreamingChatMessageContent content)
            {
                // 发布流式内容
                await PublishAsync(new StreamContentEvent
                {
                    ChatId = chatId,
                    Content = content.Content,
                    IsComplete = content == null // 检查是否为最后一条消息
                });
            }
        }
    }
}
```

## 与 AI 提供商的集成

### Azure OpenAI 集成

```csharp
protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
{
    var clientOptions = new AzureOpenAIClientOptions()
    {
        NetworkTimeout = TimeSpan.FromSeconds(llmConfig.NetworkTimeoutInSeconds)
    };
    
    var azureOpenAi = new AzureOpenAIClient(
        new Uri(llmConfig.Endpoint),
        new AzureKeyCredential(llmConfig.ApiKey),
        clientOptions
    );

    kernelBuilder.AddAzureOpenAIChatCompletion(
        llmConfig.ModelName,
        azureOpenAi);

    return Task.CompletedTask;
}
```

### Google Gemini 集成

```csharp
protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
{
    kernelBuilder.AddGeminiChatCompletion(
        modelId: "gemini-pro",
        apiKey: llmConfig.ApiKey);
        
    return Task.CompletedTask;
}
```

## 扩展模块

### 添加新的 Brain 实现

创建新的 Brain 实现需要继承 BrainBase 并实现抽象方法：

```csharp
public class CustomBrain : BrainBase
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Custom;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.Custom;
    
    public CustomBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<CustomBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
    }
    
    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        // 配置自定义 Kernel 构建器
    }
    
    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        // 创建自定义提示执行设置
    }
    
    protected override TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList)
    {
        // 计算令牌使用情况
    }
    
    public override TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        // 计算流式处理的令牌使用情况
    }
}
```

### 添加新的向量存储

实现 IVectorStore 接口来添加新的向量存储支持：

```csharp
public class CustomVectorStore : IVectorStore
{
    public Task ConfigureCollection(IKernelBuilder kernelBuilder, string collectionName)
    {
        // 配置自定义向量存储
    }
    
    public void RegisterVectorStoreTextSearch(IKernelBuilder kernelBuilder)
    {
        // 注册向量存储文本搜索
    }
}
```

## 总结

Aevatar.GAgents.SemanticKernel 模块通过将 Microsoft Semantic Kernel 与 Aevatar GAgents 框架集成，提供了一个强大的 AI 功能实现层。该模块具有以下主要优势：

1. **统一接口**：为不同 AI 提供商提供统一的接口，简化开发
2. **现代 AI 功能**：支持聊天、RAG、流式处理和图像生成等现代 AI 功能
3. **多提供商支持**：集成多种 AI 服务，满足不同需求和预算
4. **可扩展性**：易于添加新的 AI 提供商支持和自定义功能
5. **简化配置**：通过配置文件和依赖注入简化设置过程

该模块适用于构建各种 AI 驱动的应用场景，如智能客服、知识管理系统、内容生成工具等，为开发者提供了将先进 AI 能力快速集成到应用中的强大工具。 