using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Controllers;
using Aevatar.Domain.WorkflowOrchestration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
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
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="request">工作流生成请求</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    [HttpPost("generate")]
    public async Task<WorkflowViewConfigDto?> GenerateAsync([FromBody] GenerateWorkflowRequest request)
    {
        return await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);
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