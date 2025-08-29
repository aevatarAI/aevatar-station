// ABOUTME: This file demonstrates using mock LLM services with actual AIGAgent tests
// ABOUTME: Shows how mocks enable reliable unit testing without external AI service dependencies

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class AIGAgentWithMocksTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly IBrainFactory _brainFactory;

    public AIGAgentWithMocksTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _brainFactory = GetRequiredService<IBrainFactory>();
    }

    //[Fact]
    public void Should_UseMockBrainFactory_When_TestModuleConfigured()
    {
        // Assert
        _brainFactory.ShouldNotBeNull();
        _brainFactory.ShouldBeOfType<MockBrainFactory>();
    }

    //[Fact]
    public async Task Should_InitializeSuccessfully_When_UsingMockBrain()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act
        var initResult = await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "You are a helpful assistant for testing",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });
        
        // Assert
        initResult.ShouldBe(true); // Mock should always succeed initialization
        
        var state = await chatAgent.GetStateAsync();
        state.SystemLLM.ShouldBe("OpenAI");
        state.LLMConfigKey.ShouldBe("OpenAI");
        state.PromptTemplate.ShouldBe("You are a helpful assistant for testing");
    }

    //[Fact]
    public async Task Should_ReturnMockResponse_When_PromptChatAsync()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "You are a test assistant",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        // Act
        var success = await chatAgent.PromptChatAsync("Hello, test!", new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        
        // Assert
        success.ShouldBe(true);
        
        // Wait briefly for mock processing
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        
        var state = await chatAgent.GetStateAsync();
        state.ContentList.Count.ShouldBeGreaterThan(0);
        
        // Verify mock response characteristics
        var lastContent = state.ContentList[^1];
        lastContent.ResponseContent.ShouldBe("Mock AI response"); // Default mock response
    }

    //[Fact]
    public async Task Should_ConfigureMockResponse_When_CustomResponseSet()
    {
        // Arrange
        var mockFactory = (MockBrainFactory)_brainFactory;
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "You are a test assistant",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        // Configure mock response through the factory
        // Use Azure provider to match the centralized config for "OpenAI" key
        var mockBrain = mockFactory.GetChatBrain(new AI.Options.LLMProviderConfig 
        { 
            ProviderEnum = AI.Options.LLMProviderEnum.Azure, 
            ModelIdEnum = AI.Options.ModelIdEnum.OpenAI 
        }) as MockChatBrain;
        
        mockBrain!.SetNextResponse(new InvokePromptResponse
        {
            ChatReponseList = new List<ChatMessage>
            {
                new() { ChatRole = ChatRole.Assistant, Content = "Custom test response" }
            },
            TokenUsageStatistics = new TokenUsageStatistics
            {
                InputToken = 5,
                OutputToken = 10,
                TotalUsageToken = 15,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        });

        // Act
        var success = await chatAgent.PromptChatAsync("Test prompt", new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        
        // Assert
        success.ShouldBe(true);
        
        // Poll for state update with timeout
        var timeout = TimeSpan.FromSeconds(10);
        var pollingInterval = TimeSpan.FromMilliseconds(100);
        var start = DateTime.UtcNow;
        
        ChatAIGStateBase state;
        do
        {
            await Task.Delay(pollingInterval);
            state = await chatAgent.GetStateAsync();
            if (state.ContentList.Count > 0)
                break;
        }
        while (DateTime.UtcNow - start < timeout);
        
        state.ContentList.Count.ShouldBeGreaterThan(0);
    }

    //[Fact]
    public async Task Should_HandleMultipleProviders_When_DifferentConfigurations()
    {
        // Arrange
        var testConfigs = new[]
        {
            new { SystemLLM = "OpenAI", Expected = AI.Options.LLMProviderEnum.Azure }, // OpenAI key maps to Azure provider in centralized config
            new { SystemLLM = "Azure", Expected = AI.Options.LLMProviderEnum.Azure },
            new { SystemLLM = "Google", Expected = AI.Options.LLMProviderEnum.Google }
        };

        foreach (var config in testConfigs)
        {
            var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
            
            // Act
            var initResult = await chatAgent.InitializeAsync(new InitializeDto()
            {
                Instructions = $"Test with {config.SystemLLM}",
                LLMConfig = new LLMConfigDto() { SystemLLM = config.SystemLLM }
            });
            
            // Assert
            initResult.ShouldBe(true);
            
            var state = await chatAgent.GetStateAsync();
            state.SystemLLM.ShouldBe(config.SystemLLM);
            state.LLMConfigKey.ShouldBe(config.SystemLLM);
        }
    }
}