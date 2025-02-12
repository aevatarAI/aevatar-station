using System;
using System.Threading.Tasks;
using Aevatar.Workflow;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Workflow")]
[Route("api/workflow")]
public class WorkflowController : AevatarController
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly IWorkflowService _workflowService;
    
    public WorkflowController(
        ILogger<WorkflowController> logger,
        IWorkflowService workflowService
        )
    {
        _logger = logger;
        _workflowService = workflowService;
    }
    
    [HttpPost("generate")]
    public async Task<WorkflowDto> GenerateWorkflow([FromBody] GenerateWorkflowDto request)
    {
        _logger.LogInformation("GenerateWorkflow: {TaskDescription}", request.TaskDescription);
        return await _workflowService.GenerateWorkflowAsync(request.TaskDescription);
    }
    
    [HttpPut("{workflowId}/update")]
    public async Task<WorkflowDto> UpdateWorkflow([FromBody] WorkflowDto updatedWorkflow)
    {
        _logger.LogInformation("UpdateWorkflow: {WorkflowId}", updatedWorkflow.WorkflowId);
        return await _workflowService.UpdateWorkflow(updatedWorkflow);
    }
    
    [HttpGet("{workflowId}")]
    public async Task<WorkflowDto> GetWorkflowAsync(Guid workflowId)
    {
        _logger.LogInformation("GetWorkflow: {WorkflowId}", workflowId);
        return await _workflowService.GetWorkflow(workflowId);
    }
}