# MCP Gateway Developer Guide

## English Documentation

### Getting Started

This guide helps developers integrate MCP Gateway functionality into their Aevatar applications and create custom MCP-enabled agents.

#### Prerequisites
- Aevatar framework setup
- Microsoft MCP Gateway deployed and accessible
- Basic understanding of Orleans and Event Sourcing

### Quick Start

#### 1. **Configure MCP Gateway**

Add configuration to your `appsettings.json`:

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://your-mcp-gateway.com",
    "AuthToken": "Bearer your-auth-token",
    "EnableSessionAffinity": true
  }
}
```

#### 2. **Create an MCPGAgent**

```csharp
[GAgent("my-mcp-agent", "tools")]
public class MyMCPGAgent : MCPGAgent, IMyMCPGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("My custom MCP-enabled agent");
}

public interface IMyMCPGAgent : IMCPGAgent
{
    // Add custom methods here
}
```

#### 3. **Configure MCP Server**

```csharp
var mcpConfig = new MCPGAgentConfig
{
    ServerConfig = new MCPServerConfig
    {
        ServerName = "my-mcp-server",
        UseGateway = true,
        GatewayAdapterName = "my-adapter",
        Description = "My custom MCP server"
    }
};

var agent = await gAgentFactory.GetGAgentAsync<IMyMCPGAgent>(Guid.NewGuid(), mcpConfig);
```

#### 4. **Use MCP Tools**

```csharp
// Get available tools
var tools = await agent.GetAvailableToolsAsync("my-mcp-server");

// Call a tool
var result = await agent.CallToolAsync("my-mcp-server", "read_file", new Dictionary<string, object>
{
    ["path"] = "/workspace/README.md"
});
```

### Advanced Usage

#### **Creating Custom Gateway Managers**

```csharp
public class CustomMCPGatewayManager : MCPGatewayManager
{
    public CustomMCPGatewayManager(
        HttpClient httpClient,
        IOptions<MCPGatewayConfig> config,
        ILogger<CustomMCPGatewayManager> logger) 
        : base(httpClient, config, logger)
    {
    }

    public async Task<CustomMetricsDto> GetCustomMetricsAsync(string adapterName)
    {
        // Implement custom metrics collection
        var response = await _httpClient.GetAsync($"/adapters/{adapterName}/custom-metrics");
        return await response.Content.ReadFromJsonAsync<CustomMetricsDto>();
    }
}
```

#### **Implementing Custom Permissions**

```csharp
public static class CustomMCPPermissions
{
    public const string GroupName = "CustomMCP";
    
    public static class Analytics
    {
        public const string Default = GroupName + ".Analytics";
        public const string ViewAdvanced = Default + ".ViewAdvanced";
        public const string Export = Default + ".Export";
    }
}
```

#### **Creating Specialized MCP Agents**

```csharp
[GAgent("file-manager", "filesystem")]
public class FileManagerMCPGAgent : MCPGAgent, IFileManagerMCPGAgent
{
    public async Task<string> ReadFileAsync(string path)
    {
        var result = await CallToolAsync("filesystem", "read_file", new Dictionary<string, object>
        {
            ["path"] = path
        });
        
        return result.Content?.ToString() ?? "";
    }

    public async Task WriteFileAsync(string path, string content)
    {
        await CallToolAsync("filesystem", "write_file", new Dictionary<string, object>
        {
            ["path"] = path,
            ["content"] = content
        });
    }
}
```

### Testing

#### **Unit Testing MCP Gateway Components**

```csharp
public class MCPGatewayManagerTests
{
    private readonly MCPGatewayManager _manager;
    private readonly Mock<HttpClient> _mockHttpClient;

