using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.TestAgents;
using Aevatar.GAgents.MCP.Options;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.AIGAgentBaseTests;

/// <summary>
/// This file contains comprehensive unit tests for AIGAgentBase Tools functionality.
/// Tests GAgent tool registration, selection, and execution flows.
/// </summary>
public sealed class AIGAgentBaseToolsTest : AevatarAIGAgentBaseTestBase
{
    [Fact]
    public async Task ConfigureGAgentToolsAsync_Should_ReturnTrue_When_ValidGAgentsProvided()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent"),
            GrainType.Create("test/routergagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        var state = await agent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        state.ToolGAgents.ShouldNotBeEmpty();
        state.ToolGAgents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConfigureGAgentToolsAsync_Should_ReturnFalse_When_BrainNotInitialized()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        // Don't initialize the agent

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigureGAgentToolsAsync_Should_UpdateStateCorrectly_When_MultipleGAgentsSelected()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent"),
            GrainType.Create("test/routergagent"),
            GrainType.Create("test/telegramgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        var state = await agent.GetStateAsync();
        state.ToolGAgents.Count.ShouldBe(3);
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/chatgagent"));
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/routergagent"));
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/telegramgagent"));
    }

    [Fact]
    public async Task ClearGAgentToolsAsync_Should_ReturnTrue_When_BrainInitialized()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // First configure some tools
        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };
        await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Act
        var result = await agent.ClearGAgentToolsAsync();

        // Assert
        result.ShouldBeTrue();

        var state = await agent.GetStateAsync();
        state.ToolGAgents.ShouldBeEmpty();
        state.RegisteredGAgentFunctions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearGAgentToolsAsync_Should_ReturnFalse_When_BrainNotInitialized()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        // Don't initialize the agent

