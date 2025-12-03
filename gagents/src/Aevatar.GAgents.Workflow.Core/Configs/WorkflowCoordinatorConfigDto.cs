using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Workflow.Core.Configs;

[GenerateSerializer]
public class WorkflowCoordinatorConfigDto:ConfigurationBase
{
    [Id(0)] 
    [Description("List of workflow units that define the execution sequence and flow")]
    public List<WorkflowUnitDto> WorkflowUnitList { get; set; } = new();
    
    [Id(1)] 
    [Description("Initial content or context to start the workflow execution")]
    public string? InitContent { get; set; } = null;

    [Id(2)] 
    [Description("Enables recording and tracking of workflow execution steps for debugging and monitoring")]
    public bool EnableExecutionRecord { get; set; }
}

[GenerateSerializer]
public class WorkflowUnitDto
{
    [Id(0)] 
    [Description("Unique identifier for the current workflow unit/GAgent")]
    public string GrainId { get; set; }
    
    [Id(1)] 
    [Description("Identifier for the next workflow unit/GAgent in the execution sequence")]
    public string NextGrainId { get; set; }
    
    [Id(2)] 
    [Description("Additional configuration data and parameters specific to this workflow unit")]
    public Dictionary<string,string> ExtendedData { get; set; } = new();
    
    [Id(3)] 
    [Description("Name of the agent associated with this workflow unit")]
    public string AgentName { get; set; } = string.Empty;
}