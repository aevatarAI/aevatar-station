using System.Collections.Generic;

namespace Aevatar.CombinationAgent;

public class CombinationAgentDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> AgentComponent { get; set; }
}