using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agent;

public class UpdateMultiAgentInputDtos
{
    public List<UpdateAgentDto> Agents { get; set; } = new();
}

[GenerateSerializer]
public class UpdateAgentDto : UpdateAgentInputDto
{
    public Guid Id { get; set; }
}