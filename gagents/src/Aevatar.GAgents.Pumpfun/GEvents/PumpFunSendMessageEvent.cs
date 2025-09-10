using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.PumpFun.EventDtos;

[Description("Send a mesage to PumpFunAgent.")]
[GenerateSerializer]
public class PumpFunSendMessageEvent : EventBase
{
    [Description("ReplyId is from PumpFunSendMessageGEvent")]
    [Id(1)]
    public string? ReplyId { get; set; }

    [Description("Text content of the ReplyMessage to be sent.")]
    [Id(2)]
    public string? ReplyMessage { get; set; }
}