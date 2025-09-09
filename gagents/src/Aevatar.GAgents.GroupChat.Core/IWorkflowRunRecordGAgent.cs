using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.States;

namespace Aevatar.GAgents.GroupChat.Core;

/// <summary>
/// Interface for Workflow Run Record GAgent - tracks workflow execution with real-time state and data lineage
/// </summary>
public interface IWorkflowRunRecordGAgent : IStateGAgent<WorkflowRunRecordState>
{
    /// <summary>
    /// Gets execution summary for the workflow
    /// </summary>
    Task<string> GetExecutionSummaryAsync();
    
    /// <summary>
    /// Gets all execution records for debugging
    /// </summary>
    Task<List<WorkUnitExecutionFlowRecord>> GetExecutionRecordsAsync();

    /// <summary>
    /// Gets specific work unit execution record
    /// </summary>
    Task<WorkUnitExecutionFlowRecord?> GetWorkUnitRecordAsync(string agentGrainId);
}
