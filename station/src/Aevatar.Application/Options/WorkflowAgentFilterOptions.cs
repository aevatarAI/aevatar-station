using System.Collections.Generic;

namespace Aevatar.Options;

/// <summary>
/// Configuration options for filtering workflow agents
/// </summary>
public class WorkflowAgentFilterOptions
{
    /// <summary>
    /// List of fully qualified agent type names to exclude from workflow agent list
    /// These are typically infrastructure agents like WorkflowStartAgent and WorkflowEndAgent
    /// </summary>
    public List<string> ExcludedAgentTypes { get; set; } = new()
    {
        "Aevatar.GAgents.Workflow.WorkflowStartAgent",
        "Aevatar.GAgents.Workflow.WorkflowEndAgent"
    };
}

