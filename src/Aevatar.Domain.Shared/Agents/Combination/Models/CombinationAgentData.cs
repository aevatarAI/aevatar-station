using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Combination.Models;

[GenerateSerializer]
public class CombinationAgentData
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public AgentStatus Status { get; set; }
    [Id(2)] public Guid UserId { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string GroupId { get; set; }
    [Id(5)] public Dictionary<string, string> AgentComponent { get; set; } 
    [Id(6)] public List<EventDescription> EventInfoList { get; set; }
}