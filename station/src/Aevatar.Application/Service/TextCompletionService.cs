using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

/// <summary>
/// 文本补全服务实现
/// </summary>
public class TextCompletionService : ApplicationService, ITextCompletionService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TextCompletionService> _logger;
    private readonly IUserAppService _userAppService;

    public TextCompletionService(
        IClusterClient clusterClient,
        ILogger<TextCompletionService> logger,
        IUserAppService userAppService)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _userAppService = userAppService;
    }

    public async Task<TextCompletionResponseDto> GenerateCompletionsAsync(TextCompletionRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting text completion generation for user goal length: {Length}", request.UserGoal.Length);

            // Service层验证：用户目标至少需要15个字符
            if (string.IsNullOrWhiteSpace(request.UserGoal) || request.UserGoal.Trim().Length < 15)
            {
                _logger.LogWarning("User goal validation failed: length {Length} is less than required 15 characters", request.UserGoal?.Length ?? 0);
                throw new UserFriendlyException("Please enter at least 15 characters for the user goal to generate more accurate completion suggestions.");
            }

            // 根据当前用户生成agentId
            var currentUserId = _userAppService.GetCurrentUserId();
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(currentUserId);

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