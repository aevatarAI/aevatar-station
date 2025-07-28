using Aevatar.Core.Abstractions;

namespace SignalRSample.GAgents;

[GenerateSerializer]
public class NaiveTestEvent : EventBase
{
    [Id(0)] public string Greeting { get; set; }
}