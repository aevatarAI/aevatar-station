using System.Collections.Generic;

namespace Aevatar.AtomicAgent;

public class UpdateAtomicAgentDto
{
    public string? Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}