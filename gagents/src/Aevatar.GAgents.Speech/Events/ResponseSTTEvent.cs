using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Speech.Events;

[GenerateSerializer]
public class ResponseSTTEvent : EventBase
{
    [Id(0)] public string Text { get; set; }
}