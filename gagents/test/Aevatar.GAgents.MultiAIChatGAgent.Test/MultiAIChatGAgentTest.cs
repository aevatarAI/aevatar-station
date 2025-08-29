using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents;
using Aevatar.GAgents.MultiAIChatGAgent.Test.GAgents;
using Shouldly;

namespace Aevatar.GAgents.MultiAIChatGAgent.Test;

public class MultiAIChatGAgentTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    
    public MultiAIChatGAgentTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }
    
    [Fact]
    public async Task ChatAsync_Test()
    {
        var multiAiChatGAgent = await _agentFactory.GetGAgentAsync<IMultiAIChatGAgentTest>(Guid.NewGuid());
        await multiAiChatGAgent.ConfigAsync(new MultiAIChatConfig
        {
            Instructions = "you are a nba player",
            MaxHistoryCount = 32,
            StreamingModeEnabled = true,
            StreamingConfig = new StreamingConfig
            {
                BufferingSize = 32
            },
            LLMConfigs = new List<LLMConfigDto>()
            {
                new LLMConfigDto
                {
                    SystemLLM = "OpenAI"
                },
                new LLMConfigDto
                {
                    SystemLLM = "DeepSeek"
                }
            },
            RequestRecoveryDelay = TimeSpan.FromSeconds(60)
        });

        var sessionId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var chatMessage = await multiAiChatGAgent.ChatAsync("Who are you",  null, new AIChatContextDto
        {
            RequestId = sessionId,
            ChatId = chatId.ToString()
        });
        chatMessage.ShouldNotBeEmpty();
        chatMessage.Count.ShouldBe(1);
        await Task.Delay(TimeSpan.FromSeconds(10));

        chatMessage = await multiAiChatGAgent.GetChatMessageAsync();
        chatMessage.ShouldNotBeNull();
        chatMessage.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task ChatWithStreamingAsync_Test()
    {
        var multiAiChatGAgent = await _agentFactory.GetGAgentAsync<IMultiAIChatGAgentTest>(Guid.NewGuid());
        await multiAiChatGAgent.ConfigAsync(new MultiAIChatConfig
        {
            Instructions = "you are a nba player",
            MaxHistoryCount = 32,
            StreamingModeEnabled = true,
            StreamingConfig = new StreamingConfig
            {
                BufferingSize = 32
            },
            LLMConfigs = new List<LLMConfigDto>()
            {
                new LLMConfigDto
                {
                    SystemLLM = "OpenAI"
                },
                new LLMConfigDto
                {
                    SystemLLM = "DeepSeek"
                }
            },
            RequestRecoveryDelay = TimeSpan.FromSeconds(60)
        });

        var sessionId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var chatMessage = await multiAiChatGAgent.ChatWithStreamingAsync("Who are you",  null, new AIChatContextDto
        {
            RequestId = sessionId,
            ChatId = chatId.ToString()
        });
        chatMessage.ShouldBeEmpty();
    }
}