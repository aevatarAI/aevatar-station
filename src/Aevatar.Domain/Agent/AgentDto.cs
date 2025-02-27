using System;
using System.Collections.Generic;
using Orleans.Runtime;

namespace Aevatar.Agent;

public class AgentDto
{
    public Guid Id { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public GrainId GrainId { get; set; }
    public Guid AgentGuid { get; set; }
    public string PropertyJsonSchema { get; set; }
}
