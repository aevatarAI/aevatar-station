using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;

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
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public List<ChatMessage>? CoordinatorMessages { get; set; } = null;
}