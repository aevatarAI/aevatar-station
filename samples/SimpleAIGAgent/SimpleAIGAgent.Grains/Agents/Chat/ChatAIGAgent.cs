using Aevatar.AI.Agent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using SimpleAIGAgent.Grains.Agents.Events;

namespace SimpleAIGAgent.Grains.Agents.Chat;

public interface IChatAIGAgent : IAIGAgent, IGAgent
{
    Task<string?> ChatAsync(string message);
}

public class ChatAIGAgent : AIGAgentBase<ChatAIGStateBase, ChatAIStateLogEvent>, IChatAIGAgent
{
    public ChatAIGAgent(ILogger<ChatAIGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for chatting with user.");
    }

    public async Task<string?> ChatAsync(string message)
    {
        var result = await ChatWithHistory(message);
        return result?[0].Content;
    }

    [EventHandler]
    public async Task OnChatAIEvent(ChatEvent @event)
    {
        var result = await ChatAsync(@event.Message);
        Logger.LogInformation("Chat output: {Result}", result);
    }
}