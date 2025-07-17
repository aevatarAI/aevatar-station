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

    /// <summary>
    /// 根据用户目标生成前端格式的工作流配置
    /// </summary>
    /// <param name="request">工作流生成请求</param>
    /// <returns>前端格式的工作流配置</returns>
    [HttpPost("generate-view-config")]
    public async Task<WorkflowViewConfigDto?> GenerateViewConfigAsync([FromBody] GenerateWorkflowRequest request)
    {
        // Generate workflow using LLM with updated prompt format
        var workflow = await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);
        
        // Convert to JSON string (simulate LLM output in new format)
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(workflow);
        
        // Parse to frontend format
        return await _workflowOrchestrationService.ParseWorkflowJsonToViewConfigAsync(jsonContent);
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