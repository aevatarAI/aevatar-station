using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.Extensions;
using Aevatar.GAgents.MCP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Examples;

/// <summary>
/// MCPGAgent使用示例代码
/// </summary>
public class MCPGAgentUsageExamples
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IMCPServerRegistry _mcpServerRegistry;

    public MCPGAgentUsageExamples(IGAgentFactory gAgentFactory, IMCPServerRegistry mcpServerRegistry)
    {
        _gAgentFactory = gAgentFactory;
        _mcpServerRegistry = mcpServerRegistry;
    }

    /// <summary>
    /// 示例1：使用DefaultMCPServer枚举创建核心服务器
    /// </summary>
    public async Task Example1_CreateCoreServersAsync()
    {
        // 1. 创建不需要环境变量的服务器
        var filesystemAgent = await _gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.Filesystem);
        var memoryAgent = await _gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.Memory);
        var sequentialThinkingAgent = await _gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.SequentialThinking);

        // 2. 创建需要环境变量的服务器
        var githubAgent = await _gAgentFactory.GetMCPGAgentAsync(
            DefaultMCPServer.GitHub,
            env: new Dictionary<string, string>
            {
                ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "your_github_token_here"
            });

        var slackAgent = await _gAgentFactory.GetMCPGAgentAsync(
            DefaultMCPServer.Slack,
            env: new Dictionary<string, string>
            {
                ["SLACK_BOT_TOKEN"] = "xoxb-your-slack-bot-token"
            });

        // 3. 使用自定义参数
        var customFilesystemAgent = await _gAgentFactory.GetMCPGAgentAsync(
            DefaultMCPServer.Filesystem,
            customArgs: new[] { "-y", "@modelcontextprotocol/server-filesystem", "/custom/path", "/another/path" });

        Console.WriteLine("Core servers created successfully!");
    }

    /// <summary>
    /// 示例2：使用配置文件中定义的服务器
    /// </summary>
    public async Task Example2_CreateConfiguredServersAsync()
    {
        // 1. 创建配置文件中定义的服务器（Stdio类型）
        var workflowAgent = await _gAgentFactory.GetMCPGAgentAsync("workflow-engine");
        var notionAgent = await _gAgentFactory.GetMCPGAgentAsync("notion");

        // 2. 创建StreamableHttp类型的服务器
        var companyAgent = await _gAgentFactory.GetMCPGAgentAsync("company-crm");
        var internalKbAgent = await _gAgentFactory.GetMCPGAgentAsync("internal-kb");
        var salesforceAgent = await _gAgentFactory.GetMCPGAgentAsync("salesforce");

        // 3. 检查服务器是否存在
        if (await _gAgentFactory.GetMCPGAgentAsync("non-existent-server") == null)
        {
            Console.WriteLine("Server 'non-existent-server' not found in whitelist");
        }

        // 4. 创建实时数据流服务器
        var zhipuSearchAgent = await _gAgentFactory.GetMCPGAgentAsync("zhipu-web-search-sse");
        var stockDataAgent = await _gAgentFactory.GetMCPGAgentAsync("realtime-stock-data");

        Console.WriteLine("Configured servers created successfully!");
        Console.WriteLine($"Created {(companyAgent != null ? 1 : 0) + (internalKbAgent != null ? 1 : 0) + (salesforceAgent != null ? 1 : 0)} StreamableHttp servers");
    }

    /// <summary>
    /// 示例3：便利方法的使用
    /// </summary>
    public async Task Example3_ConvenienceMethodsAsync()
    {
        // 1. 文件系统服务器便利方法（Stdio）
        var fsAgent1 = await _gAgentFactory.GetFilesystemMCPGAgentAsync("/tmp");
        var fsAgent2 = await _gAgentFactory.GetFilesystemMCPGAgentAsync("/tmp", "/Users", "/home");

        // 2. StreamableHttp服务器便利方法
        // 基本SSE服务器
        var basicSseAgent = await _gAgentFactory.GetStreamableHttpMCPGAgentAsync(
            "basic-sse-server",
            "https://api.example.com/mcp/stream");

        // 带Bearer认证的SSE服务器
        var authSseAgent = await _gAgentFactory.GetStreamableHttpMCPGAgentWithAuthAsync(
            "authenticated-sse-server",
            "https://secure-api.example.com/mcp/events",
            "your-bearer-token-here");

        // 带API Key认证的SSE服务器
        var apiKeySseAgent = await _gAgentFactory.GetStreamableHttpMCPGAgentWithApiKeyAsync(
            "api-key-sse-server",
            "https://third-party-api.com/mcp/stream",
            "your-api-key-here",
            "X-API-Key");

        // 带自定义头部的SSE服务器
        var customHeadersAgent = await _gAgentFactory.GetStreamableHttpMCPGAgentAsync(
            "custom-headers-server",
            "https://enterprise-api.com/mcp/stream",
            new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer custom-token",
                ["X-Client-Version"] = "1.0",
                ["X-Request-ID"] = Guid.NewGuid().ToString(),
                ["Accept"] = "text/event-stream",
                ["Cache-Control"] = "no-cache"
            },
            "Enterprise API with custom headers");

        // 3. 检查服务器注册状态
        var coreServers = new[] { "filesystem", "memory", "sequential-thinking", "github" };
        foreach (var serverName in coreServers)
        {
            var isRegistered = _gAgentFactory.IsMCPServerRegistered(serverName);
            Console.WriteLine($"Server '{serverName}' is registered: {isRegistered}");
        }

        // 4. 获取所有已注册服务器
        var allServers = _gAgentFactory.GetRegisteredMCPServerNames();
        Console.WriteLine($"Total registered servers: {allServers.Count()}");
        foreach (var serverName in allServers.Take(10)) // 显示前10个
        {
            Console.WriteLine($"  - {serverName}");
        }

        // 5. 获取服务器配置信息并区分传输类型
        var stdioServerConfig = _gAgentFactory.GetMCPServerConfig("github");
        if (stdioServerConfig != null)
        {
            Console.WriteLine($"Stdio Server - {stdioServerConfig.ServerName}: {stdioServerConfig.Description}");
            Console.WriteLine($"Command: {stdioServerConfig.Command} {string.Join(" ", stdioServerConfig.Args)}");
        }

        var sseServerConfig = _gAgentFactory.GetMCPServerConfig("company-crm");
        if (sseServerConfig != null)
        {
            Console.WriteLine($"StreamableHttp Server - {sseServerConfig.ServerName}: {sseServerConfig.Description}");
            Console.WriteLine($"URL: {sseServerConfig.Url}");
            Console.WriteLine($"Headers: {string.Join(", ", sseServerConfig.Headers.Select(h => $"{h.Key}=***"))}");
        }
    }

    /// <summary>
    /// 示例4：注册表的直接使用
    /// </summary>
    public async Task Example4_RegistryDirectUsageAsync()
    {
        // 1. 动态注册新服务器
        _mcpServerRegistry.RegisterServer("custom-tool", new MCPServerConfig
        {
            ServerName = "custom-tool",
            Command = "npx",
            Args = new List<string> { "-y", "@company/custom-mcp-tool" },
            Description = "Company's custom MCP tool",
            Type = MCPServerType.Stdio,
            Env = new Dictionary<string, string>
            {
                ["CUSTOM_TOOL_API_KEY"] = "api_key_here"
            }
        });

        // 2. 批量注册服务器
        var newServers = new Dictionary<string, MCPServerConfig>
        {
            ["tool-a"] = new MCPServerConfig
            {
                ServerName = "tool-a",
                Command = "node",
                Args = new List<string> { "/path/to/tool-a.js" },
                Description = "Tool A description",
                Type = MCPServerType.Stdio
            },
            ["tool-b"] = new MCPServerConfig
            {
                ServerName = "tool-b",
                Url = "https://api.toolb.com/mcp",
                Description = "Tool B description",
                Type = MCPServerType.StreamableHttp
            }
        };

        _mcpServerRegistry.RegisterServers(newServers);

        // 3. 获取所有配置
        var allConfigs = _mcpServerRegistry.GetAllServerConfigs();
        Console.WriteLine($"Total configurations: {allConfigs.Count}");

        // 4. 现在可以使用新注册的服务器
        var customToolAgent = await _gAgentFactory.GetMCPGAgentAsync("custom-tool");
        var toolAAgent = await _gAgentFactory.GetMCPGAgentAsync("tool-a");
        var toolBAgent = await _gAgentFactory.GetMCPGAgentAsync("tool-b");

        Console.WriteLine("Dynamic server registration completed!");
    }

    /// <summary>
    /// 示例5：在AIGAgent中使用MCP服务器
    /// </summary>
    public async Task Example5_AIGAgentIntegrationAsync()
    {
        // 这个示例展示如何在AIGAgent的初始化中配置MCP服务器
        var mcpServerConfigs = new List<MCPServerConfig>();

        // 添加核心工具
        var filesystemConfig = _mcpServerRegistry.GetServerConfig("filesystem");
        var memoryConfig = _mcpServerRegistry.GetServerConfig("memory");
        if (filesystemConfig != null) mcpServerConfigs.Add(filesystemConfig);
        if (memoryConfig != null) mcpServerConfigs.Add(memoryConfig);

        // 添加需要认证的服务器
        var githubConfig = _mcpServerRegistry.GetServerConfig("github");
        if (githubConfig != null)
        {
            var workingGithubConfig = githubConfig.Clone();
            workingGithubConfig.Env = new Dictionary<string, string>
            {
                ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "your_token_here"
            };
            mcpServerConfigs.Add(workingGithubConfig);
        }

        // 在AIGAgent中配置
        // var aiGAgent = await gAgentFactory.GetGAgentAsync<IMyAIGAgent>();
        // var configResult = await aiGAgent.ConfigureMCPServersAsync(mcpServerConfigs);
        
        Console.WriteLine($"Prepared {mcpServerConfigs.Count} MCP servers for AIGAgent integration");
    }

    /// <summary>
    /// 示例6：错误处理和验证
    /// </summary>
    public async Task Example6_ErrorHandlingAsync()
    {
        try
        {
            // 1. 尝试创建不存在的服务器
            var nonExistentAgent = await _gAgentFactory.GetMCPGAgentAsync("non-existent-server");
            if (nonExistentAgent == null)
            {
                Console.WriteLine("Server not found - handled gracefully");
            }

            // 2. 尝试创建需要环境变量但未提供的服务器
            try
            {
                var githubAgentWithoutEnv = await _gAgentFactory.GetMCPGAgentAsync(DefaultMCPServer.GitHub);
            }
            catch (MCPServerConfigNotFoundException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            // 3. 配置验证
            var invalidConfig = new MCPServerConfig
            {
                ServerName = "", // 无效：空名称
                Command = "",    // 无效：空命令
                Type = MCPServerType.Stdio
            };

            var validationResult = invalidConfig.Validate();
            if (!validationResult.IsValid)
            {
                Console.WriteLine($"Configuration validation failed: {validationResult.GetErrorSummary()}");
            }

            // 4. 安全信息清理
            var sensitiveConfig = new MCPServerConfig
            {
                ServerName = "test",
                Command = "npx",
                Args = new List<string> { "test-server" },
                Type = MCPServerType.Stdio,
                Env = new Dictionary<string, string>
                {
                    ["API_KEY"] = "secret_key_123",
                    ["PASSWORD"] = "super_secret_password",
                    ["DEBUG_MODE"] = "true"
                }
            };

            var sanitizedConfig = sensitiveConfig.WithoutSensitiveInfo();
            Console.WriteLine("Sensitive information has been redacted for logging");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例7：服务注册和启动配置
    /// </summary>
    public static void Example7_ServiceRegistration(IServiceCollection services)
    {
        // 在Program.cs或Startup.cs中的配置示例

        // 1. 注册MCP服务器注册表和自动初始化
        services.AddMCPServerRegistry();

        // 2. 配置MCPServerOptions
        // services.Configure<MCPServerOptions>(configuration.GetSection("MCPServerOptions"));

        // 3. 如果只需要注册表服务（不需要自动初始化）
        // services.AddMCPServerRegistryOnly();

        Console.WriteLine("MCP services registered successfully!");
    }
}

/// <summary>
/// 完整的使用示例程序
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // 模拟服务提供者设置
        var services = new ServiceCollection();
        
        // 注册MCP服务
        services.AddMCPServerRegistry();
        
        // 构建服务提供者
        var serviceProvider = services.BuildServiceProvider();
        
        // 获取服务
        var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
        var mcpServerRegistry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        
        // 创建示例实例
        var examples = new MCPGAgentUsageExamples(gAgentFactory, mcpServerRegistry);
        
        // 运行示例
        Console.WriteLine("=== MCPGAgent Usage Examples ===");
        
        await examples.Example1_CreateCoreServersAsync();
        await examples.Example2_CreateConfiguredServersAsync();
        await examples.Example3_ConvenienceMethodsAsync();
        await examples.Example4_RegistryDirectUsageAsync();
        await examples.Example5_AIGAgentIntegrationAsync();
        await examples.Example6_ErrorHandlingAsync();
        
        Console.WriteLine("=== All examples completed ===");
    }
}
