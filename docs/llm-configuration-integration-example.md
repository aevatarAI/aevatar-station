# LLM 配置系统集成示例

本文档展示了如何在实际的 AI Agent 中使用动态 LLM 配置系统，包括配置设置、Agent 集成和端到端使用流程。

## 🎯 核心集成流程

### 1. 通过 REST API 设置配置

#### 设置 Azure OpenAI Provider 配置
```bash
POST /api/llm-config/providers
Content-Type: application/json

{
  "providerType": "Azure",
  "context": {
    "secondaryScope": "project-123"
  },
  "settings": {
    "resourceName": "my-azure-openai",
    "apiVersion": "2024-02-01",
    "region": "eastus"
  }
}
```

#### 设置 GPT-4 Model 配置
```bash
POST /api/llm-config/models
Content-Type: application/json

{
  "modelType": "OpenAI",
  "context": {
    "secondaryScope": "project-123"
  },
  "settings": {
    "modelName": "gpt-4",
    "temperature": 0.7,
    "maxTokens": 4000,
    "azureDeploymentName": "gpt-4-deployment"
  }
}
```

#### 设置 API Key
```bash
POST /api/llm-config/api-keys
Content-Type: application/json

{
  "keyName": "AZURE_OPENAI_API_KEY",
  "keyValue": "sk-xxx...",
  "context": {
    "secondaryScope": "project-123"
  }
}
```

### 2. Agent 集成示例

#### ChatAIGAgent 集成示例

让我们基于实际的 `ChatAIGAgent` 来展示如何集成动态配置系统：

```csharp
using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[Description("Enhanced Chat AI agent with dynamic LLM configuration support")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(EnhancedChatAIGAgent))]
public class EnhancedChatAIGAgent : 
    GroupMemberGAgentBase<ChatAIGAgentState, ChatAIGAgentEvent, EventBase, ChatAIGAgentConfigDto>,
    IChatAIGAgent
{
    private readonly ILogger<EnhancedChatAIGAgent> _logger;

    public EnhancedChatAIGAgent(ILogger<EnhancedChatAIGAgent> logger)
    {
        _logger = logger;
    }

    protected override async Task PerformConfigAsync(ChatAIGAgentConfigDto configuration)
    {
        // Call the base implementation to set MemberName
        await base.PerformConfigAsync(configuration);

        // ✨ 新增：构建业务上下文用于动态配置
        var businessContext = BuildBusinessContext(configuration);

        // ✨ 新增：使用 Provider + Model 分离的配置
        var llmConfig = new LLMConfigDto
        {
            // 优先使用新的分离配置
            ProviderType = configuration.PreferredProvider ?? LLMProviderEnum.OpenAI,
            ModelType = configuration.PreferredModel ?? ModelIdEnum.OpenAI,
            
            // 保持向后兼容
            SystemLLM = configuration.SystemLLM?.ToString()
        };

        // ✨ 新增：使用业务上下文初始化，支持动态配置
        await InitializeAsync(new InitializeDto
        {
            Instructions = configuration.Instructions,
            LLMConfig = llmConfig,
            MCPServers = configuration.MCPServers,
            ToolGAgentTypes = configuration.ToolGAgentTypes,
            ToolGAgents = configuration.ToolGAgents,
        }, businessContext);

        _logger.LogInformation("ChatAIGAgent initialized with dynamic configuration. Context: {@Context}", 
            businessContext);
    }

    /// <summary>
    /// 构建业务上下文，将配置中的业务信息映射到 ConfigurationContext
    /// </summary>
    private ConfigurationContext BuildBusinessContext(ChatAIGAgentConfigDto configuration)
    {
        var context = ConfigurationContext.Default;

        // 根据配置构建分层上下文
        if (!string.IsNullOrEmpty(configuration.ProjectId))
        {
            context = context.WithProject(configuration.ProjectId);
        }

        if (!string.IsNullOrEmpty(configuration.UserId))
        {
            context = context.WithUser(configuration.UserId);
        }

        if (!string.IsNullOrEmpty(configuration.WorkflowId))
        {
            context = context.WithWorkflow(configuration.WorkflowId);
        }

        return context;
    }

    // 保持原有的 ChatAsync 方法不变
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId,
        List<WorkflowChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();

        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
            _logger.LogInformation($"{State.MemberName} generating default AI response based on Instructions");
            
            var promptWithInstructions = $"{State.PromptTemplate ?? ""} Please say something to start the conversation.";
            var chatWithDetails = await ChatWithHistoryAndToolsAsync(promptWithInstructions);
            var defaultResponse = chatWithDetails.Response;

            RaiseEvent(new ChatResponseEvent()
            {
                Response = defaultResponse,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            response.Content = defaultResponse;
            return response;
        }

        var userMessage = string.Join(" ", coordinatorMessages.Select(m => m.Content));
        _logger.LogInformation($"{State.MemberName} processing workflow message: {userMessage}");

        // 这里会自动使用动态配置的 LLM 服务
        var aiMessages = await ChatWithHistoryAndToolsAsync(userMessage);
        var aiResponse = !aiMessages.Response.IsNullOrEmpty()
            ? aiMessages.Response
            : $"{State.MemberName}: I'm having trouble processing your request.";

        RaiseEvent(new ChatResponseEvent()
        {
            Response = aiResponse,
            Timestamp = DateTime.UtcNow
        });
        await ConfirmEvents();

        response.Content = aiResponse;
        return response;
    }

    // 其他方法保持不变...
}
```

