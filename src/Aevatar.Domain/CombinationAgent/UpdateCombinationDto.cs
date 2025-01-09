using System.Collections.Generic;

namespace Aevatar.CombinationAgent;

public class UpdateCombinationDto
{
    public string? Name { get; set; }
    public Dictionary<string, int>? AgentComponents { get; set; }
}