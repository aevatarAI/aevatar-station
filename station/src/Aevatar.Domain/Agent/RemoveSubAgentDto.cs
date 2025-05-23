using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agent;

[GenerateSerializer]
public class RemoveSubAgentDto
{
    public List<Guid> RemovedSubAgents { get; set; } = new();
}