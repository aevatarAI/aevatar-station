using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器State - 继承AI状态基类，极简版本
/// </summary>
[GenerateSerializer]
public class TextCompletionState : AIGAgentStateBase
{
    // 不需要保存任何状态，只继承基类即可
}

/// <summary>
/// 文本补全器事件 - 空事件类（无状态服务不需要事件持久化）
/// </summary>
[GenerateSerializer]
public class TextCompletionEvent : StateLogEventBase<TextCompletionEvent>
{
    // 无状态服务，不需要任何事件属性
}

/// <summary>
/// 文本补全器GAgent接口
/// </summary>
public interface ITextCompletionGAgent : IAIGAgent, IStateGAgent<TextCompletionState>
{
    /// <summary>
    /// 根据输入文本生成5个不同的补全结果
    /// </summary>
    Task<List<string>> GenerateCompletionsAsync(string inputText);
}

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
            // 简化版本：直接使用初始化时设置的系统指令
            // 如果 inputText 不为空，则将其作为用户消息发送；否则使用系统指令中已包含的内容
            string userMessage = string.IsNullOrWhiteSpace(inputText) ? "Please generate the text completions as instructed." : inputText;
            
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
            return CleanJsonContent(response);
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

            // 确保至少有5个补全选项
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



    /// <summary>
    /// 清理JSON内容（移除markdown标记等）
    /// </summary>
    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        var cleaned = jsonContent.Trim();

        // 移除markdown代码块标记
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