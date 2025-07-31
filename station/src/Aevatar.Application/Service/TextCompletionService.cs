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
            _logger.LogInformation("Starting text completion generation for user goal length: {Length}",
                request.UserGoal.Length);

            // Service层验证：用户目标至少需要15个字符
            if (string.IsNullOrWhiteSpace(request.UserGoal) || request.UserGoal.Trim().Length < 15)
            {
                _logger.LogWarning("User goal validation failed: length {Length} is less than required 15 characters",
                    request.UserGoal?.Length ?? 0);
                throw new UserFriendlyException(
                    "Please enter at least 15 characters for the user goal to generate more accurate completion suggestions.");
            }

            // 为每次请求创建新的agent实例，避免并发冲突
            var agentId = Guid.NewGuid();
            var textCompletionAgent = _clusterClient.GetGrain<ITextCompletionGAgent>(agentId);
            
            // 只激活agent，不使用可能有问题的InitializeAsync
            await textCompletionAgent.ActivateAsync();
            // 调用agent生成补全
            var completions = await textCompletionAgent.GenerateCompletionsAsync(request.UserGoal);

            // 构建成功响应
            var response = new TextCompletionResponseDto
            {
                Completions = completions
            };

            _logger.LogInformation("Text completion generated successfully, returned {Count} options",
                completions.Count);
            return response;
        }
        catch (Exception ex)
        {
            // 其他系统错误
            _logger.LogError(ex, "Failed to generate text completion");
            return new TextCompletionResponseDto
            {
                Completions = new List<string>()
            };
        }
    }
}