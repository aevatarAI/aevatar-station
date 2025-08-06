using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器GAgent - 简化版，返回5个补全字符串
/// </summary>
[GAgent("TextCompletion")]
public class TextCompletionGAgent : AIGAgentBase<TextCompletionState, TextCompletionEvent>, ITextCompletionGAgent
{
    public TextCompletionGAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI text completion agent that generates 5 different completion results based on user input.");
    }
    
    /// <summary>
    /// 根据输入文本生成5个不同的补全结果
    /// </summary>
    public async Task<List<string>> GenerateCompletionsAsync(string inputText)
    {
        Logger.LogInformation("Starting text completion generation, input text length: {Length} characters", inputText?.Length ?? 0);

        try
        {
            // 使用AiAgentHelper标准化用户输入
            string userMessage = AiAgentHelper.NormalizeUserInput(inputText, "Please generate the text completions as instructed.");
            
            Logger.LogDebug("Sending user message to AI service: {Message}", userMessage);

            // 调用AI服务生成补全结果
            var aiResult = await CallAIForCompletionAsync(userMessage);
            
            // 解析AI返回的结果
            var completions = ParseCompletionResult(aiResult);
            
            Logger.LogInformation("Text completion generation completed, generated {Count} options", completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during text completion generation, input text: {InputText}", inputText);
            
            // 返回空列表作为回退补全结果
            return new List<string>();
        }
    }

    /// <summary>
    /// 调用AI服务进行补全生成
    /// </summary>
    private async Task<string> CallAIForCompletionAsync(string userMessage)
    {
        try
        {
            Logger.LogDebug("Sending user message to AI service, length: {Length} characters", userMessage.Length);

            // 调用真正的AI服务
            var chatResult = await ChatWithHistory(userMessage);

            if (chatResult == null || !chatResult.Any())
            {
                Logger.LogWarning("AI service returned null or empty result for text completion");
                return GetFallbackCompletionJson("AI service returned empty result");
            }

            var response = chatResult[0].Content;
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogWarning("AI returned empty content for text completion");
                return GetFallbackCompletionJson("AI service returned empty content");
            }

            Logger.LogDebug("AI completion response received, length: {Length} characters", response.Length);
            return AiAgentHelper.CleanJsonContent(response, GetFallbackCompletionJson("Invalid JSON content"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while calling AI service");
            return GetFallbackCompletionJson($"AI service error: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析AI返回的补全结果
    /// </summary>
    private List<string> ParseCompletionResult(string aiResponse)
    {
        try
        {
            // 使用AiAgentHelper安全解析JSON
            var json = AiAgentHelper.SafeParseJson(aiResponse);
            
            if (json == null)
            {
                Logger.LogWarning("Failed to parse AI response as JSON");
                return new List<string> { "", "", "", "", "" };
            }

            // 使用AiAgentHelper安全获取字符串数组
            var completionsArray = AiAgentHelper.SafeGetStringArray(json, "completions", 5);
            
            return completionsArray.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while parsing AI completion result: {Error}", ex.Message);
            return new List<string> { "", "", "", "", "" };
        }
    }

    /// <summary>
    /// 获取回退的补全JSON
    /// </summary>
    private string GetFallbackCompletionJson(string errorMessage)
    {
        var fallbackJson = new JObject
        {
            ["completions"] = new JArray
            {
                "",
                "",
                "",
                "",
                ""
            }
        };

        return fallbackJson.ToString();
    }
}