using System;
using System.Collections.Generic;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Agent;

public class CreateAgentInputDto
{
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}