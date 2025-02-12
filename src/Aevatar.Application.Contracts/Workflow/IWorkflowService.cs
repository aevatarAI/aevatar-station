using System;
using System.Threading.Tasks;

namespace Aevatar.Workflow;

public interface IWorkflowService
{
    Task<WorkflowDto> GenerateWorkflowAsync(string taskDescription);
    Task<WorkflowDto> GetWorkflow(Guid workflowId);
    Task<WorkflowDto> UpdateWorkflow(WorkflowDto updatedWorkflow);
}