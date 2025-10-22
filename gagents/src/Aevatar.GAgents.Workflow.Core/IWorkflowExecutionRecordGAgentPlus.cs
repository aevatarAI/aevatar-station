using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.Workflow.Core;

/// <summary>
/// ✅ REFACTORED: WorkflowExecutionRecordGAgent is a system agent (recorder), not a business agent
/// Removed IBusinessAgentBase inheritance as it's not a business processor
/// </summary>
public interface IWorkflowExecutionRecordGAgentPlus : IStateGAgentPlus<WorkflowExecutionRecordStatePlus>
{
}