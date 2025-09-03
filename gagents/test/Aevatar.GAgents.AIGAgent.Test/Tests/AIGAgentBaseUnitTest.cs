// ABOUTME: This file contains unit tests for AIGAgentBase centralized configuration functionality
// ABOUTME: Tests the new LLM configuration resolution methods and state transition logic

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class AIGAgentBaseUnitTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AIGAgentBaseUnitTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task GetLLMConfigAsync_Should_ReturnSystemConfig_When_LLMConfigKeyIsSet()
    {
        // Arrange - Use existing configuration from appsettings.json
        var systemLLMKey = "OpenAI"; // This exists in test configuration
        
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Act - Set LLMConfigKey using proper event sourcing
        await agent.SetLLMConfigKeyAsync(systemLLMKey);

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gpt-4o"); // From appsettings.json
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
    }

    //[Fact]
    public async Task GetLLMConfigAsync_Should_FallbackToSystemLLM_When_LLMConfigKeyIsNull()
    {
        // Arrange - Use existing configuration from appsettings.json
        var systemLLMKey = "DeepSeek"; // This exists in test configuration
        
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Act - Set SystemLLM without triggering brain initialization
        // We'll use the new SetSystemLLMAsync method I'll add to avoid brain initialization
        await agent.SetSystemLLMAsync(systemLLMKey);

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("DeepSeek-R1"); // From appsettings.json
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
    }

    //[Fact]
    public async Task GetLLMConfigAsync_Should_FallbackToResolvedLLM_When_BothKeysAreNull()
    {
        // Arrange - Use self-provided LLM config to test fallback
        var selfConfig = new SelfLLMConfig
        {
            ProviderEnum = LLMProviderEnum.Google,
            ModelId = ModelIdEnum.Gemini,
            ModelName = "gemini-pro",
            Endpoint = "https://ai.google.dev",
            ApiKey = "google-key"
        };

        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Act - Initialize with self-provided config (sets LLM property directly)
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto { SelfLLMConfig = selfConfig }
        });

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gemini-pro");
        resolvedConfig.ApiKey.ShouldBe("google-key");
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Google);
    }

    //[Fact]
    public async Task GetLLMConfigAsync_Should_ReturnNull_When_SystemConfigNotFound()
    {
        // Arrange
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Act - Set non-existent system LLM key using proper event sourcing
        await agent.SetLLMConfigKeyAsync("NonExistentConfig");

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldBeNull();
    }

    //[Fact]
    public async Task GetLLMConfigAsync_Should_UsePriorityOrder_When_MultipleConfigsSet()
    {
        // Arrange - Test priority: LLMConfigKey (OpenAI) over SystemLLM (DeepSeek) over LLM (self-config)
        var fallbackConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Google,
            ModelIdEnum = ModelIdEnum.Gemini,
            ModelName = "priority-3-config",
            Endpoint = "https://priority3.com",
            ApiKey = "priority-3-key"
        };

        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Act - Set up multiple configs using non-brain-initializing methods
        // 1. First set LLM config (Priority 3 - lowest) 
        await agent.SetLLMAsync(fallbackConfig, null);

        // 2. Then set SystemLLM (Priority 2 - middle) 
        await agent.SetSystemLLMAsync("DeepSeek");

        // 3. Finally set LLMConfigKey (Priority 1 - highest)
        await agent.SetLLMConfigKeyAsync("OpenAI");

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert - Should return Priority 1 (LLMConfigKey = OpenAI)
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gpt-4o"); // From OpenAI config in appsettings.json
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
    }
}