#### 扩展 ChatAIGAgentConfigDto

为了支持动态配置，我们需要扩展配置 DTO：

```csharp
[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberGAgentConfigDto
{
    // 现有字段
    [Id(0)] public SystemLLMEnum SystemLLM { get; set; } = SystemLLMEnum.OpenAI;
    [Id(1)] public string Instructions { get; set; } = string.Empty;
    [Id(2)] public List<MCPServerConfig> MCPServers { get; set; } = new();
    [Id(3)] public List<Type> ToolGAgentTypes { get; set; } = new();
    [Id(4)] public List<GrainId> ToolGAgents { get; set; } = new();

    // ✨ 新增：动态配置支持
    [Id(5)] public LLMProviderEnum? PreferredProvider { get; set; }
    [Id(6)] public ModelIdEnum? PreferredModel { get; set; }
    [Id(7)] public string? ProjectId { get; set; }
    [Id(8)] public string? UserId { get; set; }
    [Id(9)] public string? WorkflowId { get; set; }
}
```

#### ChatAIGAgent 集成
```csharp
using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

public class ChatAIGAgent :
    GroupMemberGAgentBase<ChatAIGAgentState, ChatAIGAgentEvent, EventBase, ChatAIGAgentConfigDto>,
    IChatAIGAgent
{
    protected override async Task PerformConfigAsync(ChatAIGAgentConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);

        // Build business context with workflow and user information
        var businessContext = ConfigurationContext
            .ForWorkflow(configuration.WorkflowId ?? "default-workflow")
            .WithProject(configuration.ProjectId ?? "default-project")
            .WithUser(configuration.UserId ?? "system-user");

        // Use dynamic configuration with separated Provider + Model
        var llmConfig = new LLMConfigDto
        {
            ProviderType = configuration.PreferredProvider ?? LLMProviderEnum.OpenAI,
            ModelType = configuration.PreferredModel ?? ModelIdEnum.OpenAI
        };

        // Initialize with business context for dynamic configuration resolution
        await InitializeAsync(new InitializeDto
        {
            Instructions = configuration.Instructions,
            LLMConfig = llmConfig,
            MCPServers = configuration.MCPServers,
            ToolGAgentTypes = configuration.ToolGAgentTypes,
            ToolGAgents = configuration.ToolGAgents,
        }, businessContext);

        Logger.LogInformation("ChatAI Agent initialized with dynamic LLM configuration");
    }
}
```

### 3. 配置作用域映射

不同的业务场景使用不同的配置作用域：

```csharp
// 系统级配置 (所有 Agent 共享)
var systemContext = ConfigurationContext.Default;

// 项目级配置 (项目内 Agent 共享)
var projectContext = ConfigurationContext.ForProject("project-123");

// 用户级配置 (特定用户的 Agent)
var userContext = ConfigurationContext.ForUser("user-456");

// 工作流级配置 (特定工作流的 Agent)
var workflowContext = ConfigurationContext.ForWorkflow("workflow-789");

// 复合上下文 (多层级配置)
var complexContext = ConfigurationContext
    .ForProject("project-123")
    .WithUser("user-456")
    .WithWorkflow("workflow-789");
```

### 4. 配置级联查找

配置系统按以下优先级查找配置：

```
Workflow (TertiaryScope) → Project (SecondaryScope) → User (PrimaryScope) → System
```

示例：
```csharp
// 如果 workflow-789 有特定的 temperature 配置，使用 workflow 级别的
// 否则检查 project-123 的配置
// 否则检查 user-456 的配置  
// 最后使用系统默认配置
```

## 🔄 端到端使用流程

### 场景：为群聊 AI Agent 配置个性化 LLM

#### 步骤 1: 管理员设置项目级配置

