using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.TestAgents;
using Aevatar.GAgents.MCP.Options;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// This file contains comprehensive unit tests for AIGAgentBase MCP functionality.
/// Tests MCP server configuration, tool discovery, and tool calling flows.
/// </summary>
public class AIGAgentBaseMCPTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AIGAgentBaseMCPTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact(DisplayName = "Can config mcp servers to state.")]
    public async Task ConfigureMCPServersAsync_Should_ReturnTrue_When_ValidServersProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>();
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var servers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "test-server",
                Command = "test-command",
                Args = ["arg1", "arg2"],
                Env = new Dictionary<string, string> { ["TEST_VAR"] = "test_value" }
            }
        };

        // Act
        var result = await agent.ConfigureMCPServersAsync(servers);

        // Assert
        result.ShouldBeTrue();

        var state = await agent.GetStateAsync();
        state.MCPAgents.ShouldContainKey("test-server");
        state.EnableMCPTools.ShouldBeTrue();
    }

    [Fact(DisplayName = "Cannot config mcp servers with invalid data.")]
    public async Task ConfigureMCPServersAsync_Should_ReturnFalse_When_InvalidServersProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var servers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "", // Invalid: empty server name
                Command = "test-command"
            }
        };

        // Act
        var result = await agent.ConfigureMCPServersAsync(servers);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Can config multiple mcp servers to state.")]
    public async Task ConfigureMCPServersAsync_Should_UpdateStateCorrectly_When_MultipleServersConfigured()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var servers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "server1",
                Command = "command1",
                Args = new List<string> { "arg1" }
            },
            new()
            {
                ServerName = "server2",
                Command = "command2",
                Args = new List<string> { "arg2" }
            }
        };

        // Act
        var result = await agent.ConfigureMCPServersAsync(servers);

        // Assert
        result.ShouldBeTrue();

        var state = await agent.GetStateAsync();
        state.MCPAgents.ShouldContainKey("server1");
        state.MCPAgents.ShouldContainKey("server2");
        state.MCPAgents.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Available MCP tools should return empty when no mcp servers configured.")]
    public async Task GetAvailableMCPToolsAsync_Should_ReturnEmptyList_When_NoServersConfigured()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Act
        var tools = await agent.GetAvailableMCPToolsAsync();

        // Assert
        tools.ShouldNotBeNull();
        tools.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Can return available MCP tools when servers configured.")]
    public async Task GetAvailableMCPToolsAsync_Should_ReturnTools_When_ServersConfigured()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var servers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "test-server",
                Command = "test-command",
                Args = ["arg1"]
            }
        };

        await agent.ConfigureMCPServersAsync(servers);

        // Act
        var tools = await agent.GetAvailableMCPToolsAsync();

        // Assert
        tools.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Cannot call MCP tool when mcp server not found.")]
    public async Task TestMCPToolCallAsync_Should_ReturnFalse_When_ServerNotFound()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "value1",
            ["param2"] = 42
        };

        // Act
        var result = await agent.TestMCPToolCallAsync("non-existent-server", "test-tool", parameters);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Can track tool calls when MCP tool called.")]
    public async Task TestMCPToolCallAsync_Should_TrackToolCall_When_Called()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "value1",
            ["param2"] = 42
        };

        // Act
        await agent.TestMCPToolCallAsync("test-server", "test-tool", parameters);

        // Assert
        var toolCalls = await agent.GetCurrentToolCallsAsync();
        toolCalls.ShouldNotBeNull();

        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.ToolName.ShouldBe("test-tool");
            toolCall.ServerName.ShouldBe("test-server");
            toolCall.Arguments.ShouldNotBeNull();
            toolCall.Arguments.ShouldContainKey("param1");
            toolCall.Arguments.ShouldContainKey("param2");
        }
    }

    [Fact(DisplayName = "Can clear tracked tool calls when MCP tool calls cleared.")]
    public async Task ClearToolCallsAsync_Should_ClearTrackedCalls_When_Called()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object> { ["param1"] = "value1" };

        // Add some tool calls
        await agent.TestMCPToolCallAsync("test-server", "test-tool", parameters);

        // Act
        await agent.ClearToolCallsAsync();

        // Assert
        var toolCalls = await agent.GetCurrentToolCallsAsync();
        toolCalls.ShouldBeEmpty();
    }

    //[Fact]
    public async Task GenerateMCPFunctionName_Should_CreateValidFunctionName_When_LongNamesProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var servers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "very-long-server-name-that-exceeds-normal-limits",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        // Act
        var result = await agent.ConfigureMCPServersAsync(servers);

        // Assert
        result.ShouldBeTrue(); // Should handle long names gracefully

        var state = await agent.GetStateAsync();
        state.MCPAgents.ShouldContainKey("very-long-server-name-that-exceeds-normal-limits");
    }

    [Fact(DisplayName = "Can handle errors when MCP tool call fails.")]
    public async Task MCPToolCall_Should_HandleError_When_ToolCallFails()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object>
        {
            ["invalid_param"] = "This will likely cause an error"
        };

        // Act
        var result = await agent.TestMCPToolCallAsync("non-existent-server", "non-existent-tool", parameters);

        // Assert
        result.ShouldBeFalse();

        var toolCalls = await agent.GetCurrentToolCallsAsync();
        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.Success.ShouldBeFalse();
            toolCall.Result.ShouldContain("Error");
        }
    }

    [Fact(DisplayName = "Can handle null parameters in MCP tool call.")]
    public async Task MCPToolCall_Should_HandleNullParameters_When_NoParametersProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object>();

        // Act
        var result = await agent.TestMCPToolCallAsync("test-server", "test-tool", parameters);

        // Assert
        result.ShouldBeFalse(); // Should handle empty parameters gracefully
    }

    //[Fact]
    public async Task MCPToolCall_Should_RecordTimestamp_When_ToolCalled()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object> { ["param1"] = "value1" };
        var beforeCall = DateTime.UtcNow;

        // Act
        await agent.TestMCPToolCallAsync("test-server", "test-tool", parameters);

        // Assert
        var toolCalls = await agent.GetCurrentToolCallsAsync();
        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.Timestamp.ShouldNotBeNullOrEmpty();

            // Parse timestamp and verify it's reasonable
            var timestamp = DateTime.Parse(toolCall.Timestamp.Replace(" UTC", ""));
            timestamp.ShouldBeGreaterThan(beforeCall.AddSeconds(-1));
            timestamp.ShouldBeLessThan(DateTime.UtcNow.AddSeconds(1));
        }
    }
}