    [Fact]
    public async Task CreateAdapterAsync_ShouldCallCorrectEndpoint()
    {
        // Arrange
        var input = new CreateMCPAdapterDto { Name = "test-adapter" };
        
        // Act
        await _manager.CreateAdapterAsync(input);
        
        // Assert
        _mockHttpClient.Verify(client => 
            client.PostAsJsonAsync("/adapters", It.IsAny<object>(), It.IsAny<JsonSerializerOptions>()),
            Times.Once);
    }
}
```

#### **Integration Testing**

```csharp
public class MCPGatewayIntegrationTests : AevatarApplicationTestBase
{
    [Fact]
    public async Task EndToEnd_CreateAndUseAdapter_ShouldWork()
    {
        // Create adapter through API
        var adapter = await _appService.CreateAdapterAsync(new CreateMCPAdapterDto
        {
            Name = "test-adapter",
            ImageName = "test-image",
            ImageVersion = "1.0.0"
        });

        // Create MCPGAgent with gateway configuration
        var config = new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "test-server",
                UseGateway = true,
                GatewayAdapterName = "test-adapter"
            }
        };

        var agent = await GetRequiredService<IGAgentFactory>()
            .GetGAgentAsync<IMCPGAgent>(Guid.NewGuid(), config);

        // Test tool calling
        var tools = await agent.GetAvailableToolsAsync();
        tools.Should().NotBeEmpty();
    }
}
```

### Best Practices

#### **Configuration Management**
1. Use environment-specific configuration files
2. Store sensitive tokens in secure configuration providers
3. Implement configuration validation in startup
4. Use structured logging for troubleshooting

#### **Error Handling**
1. Implement circuit breaker pattern for gateway calls
2. Use exponential backoff for retries
3. Provide meaningful error messages to users
4. Log all gateway communication for debugging

#### **Performance Optimization**
1. Use connection pooling for HTTP clients
2. Implement caching for frequently accessed data
3. Monitor gateway latency and adjust timeouts
4. Use async/await patterns consistently

#### **Security**
1. Rotate authentication tokens regularly
2. Use HTTPS for all gateway communications
3. Implement proper authorization checks
4. Audit all adapter management operations

---

## 中文文档

### 快速开始

本指南帮助开发者将MCP Gateway功能集成到他们的Aevatar应用程序中，并创建自定义的MCP启用智能体。

#### 前置条件
- Aevatar框架设置
- Microsoft MCP Gateway已部署且可访问
- 基本了解Orleans和事件溯源

### 快速开始

#### 1. **配置MCP Gateway**

在`appsettings.json`中添加配置：

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://your-mcp-gateway.com",
    "AuthToken": "Bearer your-auth-token",
    "EnableSessionAffinity": true
  }
}
```

#### 2. **创建MCPGAgent**

```csharp
[GAgent("我的MCP智能体", "工具")]
public class MyMCPGAgent : MCPGAgent, IMyMCPGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("我的自定义MCP启用智能体");
}

public interface IMyMCPGAgent : IMCPGAgent
{
    // 在此添加自定义方法
}
```

#### 3. **配置MCP服务器**

```csharp
var mcpConfig = new MCPGAgentConfig
{
    ServerConfig = new MCPServerConfig
    {
        ServerName = "我的MCP服务器",
        UseGateway = true,
        GatewayAdapterName = "my-adapter",
        Description = "我的自定义MCP服务器"
    }
};

var agent = await gAgentFactory.GetGAgentAsync<IMyMCPGAgent>(Guid.NewGuid(), mcpConfig);
```

#### 4. **使用MCP工具**

```csharp
// 获取可用工具
var tools = await agent.GetAvailableToolsAsync("我的MCP服务器");

// 调用工具
var result = await agent.CallToolAsync("我的MCP服务器", "read_file", new Dictionary<string, object>
{
    ["path"] = "/workspace/README.md"
});
```

### 高级用法

#### **创建自定义网关管理器**

```csharp
public class CustomMCPGatewayManager : MCPGatewayManager
{
    public async Task<CustomMetricsDto> GetCustomMetricsAsync(string adapterName)
    {
        // 实现自定义指标收集
        var response = await _httpClient.GetAsync($"/adapters/{adapterName}/custom-metrics");
        return await response.Content.ReadFromJsonAsync<CustomMetricsDto>();
    }
}
```

#### **实现自定义权限**

```csharp
public static class CustomMCPPermissions
{
    public const string GroupName = "自定义MCP";
    
    public static class Analytics
    {
        public const string Default = GroupName + ".分析";
        public const string ViewAdvanced = Default + ".查看高级";
        public const string Export = Default + ".导出";
    }
}
```

### 最佳实践

#### **配置管理**
1. 使用特定环境的配置文件
2. 将敏感令牌存储在安全配置提供程序中
3. 在启动时实施配置验证
4. 使用结构化日志记录进行故障排除

#### **错误处理**
1. 为网关调用实施断路器模式
2. 对重试使用指数退避
3. 向用户提供有意义的错误消息
4. 记录所有网关通信以便调试

#### **性能优化**
1. 对HTTP客户端使用连接池
2. 对频繁访问的数据实施缓存
3. 监控网关延迟并调整超时
4. 一致使用async/await模式

#### **安全性**
1. 定期轮换身份验证令牌
2. 对所有网关通信使用HTTPS
3. 实施适当的授权检查
4. 审计所有适配器管理操作
