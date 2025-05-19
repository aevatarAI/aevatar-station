using System;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class CreateAgentGEvent : CreatorAgentGEvent
{
    [Id(0)] public override Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string AgentType { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public Guid AgentId { get; set; }
    [Id(5)] public string Properties { get; set; }
    [Id(6)] public GrainId BusinessAgentGrainId { get; set; }
}