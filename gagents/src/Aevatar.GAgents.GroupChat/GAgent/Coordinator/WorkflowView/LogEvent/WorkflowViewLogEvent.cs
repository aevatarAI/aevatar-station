using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;

namespace Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.LogEvent;

[GenerateSerializer]
public class WorkflowViewLogEvent : StateLogEventBase<WorkflowViewLogEvent>
{
    
}

[GenerateSerializer]
public class UpdateWorkflowViewLogEvent : WorkflowViewLogEvent
{
    [Id(0)] public List<WorkflowNodeDto> AddNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeDto> UpdateNodeList { get; set; } = new();
    [Id(2)] public List<Guid> RemoveNodeIdList { get; set; } = new();
    [Id(3)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(4)] public string Name { get; set; }
}

[GenerateSerializer]
public class UpdateNodeAgentIdLogEvent : WorkflowViewLogEvent
{
    [Id(0)] public Guid NodeId { get; set; }
    [Id(1)] public Guid AgentId { get; set; }
}

[GenerateSerializer]
public class UpdateWorkflowAgentIdLogEvent : WorkflowViewLogEvent
{
    [Id(0)] public Guid AgentId { get; set; }
}