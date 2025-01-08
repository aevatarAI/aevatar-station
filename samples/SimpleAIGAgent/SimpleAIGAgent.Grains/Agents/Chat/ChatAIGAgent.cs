using Aevatar.AI.Agent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace SimpleAIGAgent.Grains.Agents.Chat;

public class ChatAIGAgent : AIGAgentBase<ChatAIGState, ChatAIEvent>
{
    public ChatAIGAgent(ILogger logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for chatting with user.");
    }
    
    [EventHandler]
    public Task OnChatAIEvent(ChatAIEvent chatAIEvent)
    {
        var result = await InvokePromptAsync(chatAIEvent.)
    }
}