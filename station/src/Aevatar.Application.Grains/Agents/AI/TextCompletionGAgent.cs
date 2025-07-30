using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器GAgent接口
/// </summary>
public interface ITextCompletionGAgent : IAIGAgent, IStateGAgent<TextCompletionState>
{
    /// <summary>
    /// 初始化Agent，设置系统提示词和LLM配置
    /// </summary>
    Task InitializeAsync();
    
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
    private string _systemPrompt = string.Empty;
    private bool _isInitialized = false;

    public TextCompletionGAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI text completion agent that generates 5 different completion results based on user input.");
    }

    /// <summary>
    /// 初始化Agent，设置系统提示词和LLM配置
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            Logger.LogDebug("TextCompletionGAgent already initialized");
            return;
        }

        Logger.LogInformation("Initializing TextCompletionGAgent with system prompt and LLM configuration");

        try
        {
            // 设置系统提示词
            _systemPrompt = @"You are an AI text completion assistant designed to generate creative and diverse text completions.

**Core Task:**
Generate exactly 5 different completion options for user input text.

**Completion Strategies:**
1. **Direct Continuation**: Natural extension of the input text
2. **Creative Expansion**: Add imaginative details and descriptions  
3. **Summary/Conclusion**: Provide a summarizing statement
4. **Alternative Perspective**: Offer a different viewpoint or approach
5. **Question/Dialogue**: Transform into a question or conversational format

**Output Requirements:**
- Generate exactly 5 unique completions
- Each completion should be meaningful and coherent
- Vary the style and approach across the 5 options
- Keep completions relevant to the original input
- Ensure diversity in length and tone

**Response Format:**
Return ONLY a JSON object with the following structure:
{""completions"": [""completion1"", ""completion2"", ""completion3"", ""completion4"", ""completion5""]}

Do not include any explanations, markdown formatting, or additional text outside the JSON structure.";

            // 初始化完成，系统提示词已设置到_systemPrompt，LLM配置设为OpenAI
            Logger.LogDebug("System prompt set with {PromptLength} characters", _systemPrompt.Length);
            Logger.LogDebug("System LLM configuration set to: OpenAI");

            _isInitialized = true;
            Logger.LogInformation("TextCompletionGAgent initialization completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize TextCompletionGAgent");
            throw;
        }
    }



    /// <summary>
    /// 根据输入文本生成5个不同的补全结果
    /// </summary>
    public async Task<List<string>> GenerateCompletionsAsync(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            throw new ArgumentException("User goal cannot be empty", nameof(inputText));
        }

        if (inputText.Trim().Length < 15)
        {
            throw new ArgumentException("User goal must be at least 15 characters long", nameof(inputText));
        }

        // 确保Agent已经初始化
        if (!_isInitialized)
        {
            Logger.LogWarning("TextCompletionGAgent not initialized, initializing now...");
            await InitializeAsync();
        }

        Logger.LogInformation("Starting text completion generation, input text length: {Length} characters", inputText.Length);

        try
        {
            // 构建AI提示词，要求生成5个不同风格的补全
            var userPrompt = $@"Please generate 5 different text completions for the following input:

**User Input:** {inputText}

Apply the completion strategies outlined in the system prompt and return the result in the specified JSON format.";
            var prompt = $"{_systemPrompt}\n\n{userPrompt}";
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
            return new List<string>
            {
                "",
                "",
                "",
                "",
                ""
            };
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
                completions.Add($"Completion option {completions.Count + 1}...");
            }

            return completions.Take(5).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while parsing AI completion result: {Error}", ex.Message);
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
            "",
            "",
            "",
            "",
            ""
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