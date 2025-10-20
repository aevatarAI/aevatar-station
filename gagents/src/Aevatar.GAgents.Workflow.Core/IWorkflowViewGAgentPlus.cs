using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.Workflow.Core;

public interface IWorkflowViewGAgentPlus : IStateGAgentPlus<WorkflowViewStatePlus>
{
    /// <summary>
    /// Get current round ID (execution counter)
    /// </summary>
    Task<int> GetCurrentRoundIdAsync();
    
    /// <summary>
    /// Execute workflow by creating and sending WorkflowEvent to StartAgent, and increment RoundId
    /// Execution name is automatically generated as: {WorkflowName}-Round{RoundId}
    /// </summary>
    /// <returns>Event ID of the workflow event</returns>
    Task<Guid> ExecuteWorkflowAsync();
}