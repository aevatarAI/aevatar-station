using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Newtonsoft.Json;
using System.Linq;

namespace Aevatar.Controllers;

/// <summary>
/// 文本补全请求DTO
/// </summary>
public class TextCompletionRequestDto
{
    /// <summary>
    /// 需要补全的输入文本
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "输入文本长度必须在1-2000字符之间")]
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
    public bool Success { get; set; } = true;

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
    /// 最后输入的文本
    /// </summary>
    public string LastInputText { get; set; } = string.Empty;

    /// <summary>
    /// 历史记录数量
    /// </summary>
    public int HistoryCount { get; set; }

    /// <summary>
    /// 最后补全时间
    /// </summary>
    public DateTime? LastCompletionTime { get; set; }
}

/// <summary>
/// 文本补全API控制器
/// </summary>
[Route("api/text-completion")]
public class TextCompletionController : AevatarController
{
    private readonly ILogger<TextCompletionController> _logger;
    private readonly IClusterClient _clusterClient;

    public TextCompletionController(
        ILogger<TextCompletionController> logger,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    /// <summary>
    /// 生成文本补全
    /// </summary>
    /// <param name="request">补全请求</param>
    /// <returns>包含5个补全选项的响应</returns>
    [HttpPost("generate")]
    [Authorize]
    public async Task<TextCompletionResponseDto> GenerateCompletionAsync([FromBody] TextCompletionRequestDto request)
    {
        _logger.LogInformation("开始文本补全生成，输入文本长度: {Length}字符", request.InputText.Length);

        try
        {
            // 获取TextCompletionGAgent实例
            var agentId = Guid.NewGuid(); // 每次使用新的agent实例
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

            // 调用agent生成补全
            var completions = await textCompletionAgent.GenerateCompletionsAsync(request.InputText);

            // 转换为响应DTO
            var response = new TextCompletionResponseDto
            {
                Completions = completions,
                Success = true
            };

            _logger.LogInformation("文本补全生成成功，返回{Count}个选项", completions.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文本补全生成失败，输入文本: {InputText}", request.InputText);
            
            return new TextCompletionResponseDto
            {
                Completions = new List<string>(),
                Success = false,
                ErrorMessage = $"补全生成失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 快速文本补全（简化接口）
    /// </summary>
    /// <param name="text">需要补全的文本</param>
    /// <returns>补全结果</returns>
    [HttpPost("quick")]
    [Authorize]
    public async Task<TextCompletionResponseDto> QuickCompletionAsync([FromBody] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TextCompletionResponseDto
            {
                Completions = new List<string>(),
                Success = false,
                ErrorMessage = "输入文本不能为空"
            };
        }

        var request = new TextCompletionRequestDto { InputText = text };
        return await GenerateCompletionAsync(request);
    }

    /// <summary>
    /// 获取Agent状态信息
    /// </summary>
    /// <returns>Agent当前状态</returns>
    [HttpGet("status")]
    [Authorize]
    public async Task<TextCompletionAgentStatusDto> GetAgentStatusAsync()
    {
        try
        {
            // 使用固定的agent ID来获取状态
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
                LastCompletionTime = DateTime.UtcNow // 简化为当前时间
            };

            _logger.LogDebug("获取Agent状态成功");
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Agent状态失败");
            
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
    /// 获取最近的补全历史记录
    /// </summary>
    /// <returns>最近的补全记录</returns>
    [HttpGet("history")]
    [Authorize]
    public async Task<List<string>> GetHistoryAsync()
    {
        try
        {
            var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

            var recentCompletions = await textCompletionAgent.GetRecentCompletionsAsync();

            _logger.LogInformation("获取补全历史成功，返回{Count}条记录", recentCompletions.Count);
            return recentCompletions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取补全历史失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 清空补全历史记录
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpDelete("history")]
    [Authorize]
    public async Task<IActionResult> ClearHistoryAsync()
    {
        try
        {
            var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

            var success = await textCompletionAgent.ClearHistoryAsync();

            if (success)
            {
                _logger.LogInformation("清空补全历史成功");
                return Ok(new { message = "历史记录已清空", success = true });
            }
            else
            {
                _logger.LogWarning("清空补全历史失败");
                return BadRequest(new { message = "清空历史记录失败", success = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空补全历史时发生错误");
            return StatusCode(500, new { message = $"服务器错误: {ex.Message}", success = false });
        }
    }

    /// <summary>
    /// 获取Agent描述信息
    /// </summary>
    /// <returns>Agent功能描述</returns>
    [HttpGet("description")]
    [Authorize]
    public async Task<string> GetDescriptionAsync()
    {
        try
        {
            var agentId = Guid.NewGuid();
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

            var description = await textCompletionAgent.GetDescriptionAsync();
            
            _logger.LogDebug("获取Agent描述成功");
            return description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Agent描述失败");
            return $"{{\"error\": \"获取描述失败: {ex.Message}\"}}";
        }
    }
} 