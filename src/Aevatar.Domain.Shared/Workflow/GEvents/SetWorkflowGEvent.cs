using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Workflow.GEvents;

[GenerateSerializer]
public class SetWorkflowGEvent : WorkflowAgentGEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public string WorkflowName { get; set; }
    [Id(2)] public string TriggerEvent { get; set; }
    [Id(3)] public List<EventFlow> EventFlow { get; set; }
}