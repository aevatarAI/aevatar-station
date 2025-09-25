using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Test.Mocks;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.MCP.Test;

/// <summary>
/// Comprehensive MCP GAgent unit tests
/// Covering all core functionalities and edge cases
/// </summary>
public class MCPGAgentTests : AevatarMCPTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly MockMcpClientProvider _mockProvider;

    public MCPGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
        _mockProvider = GetMockMcpClientProvider();
    }

    #region Configuration and Initialization Tests

    [Fact]
    public async Task ConfigureAsync_WithValidFileSystemConfig_Should_Initialize_Successfully()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            ServerConfig = CreateFileSystemServerConfig("test-filesystem"),
            RequestTimeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Assert
        var state = await mcpGAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.MCPServerConfig.ServerName.ShouldBe("test-filesystem");
        state.RequestTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        
        _testOutputHelper.WriteLine(
            $"Successfully configured MCP GAgent with server: {state.MCPServerConfig.ServerName}");
    }

    [Fact]
    public async Task ConfigureAsync_WithValidSQLiteConfig_Should_Initialize_Successfully()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            ServerConfig = CreateSQLiteServerConfig("test-sqlite"),
            RequestTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Assert
        var state = await mcpGAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.MCPServerConfig.ServerName.ShouldBe("test-sqlite");
        state.MCPServerConfig.Args.ShouldContain("memory:");
    }

    [Fact]
    public async Task ConfigureAsync_WithMultipleConfigurations_Should_Handle_Independently()
    {
        // Arrange
        var filesystemConfig = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("fs1") };
        var sqliteConfig = new MCPGAgentConfig { ServerConfig = CreateSQLiteServerConfig("sql1") };

        // Act
        var mcpGAgent1 = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(filesystemConfig);
        var mcpGAgent2 = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(sqliteConfig);

        // Assert
        var state1 = await mcpGAgent1.GetStateAsync();
        var state2 = await mcpGAgent2.GetStateAsync();

        state1.MCPServerConfig.ServerName.ShouldBe("fs1");
        state2.MCPServerConfig.ServerName.ShouldBe("sql1");

        // Verify they are independent instances
        state1.MCPServerConfig.ServerName.ShouldNotBe(state2.MCPServerConfig.ServerName);
    }

    #endregion

    #region Tool Discovery Tests

    [Fact]
    public async Task HandleEventAsync_MCPDiscoverToolsEvent_Should_Return_FileSystem_Tools()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var discoverEvent = new MCPDiscoverToolsEvent();
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, discoverEvent, typeof(MCPToolsDiscoveredEvent));

        var response = JsonConvert.DeserializeObject<MCPToolsDiscoveredEvent>(responseJson, new JsonSerializerSettings
        {
            Converters = { new GrainIdConverter() }
        });

        // Assert
        response.ShouldNotBeNull();
        response.ServerName.ShouldBe("filesystem");
        response.Tools.ShouldNotBeNull();
        response.Tools.Count.ShouldBeGreaterThan(0);

        // Verify specific tools exist
        response.Tools.ShouldContain(t => t.Name == "read_file");
        response.Tools.ShouldContain(t => t.Name == "write_file");
        response.Tools.ShouldContain(t => t.Name == "list_directory");

        _testOutputHelper.WriteLine($"Discovered {response.Tools.Count} tools");
        foreach (var tool in response.Tools)
        {
            _testOutputHelper.WriteLine($"- {tool.Name}: {tool.Description}");
        }
    }

    [Fact]
    public async Task HandleEventAsync_MCPDiscoverToolsEvent_Should_Return_SQLite_Tools()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateSQLiteServerConfig("sqlite") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var discoverEvent = new MCPDiscoverToolsEvent();
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, discoverEvent, typeof(MCPToolsDiscoveredEvent));

        var response = JsonConvert.DeserializeObject<MCPToolsDiscoveredEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Tools.ShouldContain(t => t.Name == "execute_query");
        response.Tools.ShouldContain(t => t.Name == "list_tables");
        response.Tools.ShouldContain(t => t.Name == "describe_table");
    }

    [Fact]
    public async Task GetAvailableToolsAsync_Should_Return_Available_Tools()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Discover tools first
        await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, new MCPDiscoverToolsEvent(), typeof(MCPToolsDiscoveredEvent));

        // Act
        var availableTools = await mcpGAgent.GetAvailableToolsAsync();

        // Assert
        availableTools.ShouldNotBeNull();
        availableTools.Count.ShouldBeGreaterThan(0);
        availableTools.Any(t => t.ServerName == "filesystem").ShouldBeTrue();
    }

    [Fact]
    public async Task GetAvailableToolsAsync_WithServerNameFilter_Should_Return_Filtered_Tools()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Discover tools first
        await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, new MCPDiscoverToolsEvent(), typeof(MCPToolsDiscoveredEvent));

        // Act
        var availableTools = await mcpGAgent.GetAvailableToolsAsync("filesystem");

        // Assert
        availableTools.ShouldNotBeNull();
        availableTools.All(t => t.ServerName == "filesystem").ShouldBeTrue();
    }

    #endregion

    #region Tool Call Tests

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_ReadFile_Should_Return_Success_Response()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "filesystem.read_file",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/test/example.txt"
            }
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson, new JsonSerializerSettings
        {
            Converters = { new GrainIdConverter() }
        });

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ServerName.ShouldBe("filesystem");
        response.ToolName.ShouldBe("filesystem.read_file");
        response.Result.ShouldNotBeNull();
        response.ErrorMessage.ShouldBeNull();

        _testOutputHelper.WriteLine($"Tool call result: {JsonConvert.SerializeObject(response.Result, Formatting.Indented)}");
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WriteFile_Should_Return_Success_Response()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "filesystem.write_file",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/test/output.txt",
                ["content"] = "Hello, MCP World!"
            }
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_SQLiteQuery_Should_Return_Success_Response()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateSQLiteServerConfig("sqlite") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "sqlite",
            ToolName = "sqlite.execute_query",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "SELECT * FROM users WHERE id = 1"
            }
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    [Fact]
    public async Task CallToolAsync_DirectMethod_Should_Work_Correctly()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var response = await mcpGAgent.CallToolAsync("filesystem", "read_file", new Dictionary<string, object>
        {
            ["path"] = "/test/direct-call.txt"
        });

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithNonExistentServer_Should_Return_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "non-existent-server",
            ToolName = "non-existent-server.some_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson, new JsonSerializerSettings
            {
                Converters = { new GrainIdConverter() }
            });

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldNotBeNull();
        response.Result!.ToString()!.ShouldContain("not found");
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithNonExistentTool_Should_Return_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "filesystem.non_existent_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldNotBeNull();
        response.Result!.ToString()!.ShouldContain("not found");
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithMissingRequiredParameters_Should_Return_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "filesystem.read_file",
            Arguments = new Dictionary<string, object>() // Missing required path parameter
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldNotBeNull();
        response.Result!.ToString()!.ShouldContain("is missing");
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithFailingTool_Should_Return_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateErrorServerConfig("error-server") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "error-server",
            ToolName = "error-server.failing_tool",
            Arguments = new Dictionary<string, object>
            {
                ["input"] = "test"
            }
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse(); // Mock returns success, but result contains error info
        response.Result.ShouldNotBeNull();
        response.Result!.ToString()!.ShouldContain("always fails");
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithInvalidToolNameFormat_Should_Return_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "invalid_format", // Missing server prefix
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldNotBeNull();
        response.ErrorMessage.ShouldContain("Invalid tool name");
    }

    #endregion

    #region Parameter Validation Tests

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithValidParameters_Should_ValidateCorrectly()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "filesystem.write_file",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/test/valid.txt",
                ["content"] = "Valid content"
            }
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_WithOptionalParameters_Should_Work()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateSQLiteServerConfig("sqlite") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "sqlite",
            ToolName = "sqlite.list_tables",
            Arguments = new Dictionary<string, object>() // No optional parameters
        };

        // Act
        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task GetStateAsync_Should_Return_Current_State()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            ServerConfig = CreateFileSystemServerConfig("test-filesystem"),
            RequestTimeout = TimeSpan.FromSeconds(45)
        };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var state = await mcpGAgent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.MCPServerConfig.ShouldNotBeNull();
        state.MCPServerConfig.ServerName.ShouldBe("test-filesystem");
        state.RequestTimeout.ShouldBe(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task State_Should_Track_LastToolCall()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var initialState = await mcpGAgent.GetStateAsync();
        var initialLastCall = initialState.LastToolCall;

        // Act
        await mcpGAgent.CallToolAsync("filesystem", "read_file", new Dictionary<string, object>
        {
            ["path"] = "/test/timestamp-test.txt"
        });

        var updatedState = await mcpGAgent.GetStateAsync();

        // Assert
        updatedState.LastToolCall.ShouldBeGreaterThan(initialLastCall);
    }

    #endregion

    #region Concurrency and Performance Tests

    [Fact]
    public async Task HandleEventAsync_ConcurrentToolCalls_Should_Handle_Correctly()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        var tasks = new List<Task<MCPToolResponseEvent>>();

        // Act - Start multiple concurrent tool calls
        for (int i = 0; i < 5; i++)
        {
            int index = i; // Capture loop variable
            var task = mcpGAgent.CallToolAsync("filesystem", "read_file", new Dictionary<string, object>
            {
                ["path"] = $"/test/concurrent-{index}.txt"
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(5);
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        _testOutputHelper.WriteLine($"Successfully processed {results.Length} concurrent tool calls");
    }

    [Fact]
    public async Task HandleEventAsync_MultipleServers_Should_Handle_Independently()
    {
        // Arrange
        var filesystemConfig = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var sqliteConfig = new MCPGAgentConfig { ServerConfig = CreateSQLiteServerConfig("sqlite") };

        var mcpGAgent1 = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(filesystemConfig);
        var mcpGAgent2 = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(sqliteConfig);

        // Act
        var task1 = mcpGAgent1.CallToolAsync("filesystem", "read_file", new Dictionary<string, object>
        {
            ["path"] = "/test/multi-server-1.txt"
        });

        var task2 = mcpGAgent2.CallToolAsync("sqlite", "list_tables", new Dictionary<string, object>());

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results[0].Success.ShouldBeTrue();
        results[1].Success.ShouldBeTrue();

        // Verify results come from different servers
        results[0].ServerName.ShouldBe("filesystem");
        results[1].ServerName.ShouldBe("sqlite");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWorkflow_DiscoverAndCallTools_Should_Work_EndToEnd()
    {
        // Arrange
        var config = new MCPGAgentConfig { ServerConfig = CreateFileSystemServerConfig("filesystem") };
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act 1: Discover tools
        var discoverEvent = new MCPDiscoverToolsEvent();
        var discoverResponseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, discoverEvent, typeof(MCPToolsDiscoveredEvent));

        var discoverResponse = JsonConvert.DeserializeObject<MCPToolsDiscoveredEvent>(discoverResponseJson);

        // Act 2: Call discovered tools
        var readFileTool = discoverResponse.Tools.FirstOrDefault(t => t.Name == "read_file");
        readFileTool.ShouldNotBeNull();

        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = $"filesystem.{readFileTool.Name}",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/test/integration-test.txt"
            }
        };

        var callResponseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
            mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));

        var callResponse = JsonConvert.DeserializeObject<MCPToolResponseEvent>(callResponseJson);

        // Assert
        discoverResponse.ShouldNotBeNull();
        discoverResponse.Tools.Count.ShouldBeGreaterThan(0);

        callResponse.ShouldNotBeNull();
        callResponse.Success.ShouldBeTrue();
        callResponse.Result.ShouldNotBeNull();

        _testOutputHelper.WriteLine($"Full workflow test succeeded: Discovered {discoverResponse.Tools.Count} tools, successfully called {readFileTool.Name}");
    }

    #endregion
}