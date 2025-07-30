using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.HttpApi.Controllers
{
    /// <summary>
    /// 工作流相关API控制器
    /// </summary>
    [ApiController]
    [Route("api/workflow")]
    public class WorkflowController : AbpControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(IClusterClient clusterClient, ILogger<WorkflowController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
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
            try
            {
                // 验证输入
                if (request == null || string.IsNullOrWhiteSpace(request.InputText))
                {
                    return new TextCompletionResponseDto
                    {
                        Success = false,
                        Completions = new List<string>(),
                        ErrorMessage = "Input text cannot be empty"
                    };
                }

                // 验证最少15个字符
                if (request.InputText.Trim().Length < 15)
                {
                    return new TextCompletionResponseDto
                    {
                        Success = false,
                        Completions = new List<string>(),
                        ErrorMessage = "Input text must be at least 15 characters long"
                    };
                }

                // 获取TextCompletionGAgent实例
                var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
                var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

                // 调用agent生成补全
                var completions = await textCompletionAgent.GenerateCompletionsAsync(request.InputText);

                // 转换为响应DTO
                var response = new TextCompletionResponseDto
                {
                    Completions = completions,
                    Success = true
                };

                _logger.LogInformation("Text completion generated successfully, returned {Count} options", completions.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate text completion");
                
                return new TextCompletionResponseDto
                {
                    Completions = new List<string>(),
                    Success = false,
                    ErrorMessage = $"Failed to generate completion: {ex.Message}"
                };
            }
        }
    }

    #region DTOs

    /// <summary>
    /// 文本补全请求DTO
    /// </summary>
    public class TextCompletionRequestDto
    {
        /// <summary>
        /// 需要补全的输入文本
        /// </summary>
        public string InputText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文本补全响应DTO - 简化版，只返回5个补全字符串
    /// </summary>
    public class TextCompletionResponseDto
    {
        /// <summary>
        /// 5个补全选项
        /// </summary>
        public List<string> Completions { get; set; } = new();

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }



    #endregion
} 