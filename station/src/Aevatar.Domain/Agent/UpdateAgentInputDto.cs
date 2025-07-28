using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agent;

[GenerateSerializer]
public class UpdateAgentInputDto
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public Dictionary<string, object>? Properties { get; set; }
}