using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.Telegram.Agent.GEvents;
using Aevatar.GAgents.Telegram.GEvents;
using Aevatar.GAgents.Telegram.Grains;
using Aevatar.GAgents.Telegram.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Providers;

namespace Aevatar.GAgents.Telegram.Agent;

[Description("Advanced Telegram bot integration agent that enables automated messaging, group management, inline queries, and custom commands. Supports rich media handling, user authentication, and seamless bot-to-user communication.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TelegramGAgent))]
public class TelegramGAgent : GAgentBase<TelegramGAgentState, MessageSEvent, EventBase, TelegramOptionsDto>,
    ITelegramGAgent
{
    public TelegramGAgent(ILogger<TelegramGAgent> logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Telegram Bot Agent");
    }

    public async Task RegisterTelegramAsync(string botName, string token)
    {
        RaiseEvent(new SetTelegramConfigEvent()
        {
            BotName = botName,
            Token = token
        });
        await ConfirmEvents();
        await GrainFactory.GetGrain<ITelegramGrain>(botName).RegisterTelegramAsync(State.Webhook,
            State.BotName, State.Token);
    }

    public async Task UnRegisterTelegramAsync(string botName)
    {
        await GrainFactory.GetGrain<ITelegramGrain>(botName).UnRegisterTelegramAsync(
            State.Token);
    }


    [EventHandler]
    public async Task HandleEventAsync(ReceiveMessageGEvent @event)
    {
        Logger.LogInformation("Telegram ReceiveMessageEvent " + @event.MessageId);
        if (State.PendingMessages.TryGetValue(@event.MessageId, out _))
        {
            Logger.LogDebug("Message reception repeated for Telegram Message ID: " + @event.MessageId);
            return;
        }

        var requestId = Guid.NewGuid();
        RaiseEvent(new TelegramRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        RaiseEvent(new ReceiveMessageSEvent
        {
            MessageId = @event.MessageId,
            ChatId = @event.ChatId,
            Message = @event.Message,
            NeedReplyBotName = State.BotName
        });
        await ConfirmEvents();
        await PublishAsync(new SocialGEvent()
        {
            Content = @event.Message,
            MessageId = @event.MessageId,
            ChatId = @event.ChatId
        });
        Logger.LogDebug("Publish AutoGenCreatedEvent for Telegram Message ID: " + @event.MessageId);
    }

    [EventHandler]
    public async Task HandleEventAsync(SendMessageGEvent @event)
    {
        Logger.LogDebug("Publish SendMessageEvent for Telegram Message: " + @event.Message);
        await SendMessageAsync(@event.Message, @event.ChatId, @event.ReplyMessageId);
    }

    [EventHandler]
    public async Task HandleEventAsync(SocialResponseGEvent @event)
    {
        if (@event.RequestId != Guid.Empty)
        {
            if (State.SocialRequestList.Contains(@event.RequestId))
            {
                RaiseEvent(new TelegramSocialResponseSEvent() { ResponseId = @event.RequestId });
                await ConfirmEvents();
            }
            else
            {
                return;
            }
        }

        Logger.LogDebug("SocialResponse for Telegram Message: " + @event.ResponseContent);
        await SendMessageAsync(@event.ResponseContent, @event.ChatId, @event.ReplyMessageId);
    }

    [EventHandler]
    public async Task HandleEventAsync(RegisterTelegramGEvent @event)
    {
        await RegisterTelegramAsync(@event.BotName, @event.Token);
    }

    [EventHandler]
    public async Task HandleEventAsync(UnRegisterTelegramGEvent @event)
    {
        await UnRegisterTelegramAsync(@event.BotName);
    }

    private async Task SendMessageAsync(string message, string chatId, string? replyMessageId)
    {
        if (replyMessageId != null)
        {
            RaiseEvent(new SendMessageSEvent()
            {
                ReplyMessageId = replyMessageId,
                ChatId = chatId,
                Message = message
            });
            await ConfirmEvents();
        }

        await GrainFactory.GetGrain<ITelegramGrain>(State.BotName).SendMessageAsync(
            State.Token, chatId, message, replyMessageId);
    }

    protected override async Task PerformConfigAsync(TelegramOptionsDto initializationEvent)
    {
        RaiseEvent(new TelegramOptionSEvent()
            { Webhook = initializationEvent.Webhook, EncryptionPassword = initializationEvent.EncryptionPassword });

        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TelegramGAgentState state, StateLogEventBase<MessageSEvent> @event)
    {
        switch (@event)
        {
            case ReceiveMessageSEvent @receiveMessageSEvent:
                state.PendingMessages[receiveMessageSEvent.MessageId] = receiveMessageSEvent;
                break;
            case SendMessageSEvent sendMessageSEvent:
                if (!sendMessageSEvent.ReplyMessageId.IsNullOrEmpty())
                {
                    state.PendingMessages.Remove(sendMessageSEvent.ReplyMessageId);
                }

                break;
            case SetTelegramConfigEvent setTelegramConfigEvent:
                state.BotName = setTelegramConfigEvent.BotName;
                state.Token = setTelegramConfigEvent.Token;
                break;
            case TelegramRequestSEvent @requestSEvent:
                if (state.SocialRequestList.Contains(@requestSEvent.RequestId) == false)
                {
                    state.SocialRequestList.Add(@requestSEvent.RequestId);
                }

                break;
            case TelegramOptionSEvent @telegramOptionSEvent:
                state.Webhook = @telegramOptionSEvent.Webhook;
                state.EncryptionPassword = @telegramOptionSEvent.EncryptionPassword;
                break;
            case TelegramSocialResponseSEvent @telegramSocialResponseSEvent:
                if (state.SocialRequestList.Contains(@telegramSocialResponseSEvent.ResponseId))
                {
                    state.SocialRequestList.Remove(@telegramSocialResponseSEvent.ResponseId);
                }

                break;
        }
    }
}

public interface ITelegramGAgent : IStateGAgent<TelegramGAgentState>
{
    Task RegisterTelegramAsync(string botName, string token);

    Task UnRegisterTelegramAsync(string botName);
}