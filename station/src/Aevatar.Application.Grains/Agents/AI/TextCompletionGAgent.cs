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
        if (string.IsNullOrWhiteSpace(inputText))
        {
            throw new ArgumentException("Input text cannot be empty", nameof(inputText));
        }

        if (inputText.Trim().Length < 2)
        {
            throw new ArgumentException("Input text must be at least 2 characters long for completion", nameof(inputText));
        }

        Logger.LogInformation("Starting text completion generation, input text length: {Length} characters", inputText.Length);

        try
        {
            // 构建AI提示词，明确要求直接续写补全用户的文本
            var prompt = $@"You are a text completion assistant. Your task is to continue the given incomplete text by adding words that naturally complete it into a full sentence.

**User's Incomplete Text:** {inputText}

**Your Task:** 
Complete this text by adding words directly after it to form complete, natural sentences. Generate 5 different completions.

**Important Rules:**
1. Continue the text DIRECTLY from where it ends - do not add punctuation between original and completion
2. Make the result a complete, grammatically correct sentence 
3. The completion should feel like a natural continuation of the original text
4. Generate exactly 5 different completion options
5. Each completion should result in a meaningful, complete sentence
6. Use different completion approaches and styles

**Examples:**
- Input: ""今天天气很""
  → Completions: [
    ""今天天气很好适合出门运动"",
    ""今天天气很糟糕一直在下雨"", 
    ""今天天气很晴朗阳光充足"",
    ""今天天气很凉爽有微风"",
    ""今天天气很热需要开空调""
  ]

- Input: ""我正在考虑""
  → Completions: [
    ""我正在考虑换一个工作环境"",
    ""我正在考虑这个问题的解决方案"",
    ""我正在考虑是否要买车"",
    ""我正在考虑去哪里旅游"",
    ""我正在考虑学习新的技能""
  ]

- Input: ""请帮我获取""
  → Completions: [
    ""请帮我获取这个文件的最新版本"",
    ""请帮我获取明天的天气预报信息"",
    ""请帮我获取会议的详细安排"",
    ""请帮我获取项目的进度报告"",
    ""请帮我获取用户的反馈数据""
  ]

**Response Format:**
Return ONLY a JSON object: {{""completions"": [""completion1"", ""completion2"", ""completion3"", ""completion4"", ""completion5""]}}

Each completion should be the original text + direct continuation (no punctuation in between).
Return only JSON, no other explanations.";
            Logger.LogDebug("Generated prompt length: {PromptLength} characters", prompt.Length);

            // 调用AI服务生成补全结果
            var aiResult = await CallAIForCompletionAsync(prompt);
            
            // 解析AI返回的结果
            var completions = ParseCompletionResult(aiResult);
            
            // 记录日志即可，不需要保存状态
            Logger.LogDebug("Text completion completed successfully, generated {Count} options", completions.Count);

            Logger.LogInformation("Text completion generation completed, generated {Count} options", completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during text completion generation, input text: {InputText}", inputText);
            
            // 返回空字符串作为回退补全结果
            return new ();
        }
    }

    /// <summary>
    /// 调用AI服务进行补全生成
    /// </summary>
    private async Task<string> CallAIForCompletionAsync(string prompt)
    {
        try
        {
            Logger.LogDebug("Sending prompt to AI service, length: {Length} characters", prompt.Length);

            // 调用真正的AI服务
            var chatResult = await ChatWithHistory(prompt);

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