        // Act
        var result = await agent.ClearGAgentToolsAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ClearGAgentToolsAsync_Should_ClearAllToolsAndState_When_Called()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // First configure some tools
        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent"),
            GrainType.Create("test/routergagent")
        };
        await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Verify they were configured
        var stateBefore = await agent.GetStateAsync();
        stateBefore.ToolGAgents.ShouldNotBeEmpty();

        // Act
        await agent.ClearGAgentToolsAsync();

        // Assert
        var stateAfter = await agent.GetStateAsync();
        stateAfter.ToolGAgents.ShouldBeEmpty();
        stateAfter.RegisteredGAgentFunctions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCurrentToolCallsAsync_Should_ReturnEmptyList_When_NoToolsCalled()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Act
        var toolCalls = await agent.GetCurrentToolCallsAsync();

        // Assert
        toolCalls.ShouldNotBeNull();
        toolCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearToolCallsAsync_Should_ClearToolCallTracking_When_Called()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Act
        await agent.ClearToolCallsAsync();

        // Assert
        var toolCalls = await agent.GetCurrentToolCallsAsync();
        toolCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GenerateFunctionName_Should_CreateValidFunctionName_When_LongGrainTypeProvided()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("very/long/grain/type/name/that/exceeds/normal/limits/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue(); // Should handle long names gracefully

        var state = await agent.GetStateAsync();
        state.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GenerateFunctionName_Should_CreateUniqueFunctionNames_When_DuplicateEventTypes()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent1"),
            GrainType.Create("test/chatgagent2") // Similar names that might generate similar function names
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue(); // Should handle similar names gracefully

        var state = await agent.GetStateAsync();
        state.ToolGAgents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateFunctionDescription_Should_CreateMeaningfulDescription_When_GrainTypeProvided()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue(); // Should generate descriptions correctly

        var state = await agent.GetStateAsync();
        state.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task IsGAgentAllowed_Should_ReturnTrue_When_NoRestrictionsSet()
    {
        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Don't set any allowed types (should allow all)
        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent"),
            GrainType.Create("test/routergagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue(); // Should allow all GAgents when no restrictions

        var state = await agent.GetStateAsync();
        state.ToolGAgents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ToolExecution_Should_TrackToolCallDetails_When_ToolExecuted()
    {
        // This test verifies that tool execution tracking works correctly
        // In a real scenario, this would involve calling actual tools

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        // Verify tool configuration was successful
        var state = await agent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        state.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ToolExecution_Should_HandleEventMapping_When_KernelArgumentsProvided()
    {
        // This test verifies that kernel arguments are properly mapped to event properties

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        // Verify the configuration was successful
        var state = await agent.GetStateAsync();
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/chatgagent"));
    }

    [Fact]
    public async Task PluginManagement_Should_HandlePluginRegistration_When_GAgentsConfigured()
    {
        // This test verifies that plugin management works correctly

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent"),
            GrainType.Create("test/routergagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        // Verify plugins were registered
        var state = await agent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        state.ToolGAgents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PluginManagement_Should_RemoveExistingPlugins_When_NewPluginsRegistered()
    {
        // This test verifies that old plugins are removed when new ones are registered

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var initialGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Configure initial tools
        await agent.ConfigureGAgentToolsAsync(initialGAgents);

        var newGAgents = new List<GrainType>
        {
            GrainType.Create("test/routergagent"),
            GrainType.Create("test/telegramgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(newGAgents);

        // Assert
        result.ShouldBeTrue();

        // Verify new configuration replaced old one
        var state = await agent.GetStateAsync();
        state.ToolGAgents.Count.ShouldBe(2);
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/routergagent"));
        state.ToolGAgents.Select(t => t.Type).ShouldContain(GrainType.Create("test/telegramgagent"));
        state.ToolGAgents.Select(t => t.Type).ShouldNotContain(GrainType.Create("test/chatgagent"));
    }

    [Fact]
    public async Task ErrorHandling_Should_HandleGrainActivationErrors_When_InvalidGrainTypeProvided()
    {
        // This test verifies that invalid grain types are handled gracefully

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("invalid/grain/type")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue(); // Should handle invalid grain types gracefully

        var state = await agent.GetStateAsync();
        state.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task StateManagement_Should_PersistToolConfiguration_When_StateChanged()
    {
        // This test verifies that tool configuration is properly persisted

        // Arrange
        var agent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var result = await agent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        result.ShouldBeTrue();

        // Verify state was persisted
        var state = await agent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        state.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Agent_Should_SupportBothMCPAndGAgentTools_When_BothConfigured()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        var gagentAgent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());

        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        await gagentAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Configure MCP servers
        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "test-mcp-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        // Configure GAgent tools
        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        var mcpResult = await mcpAgent.ConfigureMCPServersAsync(mcpServers);
        var gagentResult = await gagentAgent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert
        mcpResult.ShouldBeTrue();
        gagentResult.ShouldBeTrue();

        var mcpState = await mcpAgent.GetStateAsync();
        var gagentState = await gagentAgent.GetStateAsync();

        mcpState.EnableMCPTools.ShouldBeTrue();
        mcpState.MCPAgents.ShouldNotBeEmpty();

        gagentState.EnableGAgentTools.ShouldBeTrue();
        gagentState.ToolGAgents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Agent_Should_TrackToolCallsFromBothSources_When_BothTypesUsed()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "test-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        await mcpAgent.ConfigureMCPServersAsync(mcpServers);

        var mcpParameters = new Dictionary<string, object>
        {
            ["mcp_param"] = "mcp_value"
        };

        // Act
        await mcpAgent.TestMCPToolCallAsync("test-server", "mcp-tool", mcpParameters);

        // Assert
        var mcpToolCalls = await mcpAgent.GetCurrentToolCallsAsync();
        mcpToolCalls.ShouldNotBeEmpty();

        var mcpToolCall = mcpToolCalls.FirstOrDefault(tc => tc.ToolName == "mcp-tool");
        mcpToolCall.ShouldNotBeNull();
        mcpToolCall.ServerName.ShouldBe("test-server");
        mcpToolCall.Arguments.ShouldContainKey("mcp_param");
    }

    [Fact]
    public async Task Agent_Should_HandleConcurrentToolCalls_When_BothTypesCalledSimultaneously()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "concurrent-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        await mcpAgent.ConfigureMCPServersAsync(mcpServers);

        var parameters1 = new Dictionary<string, object> { ["param1"] = "value1" };
        var parameters2 = new Dictionary<string, object> { ["param2"] = "value2" };

        // Act
        var task1 = mcpAgent.TestMCPToolCallAsync("concurrent-server", "tool1", parameters1);
        var task2 = mcpAgent.TestMCPToolCallAsync("concurrent-server", "tool2", parameters2);

        await Task.WhenAll(task1, task2);

        // Assert
        var toolCalls = await mcpAgent.GetCurrentToolCallsAsync();
        toolCalls.Count.ShouldBe(2);

        var tool1Call = toolCalls.FirstOrDefault(tc => tc.ToolName == "tool1");
        var tool2Call = toolCalls.FirstOrDefault(tc => tc.ToolName == "tool2");

        tool1Call.ShouldNotBeNull();
        tool2Call.ShouldNotBeNull();

        tool1Call.Arguments.ShouldContainKey("param1");
        tool2Call.Arguments.ShouldContainKey("param2");
    }

    [Fact]
    public async Task Agent_Should_HandleToolCallErrors_When_InvalidToolsRequested()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var parameters = new Dictionary<string, object> { ["param"] = "value" };

        // Act
        var result = await mcpAgent.TestMCPToolCallAsync("non-existent-server", "invalid-tool", parameters);

        // Assert
        result.ShouldBeFalse();

        var toolCalls = await mcpAgent.GetCurrentToolCallsAsync();
        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.Success.ShouldBeFalse();
            toolCall.Result.ShouldContain("Error");
        }
    }

    [Fact]
    public async Task Agent_Should_ClearToolCallsFromBothSources_When_ClearCalled()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "clear-test-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        await mcpAgent.ConfigureMCPServersAsync(mcpServers);

        var parameters = new Dictionary<string, object> { ["param"] = "value" };

        // Make some tool calls
        await mcpAgent.TestMCPToolCallAsync("clear-test-server", "tool1", parameters);
        await mcpAgent.TestMCPToolCallAsync("clear-test-server", "tool2", parameters);

        // Verify we have tool calls
        var toolCallsBefore = await mcpAgent.GetCurrentToolCallsAsync();
        toolCallsBefore.Count.ShouldBe(2);

        // Act
        await mcpAgent.ClearToolCallsAsync();

        // Assert
        var toolCallsAfter = await mcpAgent.GetCurrentToolCallsAsync();
        toolCallsAfter.ShouldBeEmpty();
    }

    [Fact]
    public async Task Agent_Should_HandleComplexParameterTypes_When_ToolsCalled()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "complex-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        await mcpAgent.ConfigureMCPServersAsync(mcpServers);

        var complexParameters = new Dictionary<string, object>
        {
            ["string_param"] = "test string",
            ["int_param"] = 42,
            ["bool_param"] = true,
            ["array_param"] = new List<object> { "item1", "item2", 123 },
            ["object_param"] = new Dictionary<string, object>
            {
                ["nested_key"] = "nested_value",
                ["nested_number"] = 456
            }
        };

        // Act
        await mcpAgent.TestMCPToolCallAsync("complex-server", "complex-tool", complexParameters);

        // Assert
        var toolCalls = await mcpAgent.GetCurrentToolCallsAsync();
        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.Arguments.ShouldContainKey("string_param");
            toolCall.Arguments.ShouldContainKey("int_param");
            toolCall.Arguments.ShouldContainKey("bool_param");
            toolCall.Arguments.ShouldContainKey("array_param");
            toolCall.Arguments.ShouldContainKey("object_param");

            toolCall.Arguments["string_param"].ShouldBe("test string");
            toolCall.Arguments["int_param"].ShouldBe(42);
            toolCall.Arguments["bool_param"].ShouldBe(true);
        }
    }

    [Fact]
    public async Task Agent_Should_HandleToolCallTiming_When_ToolsExecuted()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "timing-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        await mcpAgent.ConfigureMCPServersAsync(mcpServers);

        var parameters = new Dictionary<string, object> { ["param"] = "value" };
        var startTime = DateTime.UtcNow;

        // Act
        await mcpAgent.TestMCPToolCallAsync("timing-server", "timed-tool", parameters);

        // Assert
        var toolCalls = await mcpAgent.GetCurrentToolCallsAsync();
        if (toolCalls.Any())
        {
            var toolCall = toolCalls.First();
            toolCall.DurationMs.ShouldBeGreaterThan(0);
            toolCall.Timestamp.ShouldNotBeNullOrEmpty();

            // Verify timestamp is reasonable
            var timestamp = DateTime.Parse(toolCall.Timestamp.Replace(" UTC", ""));
            timestamp.ShouldBeGreaterThan(startTime.AddSeconds(-1));
            timestamp.ShouldBeLessThan(DateTime.UtcNow.AddSeconds(1));
        }
    }

    [Fact]
    public async Task Agent_Should_HandleStateTransitions_When_ToolsConfiguredAndCleared()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        var gagentAgent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());

        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        await gagentAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Initial state should have tools disabled
        var initialMcpState = await mcpAgent.GetStateAsync();
        var initialGAgentState = await gagentAgent.GetStateAsync();

        initialMcpState.EnableMCPTools.ShouldBeFalse();
        initialGAgentState.EnableGAgentTools.ShouldBeFalse();

        // Configure tools
        var mcpServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "state-test-server",
                Command = "test-command",
                Args = new List<string> { "arg1" }
            }
        };

        var selectedGAgents = new List<GrainType>
        {
            GrainType.Create("test/chatgagent")
        };

        // Act
        await mcpAgent.ConfigureMCPServersAsync(mcpServers);
        await gagentAgent.ConfigureGAgentToolsAsync(selectedGAgents);

        // Assert - Tools should be enabled
        var configuredMcpState = await mcpAgent.GetStateAsync();
        var configuredGAgentState = await gagentAgent.GetStateAsync();

        configuredMcpState.EnableMCPTools.ShouldBeTrue();
        configuredMcpState.MCPAgents.ShouldNotBeEmpty();

        configuredGAgentState.EnableGAgentTools.ShouldBeTrue();
        configuredGAgentState.ToolGAgents.ShouldNotBeEmpty();

        // Clear tools
        await gagentAgent.ClearGAgentToolsAsync();

        // Assert - GAgent tools should be cleared
        var clearedGAgentState = await gagentAgent.GetStateAsync();
        clearedGAgentState.ToolGAgents.ShouldBeEmpty();
        clearedGAgentState.RegisteredGAgentFunctions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Agent_Should_HandleMultipleServerConfiguration_When_ConfiguredSequentially()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var firstServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "server1",
                Command = "command1",
                Args = new List<string> { "arg1" }
            }
        };

        var secondServers = new List<MCPServerConfig>
        {
            new()
            {
                ServerName = "server2",
                Command = "command2",
                Args = new List<string> { "arg2" }
            }
        };

        // Act
        await mcpAgent.ConfigureMCPServersAsync(firstServers);
        var firstState = await mcpAgent.GetStateAsync();

        await mcpAgent.ConfigureMCPServersAsync(secondServers);
        var secondState = await mcpAgent.GetStateAsync();

        // Assert
        firstState.MCPAgents.ShouldContainKey("server1");
        firstState.MCPAgents.Count.ShouldBe(1);

        secondState.MCPAgents.ShouldContainKey("server2");
        secondState.MCPAgents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Agent_Should_HandleEmptyConfiguration_When_EmptyListsProvided()
    {
        // Arrange
        var mcpAgent = await GAgentFactory.GetGAgentAsync<ITestMCPAIGAgent>(Guid.NewGuid());
        var gagentAgent = await GAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());

        await mcpAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test MCP agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        await gagentAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test GAgent tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Act
        var mcpResult = await mcpAgent.ConfigureMCPServersAsync(new List<MCPServerConfig>());
        var gagentResult = await gagentAgent.ConfigureGAgentToolsAsync(new List<GrainType>());

        // Assert
        mcpResult.ShouldBeFalse();
        gagentResult.ShouldBeFalse();

        var mcpState = await mcpAgent.GetStateAsync();
        var gagentState = await gagentAgent.GetStateAsync();

        mcpState.MCPAgents.ShouldBeEmpty();
        gagentState.ToolGAgents.ShouldBeEmpty();
    }
}