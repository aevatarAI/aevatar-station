using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.Workflow.Core;

public interface IWorkflowExecutionRecordGAgentPlus : IStateGAgentPlus<WorkflowExecutionRecordStatePlus>, IBusinessAgentBase
{
}