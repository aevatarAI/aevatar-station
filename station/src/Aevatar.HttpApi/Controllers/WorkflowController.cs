using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Controllers;
using Aevatar.Domain.WorkflowOrchestration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Workflow")]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : AevatarController
{
    private readonly IWorkflowOrchestrationService _workflowOrchestrationService;

    public WorkflowController(IWorkflowOrchestrationService workflowOrchestrationService)
    {
        _workflowOrchestrationService = workflowOrchestrationService;
    }

    /// <summary>
    /// 根据用户目标生成完整工作流
    /// </summary>
    /// <param name="request">工作流生成请求</param>
    /// <returns>完整的工作流定义</returns>
    [HttpPost("generate")]
    public async Task<WorkflowDefinition> GenerateAsync([FromBody] GenerateWorkflowRequest request)
    {
        var workflow = await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);
        return workflow;
    }
}

/// <summary>
/// 工作流生成请求
/// </summary>
public class GenerateWorkflowRequest
{
    /// <summary>
    /// 用户目标描述
    /// </summary>
    public string UserGoal { get; set; } = string.Empty;
} 