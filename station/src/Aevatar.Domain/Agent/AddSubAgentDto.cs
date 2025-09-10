using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agent;

[GenerateSerializer]
public class AddSubAgentDto
{
    public List<Guid> SubAgents { get; set; } = new();
}