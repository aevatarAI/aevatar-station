using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Workflow;

[GenerateSerializer]
public class WorkflowDto
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public string TriggerEvent { get; set; }
    public List<EventFlow> EventFlow { get; set; }
}
