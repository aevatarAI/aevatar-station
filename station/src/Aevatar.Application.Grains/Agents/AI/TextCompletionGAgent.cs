using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器GAgent - 简化版，返回5个补全字符串
/// </summary>
[GAgent("TextCompletion")]
public class TextCompletionGAgent : GAgentBase<TextCompletionState, TextCompletionEvent>
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
            throw new ArgumentException("输入文本不能为空", nameof(inputText));
        }

        Logger.LogInformation("开始生成文本补全，输入文本长度: {Length}字符", inputText.Length);

        try
        {
            // 构建AI提示词，要求生成5个不同风格的补全
            var prompt = BuildCompletionPrompt(inputText);
            Logger.LogDebug("生成的提示词长度: {PromptLength}字符", prompt.Length);

            // 调用AI服务生成补全结果
            var aiResult = await CallAIForCompletionAsync(prompt);
            
            // 解析AI返回的结果
            var completions = ParseCompletionResult(aiResult);
            
            // 更新状态
            await UpdateStateAfterCompletion(completions);

            // 记录事件
            await LogCompletionEvent(inputText, completions.Count);

            Logger.LogInformation("文本补全生成完成，生成了 {Count} 个选项", completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "生成文本补全时发生错误，输入文本: {InputText}", inputText);
            
            // 返回回退补全结果
            return new List<string>
            {
                inputText + "...",
                inputText + "（请稍后重试）",
                inputText + "（AI服务暂时不可用）",
                inputText + "（正在处理中）",
                inputText + "（系统繁忙）"
            };
        }
    }

    /// <summary>
    /// 获取最近的补全历史记录
    /// </summary>
    public async Task<List<string>> GetRecentCompletionsAsync()
    {
        var state = await GetStateAsync();
        return state.RecentCompletions.ToList();
    }

    /// <summary>
    /// 清空补全历史记录
    /// </summary>
    public async Task<bool> ClearHistoryAsync()
    {
        try
        {
            var clearEvent = new TextCompletionEvent
            {
                EventType = "HistoryCleared",
                InputText = "",
                CompletionCount = 0
            };
            
            RaiseEvent(clearEvent);
            await ConfirmEvents();

            Logger.LogInformation("补全历史记录已清空");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "清空补全历史记录时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 构建文本补全的AI提示词
    /// </summary>
    private string BuildCompletionPrompt(string inputText)
    {
        return $@"
Generate 5 different text completions for the following input:

Input text: {inputText}

Requirements:
1. Generate 5 completions with different styles
2. Each completion should be natural and coherent
3. Include various completion strategies: continuation, expansion, summary, rewriting, creative extension

Return in JSON format with only the completion texts array:
{{""completions"": [""completion1"", ""completion2"", ""completion3"", ""completion4"", ""completion5""]}}

Return only JSON, no other explanations.
";
    }

    /// <summary>
    /// 调用AI服务进行补全生成（模拟实现）
    /// </summary>
    private async Task<string> CallAIForCompletionAsync(string prompt)
    {
        try
        {
            Logger.LogDebug("正在调用AI服务进行文本补全...");
            
            // TODO: 这里应该调用实际的AI服务
            // 目前返回模拟数据
            await Task.Delay(100); // 模拟AI调用延迟
            
            var fallbackJson = GetFallbackCompletionJson("模拟AI响应");
            Logger.LogDebug("AI补全响应长度: {Length}字符", fallbackJson.Length);
            return fallbackJson;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "调用AI服务时发生错误");
            return GetFallbackCompletionJson($"AI服务错误: {ex.Message}");
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
                Logger.LogWarning("AI响应中缺少completions数组");
                return GetFallbackCompletions();
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
                completions.Add($"补全选项 {completions.Count + 1}...");
            }

            return completions.Take(5).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "解析AI补全结果时发生错误: {Error}", ex.Message);
            return GetFallbackCompletions();
        }
    }

    /// <summary>
    /// 获取回退补全结果
    /// </summary>
    private List<string> GetFallbackCompletions()
    {
        return new List<string>
        {
            "补全选项1：继续写作...",
            "补全选项2：总结以上内容。",
            "补全选项3：换个角度来看，",
            "补全选项4：这意味着",
            "补全选项5：总的来说，"
        };
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
                "继续写作...",
                "总结以上内容。",
                "换个角度来看，",
                "这意味着",
                "总的来说，"
            }
        };

        return fallbackJson.ToString();
    }

    /// <summary>
    /// 补全完成后更新状态
    /// </summary>
    private async Task UpdateStateAfterCompletion(List<string> completions)
    {
        var completionEvent = new TextCompletionEvent
        {
            EventType = "TextCompletion",
            InputText = "用户输入",
            CompletionCount = completions.Count
        };
        
        RaiseEvent(completionEvent);
        await ConfirmEvents();
    }

    /// <summary>
    /// 记录补全事件
    /// </summary>
    private async Task LogCompletionEvent(string inputText, int completionCount)
    {
        var completionEvent = new TextCompletionEvent
        {
            EventType = "TextCompletion",
            InputText = inputText,
            CompletionCount = completionCount
        };
        
        RaiseEvent(completionEvent);
        await ConfirmEvents();
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

    /// <summary>
    /// 状态转换方法 - 处理事件对状态的影响
    /// </summary>
    protected override void GAgentTransitionState(TextCompletionState state, StateLogEventBase<TextCompletionEvent> @event)
    {
        if (@event is TextCompletionEvent completionEvent)
        {
            switch (completionEvent.EventType)
            {
                case "TextCompletion":
                    state.TotalCompletions++;
                    // 保留最近的补全记录
                    if (state.RecentCompletions.Count >= 10)
                    {
                        state.RecentCompletions.RemoveAt(0);
                    }
                    state.RecentCompletions.Add($"[{DateTime.UtcNow:HH:mm:ss}] {completionEvent.InputText}");
                    break;
                    
                case "HistoryCleared":
                    state.RecentCompletions.Clear();
                    state.TotalCompletions = 0;
                    break;
            }
        }
    }
} 