假设我们有一个客服项目，需要为 ChatAIGAgent 配置专门的 LLM 参数：
```bash
# 设置客服项目的 Azure OpenAI Provider
curl -X POST "/api/llm-config/providers" \
  -H "Content-Type: application/json" \
  -d '{
    "providerType": "Azure",
    "context": { "secondaryScope": "project-customer-service" },
    "settings": {
      "resourceName": "customer-service-openai",
      "apiVersion": "2024-02-01"
    }
  }'

# 设置客服专用的 GPT-4 Model 配置（更保守的参数）
curl -X POST "/api/llm-config/models" \
  -H "Content-Type: application/json" \
  -d '{
    "modelType": "OpenAI", 
    "context": { "secondaryScope": "project-customer-service" },
    "settings": {
      "modelName": "gpt-4",
      "temperature": 0.2,
      "maxTokens": 1500,
      "azureDeploymentName": "gpt-4-customer-service"
    }
  }'

# 设置项目的 API Key
curl -X POST "/api/llm-config/api-keys" \
  -H "Content-Type: application/json" \
  -d '{
    "keyName": "AZURE_OPENAI_API_KEY",
    "keyValue": "actual-api-key-here",
    "context": { "secondaryScope": "project-customer-service" }
  }'
```

#### 步骤 2: 开发者创建客服 ChatAIGAgent

```csharp
// 创建客服专用的 ChatAIGAgent 配置
var customerServiceConfig = new ChatAIGAgentConfigDto
{
    MemberName = "CustomerServiceBot",
    Instructions = "You are a helpful customer service assistant. Be polite, professional, and concise in your responses.",
    
    // 指定使用 Azure + OpenAI 组合
    PreferredProvider = LLMProviderEnum.Azure,
    PreferredModel = ModelIdEnum.OpenAI,
    
    // 业务上下文
    ProjectId = "project-customer-service",
    UserId = "system-bot",
    WorkflowId = null, // 项目级配置，不限定工作流
    
    // 工具配置
    MCPServers = new List<MCPServerConfig>(),
    ToolGAgentTypes = new List<Type>(),
    ToolGAgents = new List<GrainId>()
};

// 通过 GAgentFactory 创建 Agent
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
var chatAgent = await gAgentFactory.GetGAgentAsync<IChatAIGAgent>(
    Guid.NewGuid(), 
    customerServiceConfig
);
```

#### 步骤 3: Agent 自动使用配置

```csharp
// 当 ChatAIGAgent 初始化时，AIGAgentBase 内部自动执行：

// 1. BuildBusinessContext() 创建配置上下文
var context = ConfigurationContext.ForProject("project-customer-service");

// 2. LLMConfigurationService 查找项目级配置
// - Azure Provider: resourceName = "customer-service-openai" 
// - OpenAI Model: temperature = 0.2, maxTokens = 1500
// - API Key: AZURE_OPENAI_API_KEY

// 3. 创建完整的 LLMService 实例
var llmService = new LLMService(
    new AzureProviderConfiguration { 
        ResourceName = "customer-service-openai" 
    },
    new OpenAIModelConfiguration { 
        Temperature = 0.2, 
        MaxTokens = 1500,
        AzureDeploymentName = "gpt-4-customer-service"
    }
);

// 4. 初始化 Brain 并准备处理群聊消息
```

#### 步骤 4: 群聊中的实际使用

```csharp
// 用户在群聊中发送消息
var userMessages = new List<WorkflowChatMessage>
{
    new WorkflowChatMessage 
    { 
        Content = "我的订单什么时候能到？订单号是 12345",
        Sender = "customer_001"
    }
};

// ChatAIGAgent 处理消息
var response = await chatAgent.ChatAsync(blackboardId, userMessages);

// AI 使用客服专用配置生成回复：
// - 使用 temperature=0.2 确保回复准确、一致
// - 使用 maxTokens=1500 确保回复简洁
// - 使用客服专用的 Azure 部署
Console.WriteLine(response.Content); 
// "您好！关于订单12345，我来帮您查询一下。根据我们的系统显示..."
```

## 🧪 测试配置

### 验证客服项目配置完整性
```bash
POST /api/llm-config/validate
Content-Type: application/json

{
  "context": { "secondaryScope": "project-customer-service" },
  "requiredProviders": ["Azure"],
  "requiredModels": ["OpenAI"],
  "requiredApiKeys": ["AZURE_OPENAI_API_KEY"]
}

# 响应示例:
{
  "isValid": true,
  "missingProviders": [],
  "missingModels": [],
  "missingApiKeys": [],
  "configurationErrors": [],
  "validationDetails": {
    "totalChecked": 3,
    "totalMissing": 0
  }
}
```

### 测试客服 ChatAIGAgent 的 LLM 连接
```bash
POST /api/llm-config/test
Content-Type: application/json

{
  "providerType": "Azure",
  "modelType": "OpenAI", 
  "context": { "secondaryScope": "project-customer-service" },
  "testPrompt": "您好，我是客服助手，请问有什么可以帮助您的吗？"
}

# 响应示例:
{
  "isSuccessful": true,
  "response": "您好！我很高兴为您服务。请告诉我您遇到的问题，我会尽力帮助您解决。",
  "responseTime": "00:00:02.345",
  "metadata": {
    "provider": "Azure",
    "model": "OpenAI",
    "description": "Azure-OpenAI"
  }
}
```

