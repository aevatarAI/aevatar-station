using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.Models;

namespace Aevatar.GAgents.Workflow.Core.Events;

[GenerateSerializer]
public class StartExecuteWorkflowEvent: EventBase
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long RoundId { get; set; }
    [Id(2)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new ();
    [Id(3)] public string? Content { get; set; } = null;
}

[GenerateSerializer]
public class StartExecuteWorkUnitEvent : EventBase
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public string? Instructions { get; set; } = null;
}