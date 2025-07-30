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

        #region 文本补全相关API

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

        /// <summary>
        /// 获取文本补全Agent状态
        /// </summary>
        /// <returns>Agent状态信息</returns>
        [HttpGet("text-completion/status")]
        [Authorize]
        public async Task<TextCompletionAgentStatusDto> GetTextCompletionStatusAsync()
        {
            try
            {
                var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
                var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

                // 获取agent状态
                var state = await textCompletionAgent.GetStateAsync();
                var recentCompletions = await textCompletionAgent.GetRecentCompletionsAsync();

                var status = new TextCompletionAgentStatusDto
                {
                    IsInitialized = state.TotalCompletions > 0,
                    TotalCompletions = state.TotalCompletions,
                    LastInputText = recentCompletions.LastOrDefault() ?? "",
                    HistoryCount = recentCompletions.Count,
                    LastCompletionTime = DateTime.UtcNow // Simplified to current time
                };

                _logger.LogDebug("Retrieved agent status successfully");
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get agent status");
                
                return new TextCompletionAgentStatusDto
                {
                    IsInitialized = false,
                    TotalCompletions = 0,
                    LastInputText = "",
                    HistoryCount = 0,
                    LastCompletionTime = null
                };
            }
        }

        /// <summary>
        /// Get recent completion history
        /// </summary>
        /// <returns>Recent completion records</returns>
        [HttpGet("text-completion/history")]
        [Authorize]
        public async Task<List<string>> GetCompletionHistoryAsync()
        {
            try
            {
                var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
                var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

                var recentCompletions = await textCompletionAgent.GetRecentCompletionsAsync();

                _logger.LogInformation("Successfully retrieved completion history, returned {Count} records", recentCompletions.Count);
                return recentCompletions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get completion history");
                return new List<string>();
            }
        }

        /// <summary>
        /// 清空补全历史记录
        /// </summary>
        /// <returns>操作结果</returns>
        [HttpDelete("text-completion/history")]
        [Authorize]
        public async Task<IActionResult> ClearCompletionHistoryAsync()
        {
            try
            {
                var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
                var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

                var success = await textCompletionAgent.ClearHistoryAsync();

                if (success)
                {
                    _logger.LogInformation("Completion history cleared successfully");
                    return Ok(new { Success = true, Message = "History cleared successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to clear completion history");
                    return BadRequest(new { Success = false, Message = "Failed to clear history" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing completion history");
                return StatusCode(500, new { Success = false, Message = $"Internal server error: {ex.Message}" });
            }
        }

        #endregion
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

    /// <summary>
    /// Agent状态响应DTO
    /// </summary>
    public class TextCompletionAgentStatusDto
    {
        /// <summary>
        /// Agent是否已初始化
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// 总补全次数
        /// </summary>
        public int TotalCompletions { get; set; }

        /// <summary>
        /// 最近一次输入文本
        /// </summary>
        public string LastInputText { get; set; } = string.Empty;

        /// <summary>
        /// 历史记录数量
        /// </summary>
        public int HistoryCount { get; set; }

        /// <summary>
        /// 最近一次补全时间
        /// </summary>
        public DateTime? LastCompletionTime { get; set; }
    }

    #endregion
} 