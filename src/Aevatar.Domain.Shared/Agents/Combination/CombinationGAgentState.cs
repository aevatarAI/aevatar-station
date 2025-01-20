using System;
using System.Collections.Generic;
using Aevatar.Agents.Combination.Models;
using Aevatar.Agents.Group;
using Orleans;

namespace Aevatar.Agents.Combination;

[GenerateSerializer]
public class CombinationGAgentState : GroupAgentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public AgentStatus Status { get; set; }
    [Id(2)] public Guid UserId { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string GroupId { get; set; }
    [Id(5)] public Dictionary<string, string> AgentComponent { get; set; } //  {atomicId: businessId}
    [Id(6)] public List<EventDescription> EventInfoList { get; set; } = new();
    [Id(7)] public DateTime CreateTime { get; set; } 
}

[GenerateSerializer]
public class EventDescription
{
    [Id(0)] public Type EventType { get; set; }
    [Id(1)] public string Description { get; set; }
    [Id(2)] public List<EventProperty> EventProperties { get; set; }
}

[GenerateSerializer]
public class EventProperty
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Type { get; set; }
    [Id(2)] public string Description { get; set; }
}

