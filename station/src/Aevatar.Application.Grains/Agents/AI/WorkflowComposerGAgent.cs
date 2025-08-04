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
using Aevatar.Application.Service;

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
    /// 根据用户目标生成完整的工作流JSON（接受新的AiWorkflowAgentInfoDto信息）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AiWorkflowAgentInfoDto> availableAgents);
}

/// <summary>
/// 工作流组合器GAgent - 精简的AI工作流生成器（接受AiWorkflowAgentInfoDto信息）
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
            Id = "Aevatar.Application.Grains.Agents.AI.WorkflowComposerGAgent",
            Name = "WorkflowComposer",
            Category = "AI",
            L1Description = "AI workflow generation agent that creates complete workflow JSON from user goals and available agent descriptions.",
            L2Description = "Advanced AI agent specialized in workflow orchestration. Analyzes user goals and available agent capabilities to generate optimized workflow configurations with proper node connections and data flow management.",
            Capabilities = new List<string> { "workflow-generation", "agent-orchestration", "json-creation", "ai-analysis" },
            Tags = new List<string> { "workflow", "orchestration", "ai-generation", "json" }
        };
        
        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    /// <summary>
    /// 根据用户目标和Agent描述信息生成工作流JSON
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AiWorkflowAgentInfoDto> availableAgents)
    {
        Logger.LogInformation("Starting workflow generation for goal: {UserGoal} with {AgentCount} available agents", 
            userGoal, availableAgents.Count);

        // 添加详细的调试日志记录输入参数
        Logger.LogInformation("=== Workflow Generation Input Debug Info ===");
        Logger.LogInformation("User Goal: {UserGoal}", userGoal);
        Logger.LogInformation("Available Agents Count: {AgentCount}", availableAgents.Count);
        
        if (availableAgents.Any())
        {
            Logger.LogInformation("Agent Details:");
            foreach (var agent in availableAgents)
            {
                Logger.LogInformation("  - Agent: {AgentName} (ID: {AgentId})", agent.Name, agent.Id);
                Logger.LogInformation("    L1Description: {L1Description}", agent.L1Description);
                Logger.LogInformation("    L2Description: {L2Description}", agent.L2Description);
                Logger.LogInformation("    Category: {Category}", agent.Category);
                if (agent.Capabilities?.Any() == true)
                    Logger.LogInformation("    Capabilities: {Capabilities}", string.Join(", ", agent.Capabilities));
                if (agent.Tags?.Any() == true)
                    Logger.LogInformation("    Tags: {Tags}", string.Join(", ", agent.Tags));
            }
        }
        else
        {
            Logger.LogWarning("No available agents provided for workflow generation!");
        }
        Logger.LogInformation("============================================");

        try
        {
            // 直接使用AiWorkflowAgentInfoDto生成Prompt
            var prompt = BuildPromptFromAiWorkflowAgentInfoDto(userGoal, availableAgents);
            Logger.LogDebug("Generated prompt with length: {PromptLength}", prompt.Length);

            // 调用AI生成工作流
            var aiResult = await CallAIForWorkflowGenerationAsync(prompt);
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
    /// 基于AiWorkflowAgentInfoDto构建AI提示词
    /// </summary>
    private string BuildPromptFromAiWorkflowAgentInfoDto(string userGoal, List<AiWorkflowAgentInfoDto> agentList)
    {
        var prompt = new StringBuilder();

        // System role definition
        prompt.AppendLine("# Advanced Workflow Orchestration Expert");
        prompt.AppendLine("You are an advanced AI workflow orchestration expert. Based on user goals, analyze available Agent capabilities and design a complete workflow execution plan with proper node connections and data flow.");
        prompt.AppendLine();

        // User goal
        prompt.AppendLine("## User Goal");
        prompt.AppendLine($"{userGoal}");
        prompt.AppendLine();

        // Available Agent list with rich information
        prompt.AppendLine("## Available Agent Catalog");
        if (agentList.Any())
        {
            foreach (var agent in agentList)
            {
                prompt.AppendLine($"### {agent.Name}");
                prompt.AppendLine($"**Type**: {agent.Id}");
                prompt.AppendLine($"**Quick Description**: {agent.L1Description}");
                prompt.AppendLine($"**Detailed Capabilities**: {agent.L2Description}");
                prompt.AppendLine($"**Category**: {agent.Category}");
                
                if (agent.Tags?.Any() == true)
                    prompt.AppendLine($"**Tags**: {string.Join(", ", agent.Tags)}");
                
                if (agent.Capabilities?.Any() == true)
                {
                    prompt.AppendLine($"**Capabilities**: {string.Join(", ", agent.Capabilities)}");
                }
                
                prompt.AppendLine();
            }
        }
        else
        {
            prompt.AppendLine("No available Agents found");
            prompt.AppendLine();
        }

        // Enhanced output requirements
        prompt.AppendLine("## Advanced Output Requirements");
        prompt.AppendLine("Please analyze the user goal and available agents to create an optimized workflow:");
        prompt.AppendLine("1) **Agent Selection**: Choose the most suitable agents based on capabilities and categories");
        prompt.AppendLine("2) **Node Design**: Create workflow nodes with proper configuration");
        prompt.AppendLine("3) **Connection Logic**: Define execution order and data flow between nodes");
        prompt.AppendLine("4) **Error Handling**: Consider failure scenarios and alternative paths");
        prompt.AppendLine("5) **Performance**: Optimize for execution time and resource usage");
        prompt.AppendLine();

        // JSON format specification
        prompt.AppendLine("## JSON Format Specification");
        prompt.AppendLine("Please strictly follow the following JSON format output:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"name\": \"User Goal Workflow\",");
        prompt.AppendLine("  \"properties\": {");
        prompt.AppendLine("    \"name\": \"User Goal Workflow\",");
        prompt.AppendLine("    \"workflowNodeList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"nodeId\": \"unique_node_id\",");
        prompt.AppendLine("        \"nodeName\": \"descriptive_node_name\",");
        prompt.AppendLine("        \"nodeType\": \"agent_type_name\",");
        prompt.AppendLine("        \"extendedData\": {");
        prompt.AppendLine("          \"description\": \"node_purpose_description\"");
        prompt.AppendLine("        },");
        prompt.AppendLine("        \"properties\": {");
        prompt.AppendLine("          \"config_key\": \"config_value\"");
        prompt.AppendLine("        }");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ],");
        prompt.AppendLine("    \"workflowNodeUnitList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"fromNodeId\": \"source_node_id\",");
        prompt.AppendLine("        \"toNodeId\": \"target_node_id\",");
        prompt.AppendLine($"        \"connectionType\": \"{ConnectionType.Sequential}\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("**IMPORTANT**: Only output the JSON, no additional text or explanations.");

        return prompt.ToString();
    }

    /// <summary>
    /// 调用AI服务生成工作流
    /// </summary>
    private async Task<string> CallAIForWorkflowGenerationAsync(string prompt)
    {
        try
        {
            // 修复ChatWithHistory调用，提供必要的参数
            // 使用正确的类型来避免编译错误
            var history = new List<ChatMessage>(); // 空聊天历史记录
            ExecutionPromptSettings? promptSettings = null; // 使用默认设置
            var cancellationToken = CancellationToken.None; // 默认取消令牌
            AIChatContextDto? context = null; // 默认上下文
            var imageKeys = new List<string>(); // 空图片列表
            
            // 添加详细的调试日志 - 记录传递给AI的完整prompt
            Logger.LogInformation("=== AI Workflow Generation Debug Info ===");
            Logger.LogInformation("Full prompt being sent to AI:");
            Logger.LogInformation("Prompt content: {PromptContent}", prompt);
            Logger.LogInformation("Prompt length: {PromptLength} characters", prompt.Length);
            Logger.LogInformation("==========================================");
            
            var chatResult = await ChatWithHistory(
                prompt, 
                history, 
                promptSettings, 
                cancellationToken, 
                context, 
                imageKeys);

            if (chatResult == null || !chatResult.Any())
            {
                Logger.LogWarning("AI service returned null or empty result for workflow generation");
                return GetFallbackWorkflowJson("ai_no_response", "AI service returned empty result",
                    new[] { "Check AI service connectivity", "Verify API credentials", "Retry with simpler goal" });
            }

            var response = chatResult[0].Content;
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogWarning("AI returned empty content for workflow generation");
                return GetFallbackWorkflowJson("system_error", "AI service returned empty content",
                    new[] { "Retry with a more specific goal", "Verify AI service is functioning", "Create workflow manually" });
            }

            // 添加详细的调试日志记录AI返回的原始内容
            Logger.LogInformation("=== AI Response Debug Info ===");
            Logger.LogInformation("AI raw response content: {RawResponse}", response);
            Logger.LogInformation("AI response length: {Length} chars", response.Length);
            Logger.LogInformation("===============================");

            // Validate that the response contains required error handling fields
            if (!IsValidErrorHandlingResponse(response))
            {
                Logger.LogWarning("AI response missing required error handling fields, wrapping response. Raw content preview: \"{RawPreview}\"", 
                    response.Length > 100 ? response.Substring(0, 100) + "..." : response);
                return WrapLegacyResponse(response);
            }

            // Clean the response before returning
            var cleanedResponse = CleanJsonContent(response);
            Logger.LogDebug("Cleaned AI response length: {CleanedLength} chars", cleanedResponse.Length);
            
            return cleanedResponse;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during AI workflow generation call");
            return GetFallbackWorkflowJson("system_error", $"AI generation failed: {ex.Message}",
                new[] { "Check system logs for details", "Retry the request", "Contact system administrator" });
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