## 🔧 高级用法

### 用户级个性化配置

为不同类型的客服代表配置个性化的 ChatAIGAgent：

```bash
# VIP 客服代表使用更高级的模型配置
curl -X POST "/api/llm-config/models" \
  -H "Content-Type: application/json" \
  -d '{
    "modelType": "OpenAI",
    "context": { 
      "secondaryScope": "project-customer-service",
      "primaryScope": "vip-agent-alice" 
    },
    "settings": {
      "modelName": "gpt-4-turbo",
      "temperature": 0.4,
      "maxTokens": 2500
    }
  }'

# 普通客服代表使用标准配置
curl -X POST "/api/llm-config/models" \
  -H "Content-Type: application/json" \
  -d '{
    "modelType": "OpenAI",
    "context": { 
      "secondaryScope": "project-customer-service",
      "primaryScope": "standard-agent-bob" 
    },
    "settings": {
      "modelName": "gpt-4",
      "temperature": 0.2,
      "maxTokens": 1500
    }
  }'
```

### 工作流级特殊配置

为特定的客服场景配置专门的 ChatAIGAgent：

```bash
# 技术支持工作流 - 需要更详细的回答
curl -X POST "/api/llm-config/models" \
  -H "Content-Type: application/json" \
  -d '{
    "modelType": "OpenAI",
    "context": { 
      "secondaryScope": "project-customer-service",
      "tertiaryScope": "tech-support-workflow" 
    },
    "settings": {
      "modelName": "gpt-4-turbo",
      "temperature": 0.1,
      "maxTokens": 4000
    }
  }'

# 销售咨询工作流 - 需要更有说服力的回答
curl -X POST "/api/llm-config/models" \
  -H "Content-Type: application/json" \
  -d '{
    "modelType": "OpenAI",
    "context": { 
      "secondaryScope": "project-customer-service",
      "tertiaryScope": "sales-inquiry-workflow" 
    },
    "settings": {
      "modelName": "gpt-4",
      "temperature": 0.6,
      "maxTokens": 2000
    }
  }'
```

## 📊 配置优先级示例

假设客服系统中有以下配置层级：

```
System: temperature = 0.7, maxTokens = 2000
Project "customer-service": temperature = 0.2, maxTokens = 1500  
User "vip-agent-alice": temperature = 0.4, maxTokens = 2500
Workflow "tech-support": temperature = 0.1, maxTokens = 4000
```

当 Alice（VIP 客服代表）在客服项目的技术支持工作流中使用 ChatAIGAgent 时：

```csharp
var context = ConfigurationContext
    .ForProject("customer-service")    // temperature = 0.2, maxTokens = 1500
    .WithUser("vip-agent-alice")      // temperature = 0.4, maxTokens = 2500 (覆盖项目配置)
    .WithWorkflow("tech-support");    // temperature = 0.1, maxTokens = 4000 (最终使用)

// 最终配置: temperature = 0.1, maxTokens = 4000 (工作流级别优先级最高)
// 这确保了技术支持场景下的回答更加准确和详细
```

### ChatAIGAgent 在不同场景下的表现

```csharp
// 场景1: 普通客服咨询 (使用项目级配置)
var normalContext = ConfigurationContext.ForProject("customer-service");
// temperature = 0.2, maxTokens = 1500 -> 简洁、准确的回答

// 场景2: VIP 客服咨询 (使用用户级配置)  
var vipContext = ConfigurationContext
    .ForProject("customer-service")
    .WithUser("vip-agent-alice");
// temperature = 0.4, maxTokens = 2500 -> 更详细、个性化的回答

// 场景3: 技术支持工作流 (使用工作流级配置)
var techContext = ConfigurationContext
    .ForProject("customer-service")
    .WithUser("vip-agent-alice")
    .WithWorkflow("tech-support");
// temperature = 0.1, maxTokens = 4000 -> 非常详细、技术性的回答
```

## 🎯 最佳实践

1. **配置分层**：
   - System: 默认配置和回退值
   - Project: 项目特定需求
   - User: 用户个性化偏好
   - Workflow: 特定任务优化

2. **安全考虑**：
   - API Key 永远不在日志中显示
   - 使用 HTTPS 传输配置
   - 定期轮换 API Key

3. **性能优化**：
   - 配置缓存避免重复查询
   - 批量设置相关配置
   - 异步初始化 Agent

4. **监控告警**：
   - 监控配置变更
   - API Key 过期告警
   - 配置验证失败告警

通过这个完整的集成示例，开发者可以轻松地在自己的 AI Agent 中使用动态配置系统，既保持了架构的清晰性，又提供了强大的配置管理能力。
