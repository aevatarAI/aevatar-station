using Orleans;

namespace Aevatar.Agents.Atomic.GEvents;

[GenerateSerializer]
public class AddToGroupGEvent : AtomicAgentGEvent
{
    [Id(0)] public string GroupId { get; set; }
    [Id(1)] public string BusinessAgentId { get; set; }
}