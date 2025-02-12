using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Workflow;

[GenerateSerializer]
public class WorkflowGAgentState : StateBase
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public string WorkflowName { get; set; }
    [Id(2)] public string TriggerEvent { get; set; }
    [Id(3)] public List<EventFlow> EventFlow { get; set; }
    [Id(4)] public DateTime CreateTime { get; set; } 
}