using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers
{
    /// <summary>
    /// 工作流控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : AbpControllerBase
    {
        private readonly IWorkflowOrchestrationService _workflowOrchestrationService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            IWorkflowOrchestrationService workflowOrchestrationService,
            ILogger<WorkflowController> logger)
        {
            _workflowOrchestrationService = workflowOrchestrationService;
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