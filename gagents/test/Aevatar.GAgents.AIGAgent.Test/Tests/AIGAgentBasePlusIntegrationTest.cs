using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Basic;
using Orleans;
using Orleans.Providers;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Integration tests for AIGAgentBasePlus using AevatarAIGAgentTestBase.
/// These tests use the full Orleans TestCluster infrastructure to test methods
/// that require Orleans Grain functionality (State, event sourcing, etc.)
/// </summary>
[Collection(ClusterCollection.Name)]
public class AIGAgentBasePlusIntegrationTest : AevatarAIGAgentTestBase
{
    private readonly IGrainFactory _grainFactory;

    public AIGAgentBasePlusIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region TriggerMigrationAsync Integration Tests

    [Fact]
    public async Task TriggerMigrationAsync_WhenLegacySystemLLMExists_ShouldMigrateToLLMConfigKey()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        
        // First, clear any existing LLMConfigKey to simulate legacy state
        // (grain might be auto-initialized with default config)
        await agent.SetLLMConfigKeyAsync(""); // Clear LLMConfigKey
        
        // Now set only SystemLLM to simulate legacy configuration format
        await agent.SetSystemLLMAsync("DeepSeek");
        
        // Verify pre-migration state: SystemLLM set, LLMConfigKey empty
        var preState = await agent.GetStateAsync();
        preState.SystemLLM.ShouldBe("DeepSeek");
        preState.LLMConfigKey.ShouldBeNullOrEmpty();
        
        // Act - Trigger migration to move SystemLLM to LLMConfigKey
        await agent.TriggerMigrationAsync();
        
        // Assert - After migration, LLMConfigKey should be set to the SystemLLM value
        var postState = await agent.GetStateAsync();
        postState.LLMConfigKey.ShouldBe("DeepSeek"); // Migrated from SystemLLM
        postState.SystemLLM.ShouldBe("DeepSeek"); // Preserved for backward compatibility
    }

    [Fact]
    public async Task TriggerMigrationAsync_WhenNoLegacyConfig_ShouldNotModifyState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        
        // Setup with already migrated configuration using public API
        // Use "Google" which exists in the test configuration
        await agent.SetLLMConfigKeyAsync("Google");
        
        // Act - Trigger migration when already migrated (should be no-op)
        await agent.TriggerMigrationAsync();
        
        // Assert - State should remain unchanged
        var state = await agent.GetStateAsync();
        state.LLMConfigKey.ShouldBe("Google");
        state.SystemLLM.ShouldBe("Google");
    }

    [Fact]
    public async Task TriggerMigrationAsync_WithEmptySystemLLM_ShouldNotPerformMigration()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        
        // Explicitly clear both fields to ensure clean state
        await agent.SetLLMConfigKeyAsync(""); // Clear LLMConfigKey
        await agent.SetSystemLLMAsync("");     // Clear SystemLLM
        
        // Verify initial state is empty
        var initialState = await agent.GetStateAsync();
        initialState.SystemLLM.ShouldBeNullOrEmpty();
        initialState.LLMConfigKey.ShouldBeNullOrEmpty();
        
        // Act - Trigger migration when no legacy config exists
        await agent.TriggerMigrationAsync();
        
        // Assert - State should remain empty (migration should not happen)
        var finalState = await agent.GetStateAsync();
        finalState.SystemLLM.ShouldBeNullOrEmpty();
        finalState.LLMConfigKey.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task TriggerMigrationAsync_MultipleInvocations_ShouldBeIdempotent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        
        // Use "Azure" which exists in the test configuration
        await agent.SetSystemLLMAsync("Azure");
        
        // Get state after first migration
        await agent.TriggerMigrationAsync();
        var firstState = await agent.GetStateAsync();
        
        // Act - Call migration additional times
        await agent.TriggerMigrationAsync();
        await agent.TriggerMigrationAsync();
        
        // Assert - Should result in same state (idempotent)
        var finalState = await agent.GetStateAsync();
        finalState.LLMConfigKey.ShouldBe(firstState.LLMConfigKey);
        finalState.SystemLLM.ShouldBe(firstState.SystemLLM);
    }

    #endregion

    #region GetLLMConfigAsync Integration Tests

    [Fact]
    public async Task GetLLMConfigAsync_Should_ReturnSystemConfig_When_LLMConfigKeyIsSet()
    {
        // Arrange - Use existing configuration from appsettings.json
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        var systemLLMKey = "OpenAI"; // This exists in test configuration

        // Act - Set LLMConfigKey using proper event sourcing
        await agent.SetLLMConfigKeyAsync(systemLLMKey);

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gpt-4o"); // From appsettings.json
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
    }

    [Fact]
    public async Task GetLLMConfigAsync_Should_ReturnCorrectConfig_When_LLMConfigKeyIsDeepSeek()
    {
        // Arrange - Use existing configuration from appsettings.json
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);
        var systemLLMKey = "DeepSeek"; // This exists in test configuration

        // Act - Set LLMConfigKey using proper event sourcing
        await agent.SetLLMConfigKeyAsync(systemLLMKey);

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("DeepSeek-R1"); // From appsettings.json
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
    }

    [Fact]
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

        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);

        await agent.SetLLMConfigKeyAsync("");

        // Act - Initialize with self-provided config (sets LLM property directly)
        await agent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SelfLLMConfig = selfConfig,
                SystemLLM = ""
            }
        });

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gemini-pro");
        resolvedConfig.ApiKey.ShouldBe("google-key");
        resolvedConfig.ProviderEnum.ShouldBe(LLMProviderEnum.Google);
    }

    [Fact]
    public async Task GetLLMConfigAsync_Should_ReturnNull_When_SystemConfigNotFound()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);

        // Act - Set non-existent system LLM key using proper event sourcing
        await agent.SetLLMConfigKeyAsync("NonExistentConfig");

        var resolvedConfig = await agent.GetLLMConfigAsync();

        // Assert
        resolvedConfig.ShouldBeNull();
    }

    [Fact]
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

        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestAIGAgentBasePlus>(agentId);

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

    #endregion
}

/// <summary>
/// Test interface for AIGAgentBasePlus integration testing
/// Combines IStateGAgentPlus (for state management) and IAIGAgent (for AI-specific methods)
/// </summary>
public interface ITestAIGAgentBasePlus : IStateGAgentPlus<TestAIGAgentBasePlusState>, IAIGAgent
{
}

/// <summary>
/// Test implementation of AIGAgentBasePlus for integration testing
/// </summary>
[GAgent("test-aigagent-plus", "test")]
public class TestAIGAgentBasePlus : AIGAgentBasePlus<TestAIGAgentBasePlusState, TestAIGAgentBasePlusStateLogEvent>, ITestAIGAgentBasePlus
{
    public override Task<string> GetDescriptionAsync() 
        => Task.FromResult("Test AIGAgentBasePlus for integration testing");
}

/// <summary>
/// Test state for AIGAgentBasePlus integration testing
/// </summary>
[GenerateSerializer]
public class TestAIGAgentBasePlusState : AIGAgentStateBasePlus
{
}

/// <summary>
/// Test state log events
/// </summary>
[GenerateSerializer]
public class TestAIGAgentBasePlusStateLogEvent : StateLogEventBase<TestAIGAgentBasePlusStateLogEvent>
{
}
