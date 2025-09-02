using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Speech.Events;

[GenerateSerializer]
public class RequestSTTEvent : EventBase
{
    [Id(0)] public byte[] AudioData { get; set; }
}