using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Controllers;
using Aevatar.Domain.WorkflowOrchestration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using System;
using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers;

[RemoteService]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : AevatarController
{
    private readonly IWorkflowOrchestrationService _workflowOrchestrationService;
    private readonly ITextCompletionService _textCompletionService;
    private readonly ILogger<WorkflowController> _logger;

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
    
    /// <summary>
    /// 生成文本补全
    /// </summary>
    /// <param name="request">补全请求</param>
    /// <returns>补全结果</returns>
    [HttpPost("text-completion/generate")]
    [Authorize]
    public async Task<TextCompletionResponseDto> GenerateTextCompletionAsync([FromBody] TextCompletionRequestDto request)
    {
        _logger.LogDebug("Received text completion request");
            
        if (request == null)
        {
            throw new UserFriendlyException("Request cannot be null");
        }

        return await _textCompletionService.GenerateCompletionsAsync(request);
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