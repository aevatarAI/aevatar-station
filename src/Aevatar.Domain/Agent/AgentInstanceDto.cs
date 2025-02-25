using System;
using System.Collections.Generic;
using OpenIddict.Abstractions;

namespace Aevatar.Agent;

public class AgentInstanceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public Dictionary<string,object>? Properties { get; set; }
}