namespace Aevatar.GAgents.Workflow.Core.Models;

/// <summary>
/// Consolidated WorkUnitInfo that merges runtime data (WorkflowCoordinatorState) 
/// with design-time data (WorkflowViewState) as per design document
/// </summary>
[GenerateSerializer]
public class WorkUnitInfo
{
    [Id(0)] public Guid NodeId { get; set; }                                // From WorkflowNodeDto + WorkflowNodeUnitDto - Node identifier
    [Id(1)] public Guid NextNodeId { get; set; }                            // From WorkflowNodeUnitDto.NextNodeId - Next node identifier
    [Id(2)] public Guid AgentId { get; set; }                               // From WorkUnitInfo.GrainId + WorkflowNodeDto.AgentId - Agent grain identifier
    [Id(3)] public Guid NextAgentId { get; set; }                           // From WorkUnitInfo.NextGrainId - Next agent grain identifier
    [Id(4)] public string AgentType { get; set; } = string.Empty;           // From WorkflowNodeDto
    [Id(5)] public string Name { get; set; } = string.Empty;                // From WorkflowNodeDto  
    [Id(6)] public string JsonProperties { get; set; } = string.Empty;      // From WorkflowNodeDto - EXACTLY WHERE IT IS
    [Id(7)] public WorkerUnitStatusEnum UnitStatusEnum { get; set; }        // From WorkUnitInfo
    [Id(8)] public Dictionary<string,string> ExtendedData { get; set; } = new(); // From WorkUnitInfo + WorkflowNodeDto (merged)
}