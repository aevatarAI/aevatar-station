using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class NaiveTestEvent : EventBase
{
    [Id(0)] public string Greeting { get; set; }
}