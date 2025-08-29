// ABOUTME: This file tests the MockTextToImageBrain implementation
// ABOUTME: Verifies mock behavior matches ITextToImageBrain contract for unit testing

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class MockTextToImageBrainTest
{
    //[Fact]
    public async Task Should_ReturnConfiguredResponse_When_GenerateTextToImageAsync()
    {
        // Arrange
        var expectedResponse = new List<TextToImageResponse>
        {
            new()
            {
                ResponseType = TextToImageResponseType.Base64Content,
                Base64Content = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==",
                Url = "",
                ImageType = "png"
            }
        };

        var mockBrain = new MockTextToImageBrain();
        mockBrain.SetNextResponse(expectedResponse);

        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        var option = new TextToImageOption
        {
            ModelId = "dall-e-3",
            With = 1024,
            Height = 1024,
            Count = 1,
            StyleEnum = TextToImageStyleEnum.Vivid,
            QualityEnum = TextToImageQualityEnum.HD,
            ResponseType = TextToImageResponseType.Base64Content
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.GenerateTextToImageAsync("Generate a cat", option);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ResponseType.ShouldBe(TextToImageResponseType.Base64Content);
        result[0].Base64Content.ShouldBe("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        result[0].ImageType.ShouldBe("png");
    }

    //[Fact]
    public async Task Should_ReturnDefaultResponse_When_NoResponseConfigured()
    {
        // Arrange
        var mockBrain = new MockTextToImageBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        var option = new TextToImageOption
        {
            ModelId = "dall-e-3",
            Count = 2
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.GenerateTextToImageAsync("Generate a dog", option);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2); // Should respect the count from option
        result[0].ResponseType.ShouldBe(TextToImageResponseType.Base64Content);
        result[0].Base64Content.ShouldNotBeNullOrEmpty();
        result[0].ImageType.ShouldBe("png");
    }

    //[Fact]
    public async Task Should_StoreInitializationParameters_When_InitializeAsync()
    {
        // Arrange
        var mockBrain = new MockTextToImageBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id-456", "image generation test");

        // Assert
        mockBrain.ProviderEnum.ShouldBe(LLMProviderEnum.OpenAI);
        mockBrain.ModelIdEnum.ShouldBe(ModelIdEnum.OpenAITextToImage);
    }

    //[Fact]
    public async Task Should_SupportKnowledgeUpsert_When_UpsertKnowledgeAsync()
    {
        // Arrange
        var mockBrain = new MockTextToImageBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.UpsertKnowledgeAsync();

        // Assert
        result.ShouldBe(true);
    }

    //[Fact]
    public async Task Should_HandleCancellationToken_When_GenerateTextToImageAsync()
    {
        // Arrange
        var mockBrain = new MockTextToImageBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        var option = new TextToImageOption
        {
            ModelId = "dall-e-3",
            Count = 1
        };

        using var cts = new CancellationTokenSource();

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.GenerateTextToImageAsync("Generate an image", option, cts.Token);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
    }

    //[Fact]
    public async Task Should_RespectResponseType_When_ConfiguredForUrl()
    {
        // Arrange
        var expectedResponse = new List<TextToImageResponse>
        {
            new()
            {
                ResponseType = TextToImageResponseType.Url,
                Url = "https://example.com/image.png",
                Base64Content = "",
                ImageType = "png"
            }
        };

        var mockBrain = new MockTextToImageBrain();
        mockBrain.SetNextResponse(expectedResponse);

        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAITextToImage
        };

        var option = new TextToImageOption
        {
            ResponseType = TextToImageResponseType.Url
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.GenerateTextToImageAsync("Generate image", option);

        // Assert
        result.ShouldNotBeNull();
        result[0].ResponseType.ShouldBe(TextToImageResponseType.Url);
        result[0].Url.ShouldBe("https://example.com/image.png");
        result[0].Base64Content.ShouldBe("");
    }
}