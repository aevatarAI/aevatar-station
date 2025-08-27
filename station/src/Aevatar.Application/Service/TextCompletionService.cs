using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IOptionsMonitor<AIServicePromptOptions> _promptOptions;

    public TextCompletionService(
        IClusterClient clusterClient,
        ILogger<TextCompletionService> logger,
        IUserAppService userAppService,
        IGAgentFactory gAgentFactory,
        IOptionsMonitor<AIServicePromptOptions> promptOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _userAppService = userAppService;
        _gAgentFactory = gAgentFactory;
        _promptOptions = promptOptions;
    }

    public async Task<TextCompletionResponseDto> GenerateCompletionsAsync(TextCompletionRequestDto request)
    {
        // 为每次请求创建新的agent实例，避免并发冲突  
        var agentId = Guid.NewGuid();
        
        using var scope = _logger.BeginScope("AgentId: {AgentId}, RequestInputLength: {InputLength}", agentId, request.UserGoal?.Length ?? 0);
        
        try
        {
            // 验证用户目标长度
            ValidateUserGoal(request.UserGoal ?? string.Empty);
            
            _logger.LogInformation("Starting text completion generation with request: {@Request}", 
                new { 
                    UserGoalLength = request.UserGoal.Length, 
                    UserGoalPreview = request.UserGoal.Length > 100 ? request.UserGoal.Substring(0, 100) + "..." : request.UserGoal 
                });

            var textCompletionAgent = await _gAgentFactory.GetGAgentAsync<ITextCompletionGAgent>(agentId);
            
            // 构建完整的系统指令（包含用户输入、规则、示例等）
            var systemInstructions = BuildTextCompletionSystemInstructions(request.UserGoal);
            
            // AIGAgent需要先初始化才能使用（设置系统提示词和LLM配置）
            var initializeDto = new InitializeDto()
            {
                Instructions = systemInstructions,
                LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" },
            };
            
            _logger.LogDebug("Initializing TextCompletionGAgent with complete prompt. Instructions length: {InstructionsLength}", systemInstructions.Length);
            await textCompletionAgent.InitializeAsync(initializeDto);
            
            // 调用agent生成补全（传递空字符串，因为所有信息都已经在系统指令中了）
            _logger.LogDebug("Calling agent to generate completions");
            var completions = await textCompletionAgent.GenerateCompletionsAsync(string.Empty);

            // 构建成功响应
            var response = new TextCompletionResponseDto
            {
                Completions = completions
            };

            _logger.LogInformation("Text completion generated successfully: {@CompletionResult}", 
                new { 
                    CompletionCount = completions.Count,
                    CompletionLengths = completions.Select(c => c.Length).ToList(),
                    FirstCompletionPreview = completions.FirstOrDefault()?.Length > 50 ? 
                        completions.First().Substring(0, 50) + "..." : 
                        completions.FirstOrDefault()
                });
            
            return response;
        }
        catch (UserFriendlyException)
        {
            // 用户友好异常直接抛出，不处理
            throw;
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

    /// <summary>
    /// 构建文本补全的完整系统指令（包含用户输入、规则、示例等）
    /// </summary>
    private string BuildTextCompletionSystemInstructions(string userInput)
    {
        try
        {
            _logger.LogDebug("Building complete text completion system instructions for input length: {InputLength}", userInput.Length);

            var promptBuilder = new StringBuilder();

            // 1. 系统角色定义
            promptBuilder.AppendLine(_promptOptions.CurrentValue.TextCompletionSystemRole);
            promptBuilder.AppendLine();

            // 2. 任务指令（包含用户输入）
            var taskInstructions = _promptOptions.CurrentValue.TextCompletionTaskTemplate
                .Replace("{USER_INPUT}", userInput);
            promptBuilder.AppendLine(taskInstructions);
            promptBuilder.AppendLine();

            // 3. 重要规则
            promptBuilder.AppendLine(_promptOptions.CurrentValue.TextCompletionImportantRules);
            promptBuilder.AppendLine();

            // 4. 示例
            promptBuilder.AppendLine(_promptOptions.CurrentValue.TextCompletionExamples);
            promptBuilder.AppendLine();

            // 5. 输出格式要求
            promptBuilder.AppendLine(_promptOptions.CurrentValue.TextCompletionOutputRequirements);

            var systemInstructions = promptBuilder.ToString();
            _logger.LogDebug("Built complete text completion system instructions with length: {PromptLength}", systemInstructions.Length);

            return systemInstructions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building text completion system instructions");
            // 返回基础的提示词作为后备
            return _promptOptions.CurrentValue.TextCompletionSystemRole + "\n\n" + _promptOptions.CurrentValue.TextCompletionOutputRequirements;
        }
    }

    /// <summary>
    /// 验证用户目标的长度限制
    /// </summary>
    /// <param name="userGoal">用户目标文本</param>
    /// <exception cref="UserFriendlyException">当用户目标不符合长度要求时抛出</exception>
    private void ValidateUserGoal(string userGoal)
    {
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            _logger.LogWarning("UserGoal validation failed: input is null or empty");
            throw new UserFriendlyException("User goal cannot be empty.");
        }

        if (userGoal.Length < 15)
        {
            _logger.LogWarning("UserGoal validation failed: input too short. Length: {InputLength}, Required minimum: 15", userGoal.Length);
            throw new UserFriendlyException("User goal must be at least 15 characters long.");
        }

        if (userGoal.Length > 250)
        {
            _logger.LogWarning("UserGoal validation failed: input too long. Length: {InputLength}, Maximum allowed: 250", userGoal.Length);
            throw new UserFriendlyException("User goal cannot exceed 250 characters.");
        }
    }
}