using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class NewDemandTestEvent : EventBase
{
    [Id(0)] public string Description { get; set; }
}