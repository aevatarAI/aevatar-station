using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Controllers;
using Aevatar.Service;
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
    private readonly ITextCompletionService _textCompletionService;

    public WorkflowController(
        IWorkflowOrchestrationService workflowOrchestrationService,
        ITextCompletionService textCompletionService)
    {
        _workflowOrchestrationService = workflowOrchestrationService;
        _textCompletionService = textCompletionService;
    }

    /// <summary>
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="request">工作流生成请求</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    [HttpPost("generate")]
    public async Task<WorkflowViewConfigDto?> GenerateAsync([FromBody] GenerateWorkflowRequestDto request)
    {
        return await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);
    }

    /// <summary>
    /// 根据用户输入生成5个不同的文本补全选项
    /// </summary>
    /// <param name="request">文本补全请求</param>
    /// <returns>包含5个补全选项的响应</returns>
    [HttpPost("text-completion")]
    public async Task<TextCompletionResponseDto> GenerateTextCompletionAsync([FromBody] TextCompletionRequestDto request)
    {
        return await _textCompletionService.GenerateCompletionsAsync(request);
    }
} 