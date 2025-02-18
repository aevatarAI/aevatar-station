using Aevatar.Core.Abstractions;

namespace SignalRSample.Host;

[GenerateSerializer]
public class TestEvent : EventBase
{
    [Id(0)] public string Message { get; set; }
}