using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Combination.GEvents;

[GenerateSerializer]
public class CombineAgentGEvent : CombinationAgentGEvent
{
    [Id(0)] public override Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string Name { get; set; }
    [Id(3)] public string GroupId { get; set; }
    [Id(4)] public Dictionary<string, string> AgentComponent { get; set; }
    [Id(5)] public Guid CombineGAgentId { get; set; }
}