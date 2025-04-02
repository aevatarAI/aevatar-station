using System;
using System.Collections.Generic;
using OpenIddict.Abstractions;
using Orleans;

namespace Aevatar.Agent;

[GenerateSerializer]
public class AgentInstanceSQRSDto
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public string AgentType { get; set; }
    [Id(3)] public string? Properties { get; set; }
}

public class AgentInstanceDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AgentType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}