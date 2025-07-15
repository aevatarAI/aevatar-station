using System.Collections.Generic;

namespace Aevatar.Agent;

public class CreateMultiAgentInputDto
{
    public List<CreateAgentInputDto> Agents { get; set; } = new();
}