# 📐 MCP GAgent 设计文档

## 📅 创建日期：2025-07-04
## 🔄 最后更新：2025-07-04

---

## 1. 概述

MCP GAgent是一个能够与MCP（Model Context Protocol）server进行交互的GAgent实现。它允许像Cursor那样配置MCP server，并通过统一的事件系统（EventBase和EventWithResponseBase）进行通信。

### 1.1 核心功能
- 支持配置多个MCP server连接
- 通过事件驱动的方式调用MCP工具
- 支持工具调用的请求和响应
- 维护MCP session状态
- 支持工具发现和动态注册

## 2. 架构设计

### 2.1 类层次结构

```
GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    └── MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
            └── MCPGAgent
```

### 2.2 核心组件

#### 2.2.1 配置类

```csharp
[GenerateSerializer]
public class MCPServerConfig : ConfigurationBase
{
    [Id(0)] public string ServerName { get; set; }
    [Id(1)] public string Command { get; set; }
    [Id(2)] public List<string> Args { get; set; } = new();
    [Id(3)] public Dictionary<string, string> Environment { get; set; } = new();
    [Id(4)] public bool AutoReconnect { get; set; } = true;
    [Id(5)] public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}

[GenerateSerializer]
public class MCPGAgentConfig : ConfigurationBase
{
    [Id(0)] public List<MCPServerConfig> Servers { get; set; } = new();
    [Id(1)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    [Id(2)] public bool EnableToolDiscovery { get; set; } = true;
}
```

#### 2.2.2 状态类

```csharp
[GenerateSerializer]
public class MCPGAgentState : StateBase
{
    [Id(0)] public Dictionary<string, MCPServerState> ServerStates { get; set; } = new();
    [Id(1)] public Dictionary<string, MCPToolInfo> AvailableTools { get; set; } = new();
    [Id(2)] public List<MCPServerConfig> ServerConfigs { get; set; } = new();
    [Id(3)] public int TotalToolCalls { get; set; } = 0;
    [Id(4)] public DateTime LastToolCallTime { get; set; }
}

[GenerateSerializer]
public class MCPServerState
{
    [Id(0)] public string ServerName { get; set; }
    [Id(1)] public bool IsConnected { get; set; }
    [Id(2)] public DateTime LastConnectedTime { get; set; }
    [Id(3)] public string SessionId { get; set; }
    [Id(4)] public List<string> RegisteredTools { get; set; } = new();
}
```

## 3. 事件定义

### 3.1 请求/响应事件

```csharp
// 请求事件
[GenerateSerializer]
[Description("Call a tool on MCP server")]
public class MCPToolCallEvent : EventWithResponseBase<MCPToolResponseEvent>
{
    [Id(0)] public string ServerName { get; set; }
    [Id(1)] public string ToolName { get; set; }
    [Id(2)] public Dictionary<string, object> Arguments { get; set; }
    [Id(3)] public Guid RequestId { get; set; } = Guid.NewGuid();
}

// 响应事件
[GenerateSerializer]
[Description("Response from MCP tool call")]
public class MCPToolResponseEvent : EventBase
{
    [Id(0)] public Guid RequestId { get; set; }
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public object Result { get; set; }
    [Id(3)] public string ErrorMessage { get; set; }
    [Id(4)] public string ServerName { get; set; }
    [Id(5)] public string ToolName { get; set; }
}
```

### 3.2 状态日志事件

```csharp
[GenerateSerializer]
public class MCPGAgentStateLogEvent : StateLogEventBase<MCPGAgentStateLogEvent>
{
}

[GenerateSerializer]
public class AddMCPServerLogEvent : MCPGAgentStateLogEvent
{
    [Id(0)] public MCPServerConfig ServerConfig { get; set; }
}

[GenerateSerializer]
public class UpdateServerStateLogEvent : MCPGAgentStateLogEvent
{
    [Id(0)] public string ServerName { get; set; }
    [Id(1)] public MCPServerState ServerState { get; set; }
}
```

## 4. 实现细节

### 4.1 MCPGAgentBase实现框架

```csharp
public abstract class MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IMCPGAgent
    where TState : MCPGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MCPGAgentConfig
{
    private readonly IMCPClientProvider _mcpClientProvider;
    
    protected MCPGAgentBase()
    {
        _mcpClientProvider = ServiceProvider.GetRequiredService<IMCPClientProvider>();
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        // 添加服务器配置
        foreach (var serverConfig in configuration.Servers)
        {
            RaiseEvent(new AddMCPServerLogEvent { ServerConfig = serverConfig });
        }
        
        await ConfirmEvents();
        
        // 初始化MCP连接
        await InitializeMCPServersAsync();
    }

    [EventHandler]
    public async Task<MCPToolResponseEvent> HandleEventAsync(MCPToolCallEvent @event)
    {
        try
        {
            var client = await _mcpClientProvider.GetOrCreateClientAsync(
                State.ServerConfigs.First(s => s.ServerName == @event.ServerName));
                
            var result = await client.CallToolAsync(@event.ToolName, @event.Arguments);
            
            return new MCPToolResponseEvent
            {
                RequestId = @event.RequestId,
                Success = result.Success,
                Result = result.Data,
                ErrorMessage = result.ErrorMessage,
                ServerName = @event.ServerName,
                ToolName = @event.ToolName
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling MCP tool");
            
            return new MCPToolResponseEvent
            {
                RequestId = @event.RequestId,
                Success = false,
                ErrorMessage = ex.Message,
                ServerName = @event.ServerName,
                ToolName = @event.ToolName
            };
        }
    }
}
```

## 5. 使用示例

### 5.1 配置MCP GAgent

```csharp
var mcpConfig = new MCPGAgentConfig
{
    Servers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" },
            Environment = new Dictionary<string, string>
            {
                ["NODE_ENV"] = "production"
            }
        }
    },
    EnableToolDiscovery = true
};

var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>();
await mcpGAgent.ConfigureAsync(mcpConfig);
```

### 5.2 调用MCP工具

```csharp
// 发布工具调用事件
var toolCallEvent = new MCPToolCallEvent
{
    ServerName = "filesystem",
    ToolName = "read_file",
    Arguments = new Dictionary<string, object>
    {
        ["path"] = "/path/to/file.txt"
    }
};

// 发布事件并等待响应
var response = await PublishAndWaitForResponseAsync<MCPToolCallEvent, MCPToolResponseEvent>(toolCallEvent);
```

## 6. 实现计划

### Phase 1: 基础实现
- [ ] 实现MCPGAgentBase基类
- [ ] 实现基本的配置和状态管理
- [ ] 实现事件处理框架

### Phase 2: MCP集成
- [ ] 实现IMCPClientProvider
- [ ] 集成MCP SDK
- [ ] 实现工具发现功能

### Phase 3: 高级功能
- [ ] 实现连接池管理
- [ ] 实现缓存机制
- [ ] 实现重试和错误处理

### Phase 4: 测试和优化
- [ ] 编写单元测试
- [ ] 编写集成测试
- [ ] 性能优化
- [ ] 文档完善