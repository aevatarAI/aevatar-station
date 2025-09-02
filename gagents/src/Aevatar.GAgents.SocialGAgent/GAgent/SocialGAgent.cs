using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.SocialChat.GAgent;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.ChatAgent.GAgent.State;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.SocialAgent.GAgent.SEvent;
using Aevatar.GAgents.AI.Common;
using Newtonsoft.Json;

namespace Aevatar.GAgents.TestAgent;

[System.ComponentModel.Description("I can chat with users.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(SocialGAgent))]
public class SocialGAgent : ChatGAgentBase<ChatGAgentState, SocialGAgentLogEvent, EventBase, ChatConfigDto>,
    ISocialGAgent
{
    private readonly ILogger<SocialGAgent> _logger;

    public SocialGAgent(ILogger<SocialGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "SocialGAgent",
            Name = "Social Chat Agent",
            L1Description = "AI agent for social platform chat interactions with multi-turn conversation and emotion understanding capabilities",
            L2Description = "A specialized AI agent designed for social scenarios that handles user chat requests with emotion recognition, multi-turn conversation memory, and personalized responses. Suitable for social media platforms, customer service systems, and other scenarios requiring friendly interactions.",
            Category = "Social",
            Capabilities = new List<string> { "chat", "social-interaction", "emotion-understanding", "multi-turn-conversation" },
            Tags = new List<string> { "social", "chat", "ai", "conversation" }
        };
        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    [EventHandler]
    public async Task<SocialResponseGEvent> HandleEventAsync(SocialGEvent @event)
    {
        _logger.LogInformation("handle SocialEvent, content: {content}", @event.Content);

        SocialResponseGEvent aiResponseEvent = new SocialResponseGEvent();
        aiResponseEvent.RequestId = @event.RequestId;
        try
        {
            var message = await ChatAsync(@event.Content, aiChatContextDto: new AIChatContextDto()
            {
                RequestId = @event.RequestId,
                MessageId = @event.MessageId,
                ChatId = @event.ChatId
            });
            if (message != null && message.Any())
            {
                _logger.LogInformation("handle SocialEvent, AI replyMessage: {msg}", message[0].Content);

                aiResponseEvent.ResponseContent = message[0].Content!;
                aiResponseEvent.ChatId = @event.ChatId;
                aiResponseEvent.ReplyMessageId = @event.MessageId;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle SocialEvent, Get AIReplyMessage Error: {err}", e.Message);
        }

        return aiResponseEvent;
    }
}