# AIGAgentBase 工具集成指南

## 概述

本文档介绍如何通过继承 `AIGAgentBase` 来创建一个支持工具调用的智能代理（GAgent）。该代理能够：
- 调用 MCP（Model Context Protocol）工具
- 将其他 GAgent 作为工具使用
- 自动进行工具调用和结果处理
- 跟踪工具调用历史

## 目录

1. [核心概念](#核心概念)
2. [快速开始](#快速开始)
3. [详细实现步骤](#详细实现步骤)
4. [工具集成方式](#工具集成方式)
5. [示例实现](#示例实现)
6. [最佳实践](#最佳实践)
7. [常见问题](#常见问题)

## 核心概念

### AIGAgentBase
`AIGAgentBase` 是一个抽象基类，提供了：
- **Semantic Kernel 集成**：内置对 Microsoft Semantic Kernel 的支持
- **工具管理**：自动注册和管理 MCP 工具和 GAgent 工具
- **状态管理**：基于事件溯源的状态管理
- **工具调用跟踪**：记录所有工具调用的详细信息

### 工具类型
1. **MCP 工具**：通过 Model Context Protocol 暴露的外部工具
2. **GAgent 工具**：将其他 GAgent 的功能作为工具使用

## 快速开始

### 1. 创建状态类

```csharp
[GenerateSerializer]
public class MyAIAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> ConversationHistory { get; set; } = new();
    [Id(1)] public Dictionary<string, object> CustomData { get; set; } = new();
}
```

### 2. 创建状态日志事件

```csharp
[GenerateSerializer]
public class MyAIAgentStateLogEvent : StateLogEventBase<MyAIAgentStateLogEvent>
{
}

[GenerateSerializer]
public class ConversationLogEvent : MyAIAgentStateLogEvent
{
    [Id(0)] public string Role { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
}
```

### 3. 创建接口

```csharp
public interface IMyAIAgent : IStateGAgent<MyAIAgentState>
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<string> ProcessAsync(string input);
    Task<List<ToolCallDetail>> GetToolCallHistoryAsync();
}
```

### 4. 实现 GAgent

```csharp
[GAgent("my.ai.agent", "ai")]
public class MyAIAgent : AIGAgentBase<MyAIAgentState, MyAIAgentStateLogEvent, EmptyEvent, EmptyConfiguration>, 
    IMyAIAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI Agent with MCP and GAgent tool support");
    }

    public async Task<bool> InitializeAsync(string llmSystem)
    {
        // 初始化 Brain（LLM）
        await InitializeBrainAsync(llmSystem);
        
        // 启用工具支持
        State.EnableGAgentTools = true;
        State.EnableMCPTools = true;
        
        // 注册 GAgent 工具
        await RegisterGAgentsAsToolsAsync();
        
        // 配置 MCP 服务器
        var mcpServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = new[] { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }
            }
        };
        await ConfigureMCPServersAsync(mcpServers);
        
        await ConfirmEvents();
        return true;
    }

    public async Task<string> ProcessAsync(string input)
    {
        // 清除之前的工具调用记录
        ClearToolCalls();
        
        // 处理输入
        var response = await SendMessageAsync(input);
        
        // 记录对话历史
        RaiseEvent(new ConversationLogEvent { Role = "user", Message = input });
        RaiseEvent(new ConversationLogEvent { Role = "assistant", Message = response });
        
        await ConfirmEvents();
        return response;
    }

    public Task<List<ToolCallDetail>> GetToolCallHistoryAsync()
    {
        return Task.FromResult(CurrentToolCalls.ToList());
    }
}
```

## 详细实现步骤

### 步骤 1：设置项目依赖

在 `.csproj` 文件中添加必要的包引用：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" />
  <PackageReference Include="Aevatar.GAgents.AIGAgent" />
  <PackageReference Include="Aevatar.GAgents.MCP" />
</ItemGroup>
```

### 步骤 2：配置服务

在 Orleans Silo 配置中注册必要的服务：

```csharp
siloBuilder.ConfigureServices(services =>
{
    // 配置 LLM
    services.Configure<SystemLLMConfigOptions>(options =>
    {
        options.SystemLLMConfigs = new Dictionary<string, SystemLLMConfig>
        {
            ["OpenAI"] = new SystemLLMConfig
            {
                Provider = "OpenAI",
                ApiKey = "your-api-key",
                ModelName = "gpt-4"
            },
            ["DeepSeek"] = new SystemLLMConfig
            {
                Provider = "DeepSeek",
                ApiKey = "your-api-key",
                Endpoint = "https://api.deepseek.com",
                ModelName = "deepseek-chat"
            }
        };
    });
});
```

### 步骤 3：实现工具选择逻辑

重写 `RegisterGAgentsAsToolsAsync` 方法来自定义要注册的 GAgent：

```csharp
protected override async Task RegisterGAgentsAsToolsAsync()
{
    // 设置允许的 GAgent 类型
    State.AllowedGAgentTypes = new List<string>
    {
        typeof(IMathGAgent).FullName!,
        typeof(ITimeConverterGAgent).FullName!
    };
    
    // 调用基类方法进行注册
    await base.RegisterGAgentsAsToolsAsync();
}
```

### 步骤 4：处理工具调用结果

AIGAgentBase 会自动跟踪工具调用，你可以通过 `CurrentToolCalls` 属性访问：

```csharp
public async Task<ToolCallSummary> GetToolCallSummaryAsync()
{
    var summary = new ToolCallSummary
    {
        TotalCalls = CurrentToolCalls.Count,
        SuccessfulCalls = CurrentToolCalls.Count(t => t.Success),
        AverageDuration = CurrentToolCalls.Average(t => t.DurationMs),
        ToolUsage = CurrentToolCalls
            .GroupBy(t => t.ToolName)
            .ToDictionary(g => g.Key, g => g.Count())
    };
    
    return summary;
}
```

## 工具集成方式

### 1. GAgent 工具集成

GAgent 工具会自动发现并注册。每个 GAgent 的事件会被转换为 Semantic Kernel 函数：

```csharp
// 自动生成的函数名格式：{EventTypeName}
// 例如：CalculateEvent -> calculate_event

// 函数参数从事件属性自动提取
// 例如：
[GenerateSerializer]
public class CalculateEvent : EventBase
{
    [Id(0)] public string Expression { get; set; }
}
// 会生成一个带有 expression 参数的函数
```

### 2. MCP 工具集成

MCP 工具通过配置 MCP 服务器来注册：

```csharp
var mcpServers = new List<MCPServerConfig>
{
    new MCPServerConfig
    {
        ServerName = "math-server",
        Command = "python",
        Args = new[] { "math_server.py" }
    },
    new MCPServerConfig
    {
        ServerName = "web-search",
        Command = "node",
        Args = new[] { "web-search-server.js" }
    }
};

await ConfigureMCPServersAsync(mcpServers);
```

### 3. 工具调用行为

使用 Semantic Kernel 的自动工具调用功能：

```csharp
protected override async Task<string> SendMessageAsync(string message)
{
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.7,
        MaxTokens = 1000
    };
    
    return await ProcessWithBrainAsync(message, executionSettings);
}
```

## 示例实现

### 完整的工具调用 AI Agent

```csharp
[GAgent("toolcalling.ai", "ai")]
public class ToolCallingAIAgent : AIGAgentBase<ToolCallingAIAgentState, ToolCallingStateLogEvent, EmptyEvent, EmptyConfiguration>, 
    IToolCallingAIAgent
{
    private readonly List<ToolCallRecord> _toolCallRecords = new();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI Agent with comprehensive tool support");
    }

    public async Task<bool> InitializeAsync(InitConfig config)
    {
        try
        {
            // 1. 初始化 LLM
            await InitializeBrainAsync(config.LLMSystem);
            
            // 2. 启用工具
            State.EnableGAgentTools = config.EnableGAgentTools;
            State.EnableMCPTools = config.EnableMCPTools;
            
            // 3. 设置允许的 GAgent
            if (config.AllowedGAgents?.Any() == true)
            {
                State.AllowedGAgentTypes = config.AllowedGAgents;
            }
            
            // 4. 注册 GAgent 工具
            if (State.EnableGAgentTools)
            {
                await RegisterGAgentsAsToolsAsync();
            }
            
            // 5. 配置 MCP 服务器
            if (State.EnableMCPTools && config.MCPServers?.Any() == true)
            {
                await ConfigureMCPServersAsync(config.MCPServers);
            }
            
            // 6. 记录初始化事件
            RaiseEvent(new InitializedLogEvent 
            { 
                Config = config,
                Timestamp = DateTime.UtcNow
            });
            
            await ConfirmEvents();
            
            Logger.LogInformation("ToolCallingAIAgent initialized successfully with {GAgentCount} GAgent tools and {MCPCount} MCP servers",
                State.RegisteredGAgentFunctions?.Count ?? 0,
                State.MCPAgents?.Count ?? 0);
                
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize ToolCallingAIAgent");
            return false;
        }
    }

    public async Task<ProcessResult> ProcessAsync(string input)
    {
        try
        {
            // 清除上次的工具调用记录
            ClearToolCalls();
            
            // 记录开始时间
            var startTime = DateTime.UtcNow;
            
            // 创建对话历史
            var chatHistory = new ChatHistory();
            
            // 添加系统提示
            chatHistory.AddSystemMessage(GetSystemPrompt());
            
            // 添加历史对话（保留最近10条）
            foreach (var msg in State.ConversationHistory.TakeLast(10))
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else
                    chatHistory.AddAssistantMessage(msg.Content);
            }
            
            // 添加当前输入
            chatHistory.AddUserMessage(input);
            
            // 配置执行设置
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 1000
            };
            
            // 调用 LLM
            var response = await GetChatCompletionAsync(chatHistory, executionSettings);
            
            // 计算耗时
            var duration = DateTime.UtcNow - startTime;
            
            // 记录对话
            RaiseEvent(new ConversationLogEvent 
            { 
                Role = "user", 
                Message = input,
                Timestamp = startTime
            });
            
            RaiseEvent(new ConversationLogEvent 
            { 
                Role = "assistant", 
                Message = response,
                Timestamp = DateTime.UtcNow
            });
            
            // 记录工具调用
            foreach (var toolCall in CurrentToolCalls)
            {
                RaiseEvent(new ToolCallLogEvent
                {
                    ToolName = toolCall.ToolName,
                    ServerName = toolCall.ServerName,
                    Arguments = JsonSerializer.Serialize(toolCall.Arguments),
                    Result = toolCall.Result,
                    Success = toolCall.Success,
                    DurationMs = toolCall.DurationMs,
                    Timestamp = DateTime.Parse(toolCall.Timestamp)
                });
            }
            
            await ConfirmEvents();
            
            return new ProcessResult
            {
                Response = response,
                ToolCalls = CurrentToolCalls.ToList(),
                ProcessingTimeMs = (long)duration.TotalMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process input: {Input}", input);
            
            return new ProcessResult
            {
                Response = $"Error: {ex.Message}",
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string GetSystemPrompt()
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("You are a helpful AI assistant with access to various tools.");
        prompt.AppendLine();
        
        if (State.EnableGAgentTools && State.RegisteredGAgentFunctions?.Any() == true)
        {
            prompt.AppendLine("Available GAgent tools:");
            foreach (var func in State.RegisteredGAgentFunctions)
            {
                prompt.AppendLine($"- {func}");
            }
            prompt.AppendLine();
        }
        
        if (State.EnableMCPTools && State.RegisteredMCPFunctions?.Any() == true)
        {
            prompt.AppendLine("Available MCP tools:");
            foreach (var func in State.RegisteredMCPFunctions)
            {
                prompt.AppendLine($"- {func}");
            }
            prompt.AppendLine();
        }
        
        prompt.AppendLine("Use these tools when appropriate to help answer questions or complete tasks.");
        prompt.AppendLine("Always explain what you're doing when using tools.");
        
        return prompt.ToString();
    }

    public Task<List<ToolCallDetail>> GetToolCallHistoryAsync()
    {
        return Task.FromResult(State.ToolCallHistory);
    }

    public async Task<ToolUsageStats> GetToolUsageStatsAsync()
    {
        var stats = new ToolUsageStats
        {
            TotalCalls = State.ToolCallHistory.Count,
            SuccessfulCalls = State.ToolCallHistory.Count(t => t.Success),
            FailedCalls = State.ToolCallHistory.Count(t => !t.Success),
            AverageResponseTime = State.ToolCallHistory.Any() 
                ? State.ToolCallHistory.Average(t => t.DurationMs) 
                : 0,
            ToolUsageByName = State.ToolCallHistory
                .GroupBy(t => t.ToolName)
                .ToDictionary(
                    g => g.Key,
                    g => new ToolStats
                    {
                        CallCount = g.Count(),
                        SuccessCount = g.Count(t => t.Success),
                        AverageDuration = g.Average(t => t.DurationMs)
                    }
                ),
            ToolUsageByServer = State.ToolCallHistory
                .GroupBy(t => t.ServerName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                )
        };
        
        return stats;
    }
}
```

## 最佳实践

### 1. 错误处理

始终在工具调用中实现适当的错误处理：

```csharp
protected override async Task RegisterGAgentsAsToolsAsync()
{
    try
    {
        await base.RegisterGAgentsAsToolsAsync();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to register GAgent tools");
        // 可以选择降级到无工具模式
        State.EnableGAgentTools = false;
        await ConfirmEvents();
    }
}
```

### 2. 工具选择策略

根据场景选择合适的工具：

```csharp
private bool ShouldUseGAgentTools(string query)
{
    // 基于查询内容决定是否需要 GAgent 工具
    var mathKeywords = new[] { "calculate", "compute", "math", "计算" };
    var timeKeywords = new[] { "time", "timezone", "convert", "时间", "时区" };
    
    return mathKeywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
           timeKeywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase));
}
```

### 3. 性能优化

- **缓存工具实例**：避免重复创建 GAgent 实例
- **批量注册**：一次性注册所有工具而不是逐个注册
- **异步处理**：充分利用异步特性

```csharp
private readonly Dictionary<GrainType, IGAgent> _gAgentCache = new();

private async Task<IGAgent> GetOrCreateGAgentAsync(GrainType grainType)
{
    if (!_gAgentCache.TryGetValue(grainType, out var gAgent))
    {
        gAgent = await _gAgentFactory.GetGAgentAsync(grainType);
        _gAgentCache[grainType] = gAgent;
    }
    return gAgent;
}
```

### 4. 监控和日志

实现详细的监控和日志记录：

```csharp
protected override void OnToolCallCompleted(ToolCallDetail toolCall)
{
    Logger.LogInformation(
        "[ToolCall] {ToolName} from {ServerName} completed in {Duration}ms with result: {Success}",
        toolCall.ToolName,
        toolCall.ServerName,
        toolCall.DurationMs,
        toolCall.Success ? "Success" : "Failed"
    );
    
    // 发送指标到监控系统
    _telemetry.TrackMetric("tool.call.duration", toolCall.DurationMs, 
        new Dictionary<string, string>
        {
            ["tool"] = toolCall.ToolName,
            ["server"] = toolCall.ServerName,
            ["success"] = toolCall.Success.ToString()
        });
}
```

## 常见问题

### Q1: 如何限制可用的 GAgent 工具？

A: 使用 `AllowedGAgentTypes` 属性：

```csharp
State.AllowedGAgentTypes = new List<string>
{
    typeof(IMathGAgent).FullName!,
    typeof(ITimeConverterGAgent).FullName!
};
```

### Q2: 如何处理工具调用超时？

A: 在执行设置中配置超时：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await ProcessWithBrainAsync(input, executionSettings, cts.Token);
```

### Q3: 如何调试工具调用？

A: 启用详细日志并检查 `CurrentToolCalls`：

```csharp
Logger.LogDebug("Tool calls for this request: {ToolCalls}", 
    JsonSerializer.Serialize(CurrentToolCalls, new JsonSerializerOptions { WriteIndented = true }));
```

### Q4: 如何自定义工具函数名？

A: 重写 `GenerateFunctionName` 方法：

```csharp
protected override string GenerateFunctionName(GrainType grainType, Type eventType)
{
    // 自定义命名逻辑
    return $"custom_{eventType.Name.ToLower()}";
}
```

### Q5: 如何处理大量工具？

A: 实现工具分组和按需加载：

```csharp
public async Task LoadToolGroupAsync(string groupName)
{
    var toolsInGroup = GetToolsForGroup(groupName);
    foreach (var tool in toolsInGroup)
    {
        await RegisterToolAsync(tool);
    }
}
```

## 总结

通过继承 `AIGAgentBase`，你可以快速创建一个功能强大的 AI Agent，它能够：
- 自动发现和注册 GAgent 工具
- 集成 MCP 协议的外部工具
- 使用 Semantic Kernel 进行智能工具调用
- 跟踪和分析工具使用情况

关键是理解工具注册机制、正确配置 LLM、并实现适当的错误处理和监控。遵循本指南中的最佳实践，你可以构建一个稳定、高效的工具调用 AI Agent。