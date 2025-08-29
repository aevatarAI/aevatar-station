using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;

[GenerateSerializer]
public class WorkflowViewConfigDto : ConfigurationBase
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public string Name { get; set; }
    [Id(3)] public Guid WorkflowCoordinatorGAgentId { get; set; }

}

[GenerateSerializer]
public class WorkflowNodeDto
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public Dictionary<string,string> ExtendedData { get; set; } = new();
    [Id(3)] public string JsonProperties { get; set; }
    [Id(4)] public Guid NodeId { get; set; }
    [Id(5)] public Guid AgentId { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeUnitDto
{
    [Id(0)] public Guid NodeId { get; set; }
    [Id(1)] public Guid NextNodeId { get; set; }
}