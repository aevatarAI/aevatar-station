using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Aevatar.Application.Grains.Agents.AI;

[GAgent("TextCompletion")]
public class TextCompletionGAgent : AIGAgentBase<TextCompletionState, TextCompletionEvent>, ITextCompletionGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "AI text completion agent that generates 5 different completion results based on user input.");
    }

    public async Task<List<string>> GenerateCompletionsAsync(string inputText)
    {
        Logger.LogInformation("Starting text completion generation, input text length: {Length} characters",
            inputText?.Length ?? 0);

        var userMessage = string.IsNullOrWhiteSpace(inputText)
            ? "Please generate the text completions as instructed."
            : inputText;

        Logger.LogDebug("Sending user message to AI service: {Message}", userMessage);
        var aiResult = await CallAIForCompletionAsync(userMessage);
        var completions = ParseCompletionResult(aiResult);

        Logger.LogInformation("Text completion generation completed, generated {Count} options", completions.Count);
        return completions;
    }

    private async Task<string> CallAIForCompletionAsync(string userMessage)
    {
        try
        {
            Logger.LogDebug("Sending user message to AI service, length: {Length} characters", userMessage.Length);
            var chatResult = await ChatWithHistory(userMessage);

            // 使用AiAgentHelper统一处理AI响应
            return AiAgentHelper.ProcessAiChatResult(chatResult, Logger, GetFallbackCompletionJson, "text completion");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while calling AI service");
            return GetFallbackCompletionJson($"AI service error: {ex.Message}");
        }
    }

    private List<string> ParseCompletionResult(string aiResponse)
    {
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