using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatWithHistoryGAgent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Shouldly;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.Tests;

public sealed class AIChatTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly IBlobContainer _blobContainer;

    public AIChatTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _blobContainer = GetRequiredService<IBlobContainer>();
    }

    //[Fact]
    public async Task ImageTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a image reader",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var result =
            await chatAgent.ChatAsync("Obtain the content of the picture", new List<string> { "image1.png" });
        result.ShouldContain("Mock Content");
    }
    
    //[Fact]
    public async Task CancelStreamChatTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a image reader",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" },
            StreamingConfig = new StreamingConfig()
            {
                TimeOutInternal = 30000
            },
            StreamingModeEnabled = true
        });
        
        var cancelResult = await chatAgent.CancelChatAsync();
        cancelResult.ShouldBeFalse();
        
        var streamTask = Task.Run(async () =>
        {
            var result = await chatAgent.ChatAsync("test", aiChatContextDto: new AIChatContextDto());
            return result;
        });

        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(5000); 
            return await chatAgent.CancelChatAsync();
        });
        await Task.WhenAll(streamTask, cancelTask);
        cancelTask.Result.ShouldBeFalse();
        streamTask.Result.ShouldBe("MockStreamingContent");
        
        streamTask = Task.Run(async () =>
        {
            var result = await chatAgent.ChatAsync("test", aiChatContextDto: new AIChatContextDto());
            await Task.Delay(10000);
            return result;
        });

        cancelTask = Task.Run(async () =>
        {
            await Task.Delay(100); 
            return await chatAgent.CancelChatAsync();
        });
        await Task.WhenAll(streamTask, cancelTask);
        cancelTask.Result.ShouldBeTrue();
    }

    //[Fact]
    public async Task ImageStreamTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a image reader",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var result =
            await chatAgent.StreamChatAsync("Obtain the content of the picture",
                new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() }, new List<string> { "image1.png" });
        result.ShouldBe(true);
        var firstState = await chatAgent.GetStateAsync();
        firstState.ContentList.Count.ShouldBe(0);
        await Task.Delay(TimeSpan.FromSeconds(1));

        var secondState = await chatAgent.GetStateAsync();
        secondState.ContentList.Count.ShouldBeGreaterThan(0);
    }
}