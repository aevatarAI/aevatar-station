using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers
{
    /// <summary>
    /// 工作流控制器
    /// </summary>
    [ApiController]
    [Route("api/workflow")]
    [Authorize]
    public class WorkflowController : AbpControllerBase
    {
        private readonly IWorkflowOrchestrationService _workflowOrchestrationService;
        private readonly ITextCompletionService _textCompletionService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            IWorkflowOrchestrationService workflowOrchestrationService,
            ITextCompletionService textCompletionService,
            ILogger<WorkflowController> logger)
        {
            _workflowOrchestrationService = workflowOrchestrationService;
            _textCompletionService = textCompletionService;
            _logger = logger;
        }

        /// <summary>
        /// 生成工作流
        /// </summary>
        /// <param name="request">生成请求</param>
        /// <returns>工作流配置</returns>
        [HttpPost("generate")]
        public async Task<AiWorkflowViewConfigDto?> GenerateAsync([FromBody] GenerateWorkflowRequestDto request)
        {
            _logger.LogInformation("收到工作流生成请求，用户目标：{UserGoal}", request.UserGoal);

            var result = await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);

            if (result != null)
            {
                _logger.LogInformation("工作流生成成功，包含 {NodeCount} 个节点",
                    result.Properties?.WorkflowNodeList?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("工作流生成失败");
            }

            return result;
        }

        /// <summary>
        /// 根据用户输入生成5个不同的文本补全选项  
        /// </summary>
        /// <param name="request">文本补全请求</param>
        /// <returns>包含5个补全选项的响应</returns>
        [HttpPost("text-completion/generate")]
        public async Task<TextCompletionResponseDto> GenerateTextCompletionAsync(
            [FromBody] TextCompletionRequestDto request)
        {
            _logger.LogInformation("收到文本补全请求，输入：{UserGoal}", request.UserGoal);

            var result = await _textCompletionService.GenerateCompletionsAsync(request);

            _logger.LogInformation("文本补全生成成功，返回 {Count} 个选项", result.Completions?.Count ?? 0);

            return result;
        }
    }

    /// <summary>
    /// 生成工作流请求DTO
    /// </summary>
    public class GenerateWorkflowRequestDto
    {
        /// <summary>
        /// 用户目标描述
        /// </summary>
        public string UserGoal { get; set; } = string.Empty;
    }
}