namespace Aevatar.GAgents.Workflow.Core.Models;

/// <summary>
/// Consolidated WorkUnitInfo that merges runtime data (WorkflowCoordinatorState) 
/// with design-time data (WorkflowViewState) as per design document
/// </summary>
[GenerateSerializer]
public class WorkUnitInfo
{
    [Id(0)] public string NodeId { get; set; } = string.Empty;              // From WorkflowNodeDto + WorkflowNodeUnitDto - GrainId string
    [Id(1)] public string NextNodeId { get; set; } = string.Empty;          // From WorkflowNodeUnitDto.NextNodeId - GrainId string
    [Id(2)] public string AgentId { get; set; } = string.Empty;             // From WorkUnitInfo.GrainId + WorkflowNodeDto.AgentId - GrainId string
    [Id(3)] public string NextAgentId { get; set; } = string.Empty;         // From WorkUnitInfo.NextGrainId - GrainId string
    [Id(4)] public string AgentType { get; set; } = string.Empty;           // From WorkflowNodeDto
    [Id(5)] public string Name { get; set; } = string.Empty;                // From WorkflowNodeDto  
    [Id(6)] public string JsonProperties { get; set; } = string.Empty;      // From WorkflowNodeDto - EXACTLY WHERE IT IS
    [Id(7)] public WorkerUnitStatusEnum UnitStatusEnum { get; set; }        // From WorkUnitInfo
    [Id(8)] public Dictionary<string,string> ExtendedData { get; set; } = new(); // From WorkUnitInfo + WorkflowNodeDto (merged)
}