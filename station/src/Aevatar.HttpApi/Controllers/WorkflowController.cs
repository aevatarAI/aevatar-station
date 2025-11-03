using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Aevatar.WorkflowRun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers
{
    /// <summary>
    /// Workflow Controller
    /// </summary>
    [ApiController]
    [Route("api/workflow")]
    public class WorkflowController : AbpControllerBase
    {
        private readonly IWorkflowOrchestrationService _workflowOrchestrationService;
        private readonly ITextCompletionService _textCompletionService;
        private readonly IWorkflowRunService _workflowRunService;

        public WorkflowController(
            IWorkflowOrchestrationService workflowOrchestrationService,
            ITextCompletionService textCompletionService,
            IWorkflowRunService workflowRunService)
        {
            _workflowOrchestrationService = workflowOrchestrationService;
            _textCompletionService = textCompletionService;
            _workflowRunService = workflowRunService;
        }

        /// <summary>
        /// Get all workflow agent types that inherit from BusinessAgentBase
        /// </summary>
        /// <returns>List of workflow agent type information</returns>
        [HttpGet("agent-type-info-list")]
        public async Task<List<AgentTypeDto>> GetAllWorkflowAgents()
        {
            return await _workflowRunService.GetAllWorkflowAgents();
        }

        /// <summary>
        /// Generate workflow
        /// </summary>
        /// <param name="request">Generation request</param>
        /// <returns>Workflow configuration</returns>
        [HttpPost("generate")]
        public async Task<AiWorkflowViewConfigDto?> GenerateAsync([FromBody] GenerateWorkflowRequestDto request)
        {
            return await _workflowOrchestrationService.GenerateWorkflowAsync(request.UserGoal);
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
            return await _textCompletionService.GenerateCompletionsAsync(request);
        }

        /// <summary>
        /// 运行工作流 - 包含验证、发布、执行的完整流程
        /// </summary>
        /// <param name="request">工作流运行请求</param>
        /// <returns>工作流运行结果</returns>
        [HttpPost("run")]
        public async Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request)
        {
            return await _workflowRunService.RunWorkflowAsync(request);
        }

    }
}