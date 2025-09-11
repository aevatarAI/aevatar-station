using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatWithHistoryGAgent;
using Microsoft.Extensions.AI;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.Tests;

public sealed class AIStreamChatTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AIStreamChatTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task AIChatStreamAsyncTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var success =
            await chatAgent.StreamChatAsync("hello", new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        success.ShouldBe(true);
        var firstState = await chatAgent.GetStateAsync();
        firstState.ContentList.Count.ShouldBe(0);
        await Task.Delay(TimeSpan.FromSeconds(10));

        var secondState = await chatAgent.GetStateAsync();
        secondState.ContentList.Count.ShouldBeGreaterThan(0);
    }

    //[Fact]
    public async Task ChatWithHistoryAsyncTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatWithHistoryGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var success =
            await chatAgent.ChatWithStreamAsync("hello", new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        success.ShouldBe(true);
        var firstState = await chatAgent.GetStateAsync();
        firstState.ChatHistory.Count.ShouldBe(1);
        await Task.Delay(TimeSpan.FromSeconds(15));

        var secondState = await chatAgent.GetStateAsync();
        secondState.ChatHistory.Count.ShouldBe(2);
    }
}