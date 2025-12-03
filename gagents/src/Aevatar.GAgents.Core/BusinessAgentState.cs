using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Core;

/// <summary>
/// Base state for business agents with workflow coordination capabilities
/// Located in GAgents.Core alongside BusinessAgentBase
/// </summary>
[GenerateSerializer]
public abstract class BusinessAgentState : StateBasePlus
{
    [Id(0)] public string MemberName { get; set; } = string.Empty;
    [Id(1)] public Guid WorkflowCoordinatorId { get; set; }
    [Id(2)] public Guid WorkflowId { get; set; }
    [Id(3)] public DateTime StartedAt { get; set; }
    [Id(4)] public Dictionary<string, object> Metadata { get; set; } = new();
    [Id(5)] public WorkflowAgentStatus WorkflowAgentStatus { get; set; } = WorkflowAgentStatus.Pending;
    
}

