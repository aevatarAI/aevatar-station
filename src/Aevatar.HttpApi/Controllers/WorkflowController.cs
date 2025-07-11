using System.Threading.Tasks;
using Aevatar.Service;
using Aevatar.Workflow;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[Route("api/workflow")]
public class WorkflowController : AevatarController
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpPost]
    public async Task<WorkflowResponseDto> CreateWorkflowAsync([FromBody] CreateWorkflowDto dto)
    {
        return await _workflowService.CreateWorkflowAsync(dto);
    }
} 