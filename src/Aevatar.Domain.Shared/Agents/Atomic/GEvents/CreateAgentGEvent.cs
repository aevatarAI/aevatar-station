using System;
using Orleans;

namespace Aevatar.Agents.Atomic.GEvents;

[GenerateSerializer]
public class CreateAgentGEvent : AtomicAgentGEvent
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserAddress { get; set; }
    [Id(2)] public string Type { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string BusinessAgentId { get; set; }
    [Id(5)] public string Properties { get; set; }
}