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

        try
        {
            string userMessage = string.IsNullOrWhiteSpace(inputText)
                ? "Please generate the text completions as instructed."
                : inputText;

            Logger.LogDebug("Sending user message to AI service: {Message}", userMessage);

            var aiResult = await CallAIForCompletionAsync(userMessage);

            var completions = ParseCompletionResult(aiResult);

            Logger.LogInformation("Text completion generation completed, generated {Count} options", completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during text completion generation, input text: {InputText}", inputText);

            return new List<string>();
        }
    }

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
            return CleanJsonContent(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while calling AI service");
            return GetFallbackCompletionJson($"AI service error: {ex.Message}");
        }
    }

    private List<string> ParseCompletionResult(string aiResponse)
    {
        try
        {
            var cleaned = CleanJsonContent(aiResponse);
            var json = JObject.Parse(cleaned);
            var completionsArray = json["completions"] as JArray;

            if (completionsArray == null || !completionsArray.Any())
            {
                Logger.LogWarning("Missing completions array in AI response");
                return new List<string> { "", "", "", "", "" };
            }

            var completions = new List<string>();

            foreach (var item in completionsArray)
            {
                var completion = item?.ToString();
                if (!string.IsNullOrWhiteSpace(completion))
                {
                    completions.Add(completion);
                }
            }

            while (completions.Count < 5)
            {
                completions.Add("");
            }

            return completions.Take(5).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while parsing AI completion result: {Error}", ex.Message);
            return new List<string> { "", "", "", "", "" };
        }
    }

    private string GetFallbackCompletionJson(string errorMessage)
        => new JObject { ["completions"] = new JArray { "", "", "", "", "" } }.ToString();

    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        var cleaned = jsonContent.Trim();

        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        return cleaned.Trim();
    }
}