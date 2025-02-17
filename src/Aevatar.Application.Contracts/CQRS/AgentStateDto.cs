using System.Collections.Generic;

namespace Aevatar.CQRS;

public class AgentStateDto
{
    public Dictionary<string, object>? State { get; set; } = new Dictionary<string, object>();
}