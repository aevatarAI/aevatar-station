using System;
using System.Collections.Generic;

namespace Aevatar.Agent;

public class CreateAgentInputDto
{
    public Guid? AgentId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}