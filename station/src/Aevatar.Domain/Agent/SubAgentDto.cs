using System;
using System.Collections.Generic;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Agent;

public class SubAgentDto
{
    public List<Guid> SubAgents { get; set; } = new();
}