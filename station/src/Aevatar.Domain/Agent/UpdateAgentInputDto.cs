using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agent;

[GenerateSerializer]
public class UpdateAgentInputDto
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public Dictionary<string, object>? Properties { get; set; }
}

[GenerateSerializer]
public class UpdateAgentInput
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Properties { get; set; }
}