using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;

[GenerateSerializer]
public class SocialResponseGEvent:EventBase
{
    [Id(0)] public Guid RequestId { get; set; } = Guid.NewGuid();
    [Id(1)] public string ResponseContent { get; set; }
    [Description("Unique identifier for the target chat where the message will be sent.")]
    [Id(2)]  public string ChatId { get; set; }
    [Description("Optional ID of the message to which this message is a reply.")]
    [Id(3)]   public string? ReplyMessageId { get; set; }
}

[GenerateSerializer]
public class RegisterTelegramGEvent : EventBase
{
    [Id(0)] public string BotName { get; set; }
    [Id(1)] public string Token { get; set; }
}

[GenerateSerializer]
public class UnRegisterTelegramGEvent : EventBase
{
    [Id(0)] public string BotName { get; set; }
}