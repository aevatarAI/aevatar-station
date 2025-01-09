using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Combination.GEvents;

[GenerateSerializer]
public class CombineAgentGEvent : CombinationAgentGEvent
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserAddress { get; set; }
    [Id(2)] public string Name { get; set; }
    [Id(3)] public string GroupId { get; set; }
    [Id(4)] public List<string> AgentComponent { get; set; }
}