using Aevatar.Core.Abstractions;

namespace SimpleAIGAgent.Grains.Agents.Events;

[GenerateSerializer]
public class ChatEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
}