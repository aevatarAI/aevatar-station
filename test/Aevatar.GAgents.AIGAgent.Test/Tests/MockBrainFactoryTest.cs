// ABOUTME: This file tests the MockBrainFactory implementation
// ABOUTME: Verifies mock factory behavior matches IBrainFactory contract for unit testing

using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class MockBrainFactoryTest
{
    //[Fact]
    public void Should_CreateChatBrain_When_GetChatBrain()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var config = new LLMProviderConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI
        };

        // Act
        var brain = factory.GetChatBrain(config);

        // Assert
        brain.ShouldNotBeNull();
        brain.ShouldBeOfType<MockChatBrain>();
        brain.ProviderEnum.ShouldBe(LLMProviderEnum.OpenAI);
        brain.ModelIdEnum.ShouldBe(ModelIdEnum.OpenAI);
    }

    //[Fact]
    public void Should_CreateTextToImageBrain_When_GetTextToImageBrain()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var config = new LLMProviderConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        // Act
        var brain = factory.GetTextToImageBrain(config);

        // Assert
        brain.ShouldNotBeNull();
        brain.ShouldBeOfType<MockTextToImageBrain>();
        brain.ProviderEnum.ShouldBe(LLMProviderEnum.Azure);
        brain.ModelIdEnum.ShouldBe(ModelIdEnum.OpenAITextToImage);
    }

    //[Fact]
    public void Should_CreateBrain_When_CreateBrain()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var config = new LLMProviderConfig
        {
            ProviderEnum = LLMProviderEnum.Google,
            ModelIdEnum = ModelIdEnum.Gemini
        };

        // Act
        var brain = factory.CreateBrain(config);

        // Assert
        brain.ShouldNotBeNull();
        brain.ShouldBeOfType<MockChatBrain>();
        brain.ProviderEnum.ShouldBe(LLMProviderEnum.Google);
        brain.ModelIdEnum.ShouldBe(ModelIdEnum.Gemini);
    }

    //[Fact]
    public void Should_ReturnSameBrainType_When_CalledMultipleTimes()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var config = new LLMProviderConfig
        {
            ProviderEnum = LLMProviderEnum.DeepSeek,
            ModelIdEnum = ModelIdEnum.DeepSeek
        };

        // Act
        var brain1 = factory.GetChatBrain(config);
        var brain2 = factory.GetChatBrain(config);

        // Assert
        brain1.ShouldNotBeNull();
        brain2.ShouldNotBeNull();
        brain1.ShouldBeOfType<MockChatBrain>();
        brain2.ShouldBeOfType<MockChatBrain>();
        brain1.ShouldBeSameAs(brain2); // Should be cached instances for shared state
    }

    //[Fact]
    public void Should_HandleDifferentProviders_When_GetChatBrain()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var configs = new[]
        {
            new LLMProviderConfig { ProviderEnum = LLMProviderEnum.OpenAI, ModelIdEnum = ModelIdEnum.OpenAI },
            new LLMProviderConfig { ProviderEnum = LLMProviderEnum.Azure, ModelIdEnum = ModelIdEnum.OpenAI },
            new LLMProviderConfig { ProviderEnum = LLMProviderEnum.Google, ModelIdEnum = ModelIdEnum.Gemini },
            new LLMProviderConfig { ProviderEnum = LLMProviderEnum.DeepSeek, ModelIdEnum = ModelIdEnum.DeepSeek }
        };

        // Act & Assert
        foreach (var config in configs)
        {
            var brain = factory.GetChatBrain(config);
            brain.ShouldNotBeNull();
            brain.ShouldBeOfType<MockChatBrain>();
            brain.ProviderEnum.ShouldBe(config.ProviderEnum);
            brain.ModelIdEnum.ShouldBe(config.ModelIdEnum);
        }
    }

    //[Fact]
    public void Should_HandleTextToImageModels_When_GetTextToImageBrain()
    {
        // Arrange
        var factory = new MockBrainFactory();
        var config = new LLMProviderConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        // Act
        var brain = factory.GetTextToImageBrain(config);

        // Assert
        brain.ShouldNotBeNull();
        brain.ShouldBeOfType<MockTextToImageBrain>();
        brain.ProviderEnum.ShouldBe(LLMProviderEnum.OpenAI);
        brain.ModelIdEnum.ShouldBe(ModelIdEnum.OpenAITextToImage);
    }
}