using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Telegram.Agent.GEvents;

[GenerateSerializer]
public class MessageSEvent :StateLogEventBase<MessageSEvent>
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
}


[GenerateSerializer]
public class TelegramRequestSEvent : MessageSEvent
{
    [Id(0)] public Guid RequestId { get; set; }
}

[GenerateSerializer]
public class TelegramSocialResponseSEvent : MessageSEvent
{
    [Id(0)] public Guid ResponseId { get; set; }
}