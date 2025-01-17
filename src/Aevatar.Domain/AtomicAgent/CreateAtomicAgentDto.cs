using System.Collections.Generic;

namespace Aevatar.AtomicAgent;

public class CreateAtomicAgentDto
{
    public string Type { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}