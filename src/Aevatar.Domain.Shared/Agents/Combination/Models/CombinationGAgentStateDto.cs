using System;
using System.Collections.Generic;
using Aevatar.Agents.Combination.Models;

namespace Aevatar.Agents.Atomic.Models;

public class CombinationGAgentStateDto : BaseStateDto
{
    public Guid Id { get; set; }
    public AgentStatus Status { get; set; }
    public string UserAddress { get; set; }
    public string Name { get; set; }
    public string GroupId { get; set; }
    public string AgentComponent { get; set; }
}