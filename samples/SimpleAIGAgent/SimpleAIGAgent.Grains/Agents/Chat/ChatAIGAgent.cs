using Aevatar.AI.Agent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using SimpleAIGAgent.Grains.Agents.Events;

namespace SimpleAIGAgent.Grains.Agents.Chat;

public interface IChatAIGAgent : IAIGAgent, IGAgent
{
}

public class ChatAIGAgent : AIGAgentBase<ChatAIGState, ChatAIEvent>, IChatAIGAgent
{
    public ChatAIGAgent(ILogger<ChatAIGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for chatting with user.");
    }
    
    [EventHandler]
    public async Task OnChatAIEvent(ChatEvent @event)
    {
        var result = await InvokePromptAsync(@event.Message);
    }
}