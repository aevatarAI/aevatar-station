using Aevatar.Core.Abstractions;

namespace MessagingGAgent.Grains.Agents.Events;

[GenerateSerializer]
public class MessagingEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
}