using System.Collections.Generic;
using Orleans.Runtime;

namespace Aevatar.CombinationAgent;

public class CombinationAgentDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> AgentComponent { get; set; }
    public GrainId GrainId { get; set; }
}