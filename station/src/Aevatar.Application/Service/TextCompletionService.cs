using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

/// <summary>
/// 文本补全服务实现
/// </summary>
public class TextCompletionService : ApplicationService, ITextCompletionService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TextCompletionService> _logger;

    public TextCompletionService(
        IClusterClient clusterClient,
        ILogger<TextCompletionService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    /// <summary>
    /// 生成文本补全
    /// </summary>
    /// <param name="request">补全请求</param>
    /// <returns>补全结果</returns>
    public async Task<TextCompletionResponseDto> GenerateCompletionsAsync(TextCompletionRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting text completion generation for user goal length: {Length}", request.UserGoal.Length);

            // 获取TextCompletionGAgent实例
            var agentId = new Guid("12345678-1234-1234-1234-123456789abc");
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);

            // 调用agent生成补全
            var completions = await textCompletionAgent.GenerateCompletionsAsync(request.UserGoal);

            // 构建成功响应
            var response = new TextCompletionResponseDto
            {
                UserGoal = request.UserGoal,
                Completions = completions,
                Success = true
            };

            _logger.LogInformation("Text completion generated successfully, returned {Count} options", completions.Count);
            return response;
        }
        catch (ArgumentException ex)
        {
            // 输入验证错误
            _logger.LogWarning("User goal validation failed: {Message}", ex.Message);
            return new TextCompletionResponseDto
            {
                UserGoal = request.UserGoal,
                Completions = new List<string>(),
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            // 其他系统错误
            _logger.LogError(ex, "Failed to generate text completion");
            return new TextCompletionResponseDto
            {
                UserGoal = request.UserGoal,
                Completions = new List<string>(),
                Success = false,
                ErrorMessage = $"Failed to generate completion: {ex.Message}"
            };
        }
    }
} 