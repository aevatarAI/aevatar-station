using System.Threading.Tasks;
using Aevatar.Workflow;

namespace Aevatar.Service;

public interface IWorkflowService
{
    Task<WorkflowResponseDto> CreateWorkflowAsync(CreateWorkflowDto dto);
} 