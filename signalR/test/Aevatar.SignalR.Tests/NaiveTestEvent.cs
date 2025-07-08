using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Tests;

[GenerateSerializer]
public class NaiveTestEvent : EventBase
{
    [Id(0)] public string Greeting { get; set; }
}