using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.Tests;

public class AIHttpAsyncChatTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AIHttpAsyncChatTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task AIHttpChatAsyncTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var success =
            await chatAgent.PromptChatAsync("hello", new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        success.ShouldBe(true);
        var firstState = await chatAgent.GetStateAsync();
        firstState.ContentList.Count.ShouldBe(0);
        await Task.Delay(TimeSpan.FromSeconds(10));

        var secondState = await chatAgent.GetStateAsync();
        secondState.ContentList.Count.ShouldBeGreaterThan(0);
    }
}