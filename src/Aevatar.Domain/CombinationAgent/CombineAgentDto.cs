using System.Collections.Generic;

namespace Aevatar.CombinationAgent;

public class CombineAgentDto
{
    public string Name { get; set; }
    public Dictionary<string, int> AgentComponents { get; set; }
}