using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Core.Abstractions;
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
    private readonly IGAgentFactory _gAgentFactory;

    public TextCompletionService(
        IClusterClient clusterClient,
        ILogger<TextCompletionService> logger,
        IUserAppService userAppService,
        IGAgentFactory gAgentFactory)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _userAppService = userAppService;
        _gAgentFactory = gAgentFactory;
    }

    public async Task<TextCompletionResponseDto> GenerateCompletionsAsync(TextCompletionRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting text completion generation for input text length: {Length}",
                request.UserGoal.Length);

            // 为每次请求创建新的agent实例，避免并发冲突  
            var agentId = Guid.NewGuid();
            var textCompletionAgent = await _gAgentFactory.GetGAgentAsync<ITextCompletionGAgent>(agentId);
            
            // AIGAgent需要先初始化才能使用（设置系统提示词和LLM配置）
            await textCompletionAgent.InitializeAsync(new()
            {
                Instructions = "You are a text completion assistant specializing in continuing and completing incomplete text. Your role is to provide natural text continuations, not to answer questions. Focus on understanding the context and flow of the given text, then generate coherent continuations that feel like a natural extension of the original content.",
                LLMConfig = new () { SystemLLM = "OpenAI" },
            });
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