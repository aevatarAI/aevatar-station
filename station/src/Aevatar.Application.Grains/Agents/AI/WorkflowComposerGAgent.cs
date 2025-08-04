using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.Common;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Aevatar.Service;
using Aevatar.Application.Contracts.WorkflowOrchestration;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 工作流组合器State - 继承AI状态基类
/// </summary>
[GenerateSerializer]
public class WorkflowComposerState : AIGAgentStateBase
{
    // AI状态基类已包含必要的AI功能状态
}

/// <summary>
/// 工作流组合器事件
/// </summary>
[GenerateSerializer]
public class WorkflowComposerEvent : StateLogEventBase<WorkflowComposerEvent>
{
    // 简单事件实现
}

/// <summary>
/// 工作流组合器GAgent接口
/// </summary>
public interface IWorkflowComposerGAgent : IAIGAgent, IStateGAgent<WorkflowComposerState>
{
    /// <summary>
    /// 根据用户目标生成完整的工作流JSON（Service层已构造好prompt）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal);
}

/// <summary>
/// 工作流组合器GAgent - 精简的AI工作流生成器
/// Service层负责构造prompt和组织agent信息，此处只调用AI
/// </summary>
[GAgent("WorkflowComposer")]
public class WorkflowComposerGAgent : AIGAgentBase<WorkflowComposerState, WorkflowComposerEvent>,
    IWorkflowComposerGAgent
{
    public WorkflowComposerGAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        var descriptionInfo = new AiWorkflowAgentInfoDto
        {
            Name = "WorkflowComposer",
            Type = "Aevatar.Application.Grains.Agents.AI.WorkflowComposerGAgent",
            Description = "AI workflow generation agent that creates complete workflow JSON from user goals and available agent descriptions. Analyzes user goals and available agent capabilities to generate optimized workflow configurations with proper node connections and data flow management."
        };
        
        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    /// <summary>
    /// 根据用户目标生成工作流JSON
    /// 系统提示词已在InitializeAsync中设置，这里只传入用户目标
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal)
    {
        Logger.LogInformation("Starting workflow generation for goal: {UserGoal}", userGoal);

        try
        {
            // 直接调用AI生成工作流，底层会自动组合系统提示词+用户目标
            var aiResult = await CallAIForWorkflowGenerationAsync(userGoal);
            Logger.LogInformation("Successfully generated workflow JSON with length: {ResultLength}", aiResult.Length);

            return aiResult;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating workflow JSON for goal: {UserGoal}", userGoal);
            throw;
        }
    }

    /// <summary>
    /// 调用AI服务生成工作流
    /// </summary>
    private async Task<string> CallAIForWorkflowGenerationAsync(string userGoal)
    {
        try
        {
            Logger.LogDebug("Sending user goal to AI service: {UserGoal}", userGoal);

            // 调用AIGAgent基类的聊天方法，底层会自动处理系统提示词+用户目标的组合
            var chatResult = await ChatWithHistory(userGoal);

            if (chatResult == null || !chatResult.Any())
            {
                Logger.LogWarning("AI service returned null or empty result for workflow generation");
                return GetFallbackWorkflowJson("ai_service_empty", "AI service returned empty result");
            }

            var response = chatResult[0].Content;
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogWarning("AI returned empty content for workflow generation");
                return GetFallbackWorkflowJson("ai_empty_content", "AI service returned empty content");
            }

            Logger.LogDebug("AI workflow response received, length: {Length} characters", response.Length);
            return CleanJsonContent(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while calling AI service for workflow generation");
            return GetFallbackWorkflowJson("ai_service_error", $"AI service error: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证AI响应是否包含必需的错误处理字段
    /// </summary>
    private bool IsValidErrorHandlingResponse(string response)
    {
        try
        {
            // 先清理JSON内容，移除markdown代码块标记
            var cleanedResponse = CleanJsonContent(response);
            var json = JObject.Parse(cleanedResponse);
            var hasGenerationStatus = json.ContainsKey("generationStatus");
            var hasClarityScore = json.ContainsKey("clarityScore");
            var hasErrorInfo = json.ContainsKey("errorInfo");
            
            Logger.LogDebug("AI response validation - generationStatus: {HasGenerationStatus}, clarityScore: {HasClarityScore}, errorInfo: {HasErrorInfo}", 
                hasGenerationStatus, hasClarityScore, hasErrorInfo);
            
            if (!hasGenerationStatus || !hasClarityScore || !hasErrorInfo)
            {
                Logger.LogDebug("Missing required fields. Response top-level keys: {Keys}", 
                    string.Join(", ", json.Properties().Select(p => p.Name)));
            }
            
            return hasGenerationStatus && hasClarityScore && hasErrorInfo;
        }
        catch (JsonReaderException ex)
        {
            Logger.LogWarning(ex, "Failed to parse AI response for validation: {JsonError}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 包装旧版本AI响应，添加错误处理字段
    /// </summary>
    private string WrapLegacyResponse(string legacyResponse)
    {
        try
        {
            // 先清理JSON内容，移除markdown代码块标记
            var cleanedResponse = CleanJsonContent(legacyResponse);
            var legacyJson = JObject.Parse(cleanedResponse);
            
            // Create enhanced response with error handling fields
            var enhancedResponse = new JObject
            {
                ["generationStatus"] = "success",
                ["clarityScore"] = 4, // Assume good clarity if AI generated a response
                ["name"] = legacyJson["name"] ?? "Generated Workflow",
                ["properties"] = legacyJson["properties"] ?? new JObject(),
                ["errorInfo"] = null, // No errors for successful legacy response
                ["completionPercentage"] = 100,
                ["completionGuidance"] = null
            };

            return enhancedResponse.ToString();
        }
        catch (JsonReaderException ex)
        {
            // 详细记录JSON解析失败的信息
            Logger.LogError(ex, "Failed to parse AI response as JSON. Error: {JsonError}", ex.Message);
            Logger.LogError("Invalid JSON content (first 500 chars): {InvalidContent}", 
                legacyResponse.Length > 500 ? legacyResponse.Substring(0, 500) + "..." : legacyResponse);
            Logger.LogError("Full invalid JSON response length: {Length}", legacyResponse.Length);
            
            // If legacy response is not valid JSON, treat as system error
            return GetFallbackWorkflowJson("system_error", "AI returned invalid JSON format",
                new[] { "Retry workflow generation", "Simplify your goal description", "Use manual workflow creation" });
        }
    }

    /// <summary>
    /// 获取回退工作流JSON（当AI生成失败时使用）
    /// </summary>
    private string GetFallbackWorkflowJson(string errorType = "system_error", string errorMessage = "AI service unavailable", string[]? actionableSteps = null)
    {
        var fallbackJson = new JObject
        {
            ["generationStatus"] = "system_fallback",
            ["clarityScore"] = 0, // Cannot assess clarity in fallback mode
            ["name"] = "Fallback Workflow",
            ["properties"] = new JObject
            {
                ["name"] = "Fallback Workflow",
                ["workflowNodeList"] = new JArray
                {
                    new JObject
                    {
                        ["nodeId"] = "fallback-node-1",
                        ["nodeName"] = "Manual Creation Node",
                        ["nodeType"] = "ManualAgent",
                        ["extendedData"] = new JObject
                        {
                            ["description"] = "Manual processing node - workflow generation failed"
                        },
                        ["properties"] = new JObject
                        {
                            ["message"] = errorMessage,
                            ["errorType"] = errorType,
                            ["actionRequired"] = true
                        }
                    }
                },
                ["workflowNodeUnitList"] = new JArray()
            },
            ["errorInfo"] = new JObject
            {
                ["errorType"] = errorType,
                ["errorMessage"] = errorMessage,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["actionableSteps"] = actionableSteps != null ? new JArray(actionableSteps) : new JArray(
                    "Review the user goal for clarity",
                    "Check AI service availability", 
                    "Try again with a simpler request",
                    "Contact support if the issue persists"
                )
            },
            ["completionPercentage"] = 0,
            ["completionGuidance"] = "Please review the error information and take the suggested actions to resolve the issue."
        };

        return fallbackJson.ToString();
    }

    /// <summary>
    /// 清理JSON内容（移除markdown标记等）
    /// </summary>
    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return GetFallbackWorkflowJson("empty_content", "AI returned empty content");

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