using System;
using System.Collections.Generic;
using Aevatar.Agents.Combination.GEvents;
using Aevatar.Agents.Combination.Models;
using Aevatar.Agents.Group;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Combination;

[GenerateSerializer]
public class CombinationGAgentState : GroupAgentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public AgentStatus Status { get; set; }
    [Id(2)] public string UserAddress { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string GroupId { get; set; }
    [Id(5)] public Dictionary<string, string> AgentComponent { get; set; } //  {atomicId: businessId}
}

