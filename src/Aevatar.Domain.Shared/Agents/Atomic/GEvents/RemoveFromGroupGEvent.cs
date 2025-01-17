using Orleans;

namespace Aevatar.Agents.Atomic.GEvents;

[GenerateSerializer]
public class RemoveFromGroupGEvent : AtomicAgentGEvent
{
    [Id(0)] public string GroupId { get; set; }
}