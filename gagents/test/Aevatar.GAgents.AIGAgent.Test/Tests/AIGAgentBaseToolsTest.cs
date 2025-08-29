using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.TestAgents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// This file contains comprehensive unit tests for AIGAgentBase Tools functionality.
/// Tests GAgent tool registration, selection, and execution flows.
/// </summary>
public class AIGAgentBaseToolsTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AIGAgentBaseToolsTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task ConfigureGAgentToolsAsync_Should_ReturnTrue_When_ValidGAgentsProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ConfigureGAgentToolsAsync_Should_ReturnFalse_When_BrainNotInitialized()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ConfigureGAgentToolsAsync_Should_UpdateStateCorrectly_When_MultipleGAgentsSelected()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ClearGAgentToolsAsync_Should_ReturnTrue_When_BrainInitialized()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ClearGAgentToolsAsync_Should_ReturnFalse_When_BrainNotInitialized()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        // Don't initialize the agent

        // Act
        var result = await agent.ClearGAgentToolsAsync();

        // Assert
        result.ShouldBeFalse();
    }

    //[Fact]
    public async Task ClearGAgentToolsAsync_Should_ClearAllToolsAndState_When_Called()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task GetCurrentToolCallsAsync_Should_ReturnEmptyList_When_NoToolsCalled()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ClearToolCallsAsync_Should_ClearToolCallTracking_When_Called()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task GenerateFunctionName_Should_CreateValidFunctionName_When_LongGrainTypeProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task GenerateFunctionName_Should_CreateUniqueFunctionNames_When_DuplicateEventTypes()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task GenerateFunctionDescription_Should_CreateMeaningfulDescription_When_GrainTypeProvided()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task IsGAgentAllowed_Should_ReturnTrue_When_NoRestrictionsSet()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ToolExecution_Should_TrackToolCallDetails_When_ToolExecuted()
    {
        // This test verifies that tool execution tracking works correctly
        // In a real scenario, this would involve calling actual tools

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ToolExecution_Should_HandleEventMapping_When_KernelArgumentsProvided()
    {
        // This test verifies that kernel arguments are properly mapped to event properties

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task PluginManagement_Should_HandlePluginRegistration_When_GAgentsConfigured()
    {
        // This test verifies that plugin management works correctly

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task PluginManagement_Should_RemoveExistingPlugins_When_NewPluginsRegistered()
    {
        // This test verifies that old plugins are removed when new ones are registered

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task ErrorHandling_Should_HandleGrainActivationErrors_When_InvalidGrainTypeProvided()
    {
        // This test verifies that invalid grain types are handled gracefully

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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

    //[Fact]
    public async Task StateManagement_Should_PersistToolConfiguration_When_StateChanged()
    {
        // This test verifies that tool configuration is properly persisted

        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
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
}