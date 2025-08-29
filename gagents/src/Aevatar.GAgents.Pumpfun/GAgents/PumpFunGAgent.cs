using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.PumpFun.Agent.GEvents;
using Aevatar.GAgents.PumpFun.EventDtos;
using Aevatar.GAgents.PumpFun.Grains;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.PumpFun.Agent;

[Description("Advanced trading automation agent for PumpFun platform that handles token monitoring, automated trading strategies, market analysis, and portfolio management with real-time price tracking.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(PumpFunGAgent))]
public class PumpFunGAgent : GAgentBase<PumpFunGAgentState, PumpfunSEventBase>, IPumpFunGAgent
{
    private readonly ILogger<PumpFunGAgent> _logger;

    public PumpFunGAgent(ILogger<PumpFunGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("PumpFun Platform Agent");
    }

    [EventHandler]
    public async Task TaskHandleEventAsync(PumpFunReceiveMessageEvent @event)
    {
        if (@event.ReplyId.IsNullOrEmpty() || @event.RequestMessage.IsNullOrEmpty())
        {
            _logger.LogError(
                $"[PumpFunGAgent] PumpFunReceiveMessageEvent ReplyId is IsNullOrEmpty, replyId:{@event.ReplyId} requestMessage:{@event.RequestMessage}");
            return;
        }

        var requestId = Guid.NewGuid();
        RaiseEvent(new PumpfunRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        await PublishAsync(new SocialGEvent()
        {
            RequestId = requestId,
            Content = @event.RequestMessage,
            MessageId = @event.ReplyId,
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(SocialResponseGEvent @event)
    {
        if (@event.ReplyMessageId.IsNullOrEmpty())
        {
            _logger.LogError($"[PumpFunGAgent] SocialResponseGEvent ReplyMessageId is IsNullOrEmpty");
            return;
        }

        if (@event.RequestId != Guid.Empty)
        {
            if (State.SocialRequestList.Contains(@event.RequestId))
            {
                RaiseEvent(new PumpfunSocialResponseSEvent() { ResponseId = @event.RequestId });
                await ConfirmEvents();
            }
            else
            {
                return;
            }
        }

        _logger.LogDebug("[PumpFunGAgent] HandleEventAsync SocialResponseEvent, content: {text}, id: {id}",
            @event.ResponseContent, @event.ReplyMessageId);

        await GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyMessageId))
            .SendMessageAsync(@event.ReplyMessageId, @event.ResponseContent);
    }

    [EventHandler]
    public async Task HandleEventAsync(PumpFunSendMessageEvent @event)
    {
        _logger.LogInformation("PumpFunSendMessageEvent:" + JsonConvert.SerializeObject(@event));
        if (@event.ReplyId != null)
        {
            PumpFunSendMessageGEvent pumpFunSendMessageGEvent = new PumpFunSendMessageGEvent()
            {
                Id = Guid.Parse(@event.ReplyId),
                ReplyId = @event.ReplyId,
                ReplyMessage = @event.ReplyMessage
            };

            RaiseEvent(pumpFunSendMessageGEvent);
            await ConfirmEvents();
            _logger.LogInformation("PumpFunSendMessageEvent2:" +
                                   JsonConvert.SerializeObject(@pumpFunSendMessageGEvent));
            await GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId))
                .SendMessageAsync(@event.ReplyId, @event.ReplyMessage);
            _logger.LogInformation("PumpFunSendMessageEvent3,grainId:" +
                                   GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId)).GetGrainId());
        }
    }

    public async Task SetPumpFunConfig(string chatId)
    {
        _logger.LogInformation("PumpFunGAgent SetPumpFunConfig, chatId:" + chatId);
        RaiseEvent(new SetPumpFunConfigEvent()
        {
            ChatId = chatId
        });
        await ConfirmEvents();
        _logger.LogInformation("PumpFunGAgent SetPumpFunConfig2, chatId:" + chatId);
    }
}

public interface IPumpFunGAgent : IStateGAgent<PumpFunGAgentState>
{
    Task SetPumpFunConfig(string chatId);
}