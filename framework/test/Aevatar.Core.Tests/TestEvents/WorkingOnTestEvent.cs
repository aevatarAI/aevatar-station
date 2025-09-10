using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class WorkingOnTestEvent : EventBase
{
    [Id(0)] public string Description { get; set; }
}