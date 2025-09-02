using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.Events;


[GenerateSerializer]
public class ChatEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
}