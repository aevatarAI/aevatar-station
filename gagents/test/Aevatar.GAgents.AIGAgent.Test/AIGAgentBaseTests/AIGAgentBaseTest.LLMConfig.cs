using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.AIGAgentBaseTests;

public sealed class LLMConfigurationCentralizationTest : AevatarAIGAgentBaseTestBase
{
    [Fact]
    public async Task Should_NotStoreResolvedConfig_When_SystemLLMIsUsed()
    {
        // Arrange
        var systemLLMKey = "OpenAI"; // Use valid config from appsettings.json
        var chatAgent = await GAgentFactory.GetGAgentAsync<ITestChatAIGAgent>(Guid.NewGuid());

        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto { SystemLLM = systemLLMKey }
        });

        // Assert
        var state = await chatAgent.GetStateAsync();
        state.SystemLLM.ShouldBe(systemLLMKey);
        state.LLMConfigKey.ShouldBe(systemLLMKey); // Should be set by centralized approach
        state.LLM.ShouldBeNull(); // Should not store resolved config in new implementation
    }

    [Fact]
    public async Task Should_StoreSelfLLMConfig_When_SelfLLMConfigIsProvided()
    {
        // Arrange
        var selfConfig = new SelfLLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelId = ModelIdEnum.OpenAI,
            ModelName = "gpt-3.5-turbo",
            Endpoint = "https://api.openai.com",
            ApiKey = "user-provided-key"
        };

        var chatAgent = await GAgentFactory.GetGAgentAsync<ITestChatAIGAgent>(Guid.NewGuid());

        // Act
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto { SelfLLMConfig = selfConfig }
        });

        // Assert
        var state = await chatAgent.GetStateAsync();
        state.LLMConfigKey.ShouldBeNull();
        state.SystemLLM.ShouldBeNull();
        state.LLM.ShouldNotBeNull();
        state.LLM.ModelName.ShouldBe("gpt-3.5-turbo");
        state.LLM.ApiKey.ShouldBe("user-provided-key");
    }

    [Fact]
    public async Task Should_PreserveBackwardCompatibility_WithExistingStateFormat()
    {
        // Arrange
        var chatAgent = await GAgentFactory.GetGAgentAsync<ITestChatAIGAgent>(Guid.NewGuid());

        // Act - Initialize with existing pattern
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Assert - With centralized approach
        var state = await chatAgent.GetStateAsync();
        state.SystemLLM.ShouldBe("OpenAI");
        state.LLMConfigKey.ShouldBe("OpenAI"); // New centralized approach sets this
        state.LLM.ShouldBeNull(); // Resolved config not stored in centralized approach
    }

    [Fact]
    public async Task Should_HandleNonExistentSystemLLM_Gracefully()
    {
        // Arrange
        var nonExistentKey = "NonExistentConfig";
        var chatAgent = await GAgentFactory.GetGAgentAsync<ITestChatAIGAgent>(Guid.NewGuid());

        // Act & Assert - Should return false for invalid config
        var result = await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto { SystemLLM = nonExistentKey }
        });

        result.ShouldBe(false); // InitializeAsync returns false for invalid config
        
        var state = await chatAgent.GetStateAsync();
        state.SystemLLM.ShouldBeNull(); // Not set because initialization failed
        state.LLMConfigKey.ShouldBeNull(); // Not set because initialization failed
    }
}