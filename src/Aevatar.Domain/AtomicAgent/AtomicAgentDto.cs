using System.Collections.Generic;
using Orleans.Runtime;

namespace Aevatar.AtomicAgent;

public class AtomicAgentDto 
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public GrainId GrainId { get; set; }
}