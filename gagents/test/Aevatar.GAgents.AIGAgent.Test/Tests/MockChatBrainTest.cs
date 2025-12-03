// ABOUTME: This file tests the MockChatBrain implementation
// ABOUTME: Verifies mock behavior matches IChatBrain contract for unit testing

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

public class MockChatBrainTest
{
    //[Fact]
    public async Task Should_ReturnConfiguredResponse_When_InvokePromptAsync()
    {
        // Arrange
        var expectedResponse = new InvokePromptResponse
        {
            ChatReponseList = new List<ChatMessage>
            {
                new() { ChatRole = ChatRole.Assistant, Content = "Mock response" }
            },
            TokenUsageStatistics = new TokenUsageStatistics
            {
                InputToken = 10,
                OutputToken = 20,
                TotalUsageToken = 30,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        var mockBrain = new MockChatBrain();
        mockBrain.SetNextResponse(expectedResponse);

        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.InvokePromptAsync("Test prompt");

        // Assert
        result.ShouldNotBeNull();
        result.ChatReponseList.Count.ShouldBe(1);
        result.ChatReponseList[0].Content.ShouldBe("Mock response");
        result.ChatReponseList[0].ChatRole.ShouldBe(ChatRole.Assistant);
        result.TokenUsageStatistics.InputToken.ShouldBe(10);
        result.TokenUsageStatistics.OutputToken.ShouldBe(20);
        result.TokenUsageStatistics.TotalUsageToken.ShouldBe(30);
    }

    //[Fact]
    public async Task Should_ReturnDefaultResponse_When_NoResponseConfigured()
    {
        // Arrange
        var mockBrain = new MockChatBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.InvokePromptAsync("Test prompt");

        // Assert
        result.ShouldNotBeNull();
        result.ChatReponseList.Count.ShouldBe(1);
        result.ChatReponseList[0].Content.ShouldBe("Mock AI response");
        result.ChatReponseList[0].ChatRole.ShouldBe(ChatRole.Assistant);
        result.TokenUsageStatistics.ShouldNotBeNull();
    }

    //[Fact]
    public async Task Should_HandleStreamingRequest_When_InvokePromptStreamingAsync()
    {
        // Arrange
        var mockBrain = new MockChatBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var streamResult = await mockBrain.InvokePromptStreamingAsync("Test prompt");

        // Assert
        streamResult.ShouldNotBeNull();
        
        var messageList = new List<object>();
        await foreach (var item in streamResult)
        {
            messageList.Add(item);
        }
        
        messageList.Count.ShouldBeGreaterThan(0);
        
        var tokenUsage = mockBrain.GetStreamingTokenUsage(messageList);
        tokenUsage.ShouldNotBeNull();
        tokenUsage.TotalUsageToken.ShouldBeGreaterThan(0);
    }

    //[Fact]
    public async Task Should_StoreInitializationParameters_When_InitializeAsync()
    {
        // Arrange
        var mockBrain = new MockChatBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Google,
            ModelIdEnum = ModelIdEnum.Gemini
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id-123", "test description");

        // Assert
        mockBrain.ProviderEnum.ShouldBe(LLMProviderEnum.Google);
        mockBrain.ModelIdEnum.ShouldBe(ModelIdEnum.Gemini);
    }

    //[Fact]
    public async Task Should_SupportKnowledgeUpsert_When_UpsertKnowledgeAsync()
    {
        // Arrange
        var mockBrain = new MockChatBrain();
        var config = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI
        };

        // Act
        await mockBrain.InitializeAsync(config, "test-id", "test-description");
        var result = await mockBrain.UpsertKnowledgeAsync();

        // Assert
        result.ShouldBe(true);
    }
}