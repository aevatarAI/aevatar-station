using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.PumpFun.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.PumpFun.Agent;
[GenerateSerializer]
public class PumpFunGAgentState : StateBase
{
    [Id(0)] public Dictionary<string, PumpFunReceiveMessageGEvent> requestMessage { get; set; } = new Dictionary<string, PumpFunReceiveMessageGEvent>();
    [Id(1)] public Dictionary<string, PumpFunSendMessageGEvent> responseMessage { get; set; } = new Dictionary<string, PumpFunSendMessageGEvent>();
    [Id(2)] public List<Guid> SocialRequestList { get; set; } = new List<Guid>();
    
    public void Apply(PumpFunReceiveMessageGEvent receiveMessageGEvent)
    {
        requestMessage[receiveMessageGEvent.ReplyId] = receiveMessageGEvent;
    }
    
    public void Apply(PumpFunSendMessageGEvent sendMessageGEvent)
    {
        responseMessage[sendMessageGEvent.ReplyId] = sendMessageGEvent;
        requestMessage.Remove(sendMessageGEvent.ReplyId);
    }

    public void Apply(PumpfunRequestSEvent @event)
    {
        if (SocialRequestList.Contains(@event.RequestId) == false)
        {
            SocialRequestList.Add(@event.RequestId);
        }
    }

    public void Apply(PumpfunSocialResponseSEvent @event)
    {
        if (SocialRequestList.Contains(@event.ResponseId))
        {
            SocialRequestList.Remove(@event.ResponseId);
        }
    }
}