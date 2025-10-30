using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.Configs;

namespace Aevatar.GAgents.Workflow.Core.States;

[GenerateSerializer]
public class WorkflowViewStatePlus : StateBasePlus
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public Guid WorkflowCoordinatorGAgentId { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public Guid AgentId { get; set; }
}
