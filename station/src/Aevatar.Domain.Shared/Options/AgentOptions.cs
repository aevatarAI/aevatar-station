using System.Collections.Generic;

namespace Aevatar.Options;

public class AgentOptions
{
    public List<string> SystemAgentList { get; set; } = new List<string>();
    
    /// <summary>
    /// Whitelist of agents to include. If not empty, only these agents will be returned.
    /// If empty, the SystemAgentList blacklist will be used instead.
    /// </summary>
    public List<string> WhitelistAgentList { get; set; } = new List<string>();
}