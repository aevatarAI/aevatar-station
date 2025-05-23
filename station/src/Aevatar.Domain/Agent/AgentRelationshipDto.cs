using System;
using System.Collections.Generic;

namespace Aevatar.Agent;

public class AgentRelationshipDto
{
    public Guid? Parent { get; set; }
    public List<Guid> SubAgents { get; set; } = new();
}