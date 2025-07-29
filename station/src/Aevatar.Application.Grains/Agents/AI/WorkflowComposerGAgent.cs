using Aevatar.Core.Abstractions;
using Aevatar.Domain.WorkflowOrchestration;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Reflection;
using Aevatar.Options;
using Microsoft.Extensions.Options;
using Aevatar.Schema;
using Orleans.Runtime;
using Orleans.Metadata;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

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
    /// 根据用户目标生成完整的工作流JSON（接受新的AgentDescriptionInfo信息）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentDescriptionInfo> availableAgents);
}

/// <summary>
/// 工作流组合器GAgent - 精简的AI工作流生成器（接受AgentDescriptionInfo信息）
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
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "Aevatar.Application.Grains.Agents.AI.WorkflowComposerGAgent",
            Name = "WorkflowComposer",
            Category = "AI",
            L1Description =
                "AI workflow generation agent that creates complete workflow JSON from user goals and available agent descriptions.",
            L2Description =
                "Advanced AI agent specialized in workflow orchestration. Analyzes user goals and available agent capabilities to generate optimized workflow configurations with proper node connections and data flow management.",
            Capabilities = new List<string>
                { "workflow-generation", "agent-orchestration", "json-creation", "ai-analysis" },
            Tags = new List<string> { "workflow", "orchestration", "ai-generation", "json" }
        };

        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    /// <summary>
    /// 根据用户目标和Agent描述信息生成工作流JSON
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentDescriptionInfo> availableAgents)
    {
        Logger.LogInformation("Starting workflow generation for goal: {UserGoal} with {AgentCount} available agents",
            userGoal, availableAgents.Count);

        try
        {
            // 直接使用AgentDescriptionInfo生成Prompt
            var prompt = BuildPromptFromAgentDescriptionInfo(userGoal, availableAgents);
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
    /// 基于AgentDescriptionInfo构建AI提示词
    /// </summary>
    private string BuildPromptFromAgentDescriptionInfo(string userGoal, List<AgentDescriptionInfo> agentList)
    {
        var prompt = new StringBuilder();

        // System role definition
        prompt.AppendLine("# Advanced Workflow Orchestration Expert");
        prompt.AppendLine(
            "You are an advanced AI workflow orchestration expert. Based on user goals, analyze available Agent capabilities and design a complete workflow execution plan with proper node connections and data flow.");
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
        prompt.AppendLine(
            "1) **Agent Selection**: Choose the most suitable agents based on capabilities and categories");
        prompt.AppendLine("2) **Node Design**: Create workflow nodes with proper configuration");
        prompt.AppendLine("3) **Connection Logic**: Define execution order and data flow between nodes");
        prompt.AppendLine("4) **Error Handling**: Consider failure scenarios and alternative paths");
        prompt.AppendLine("5) **Performance**: Optimize for execution time and resource usage");
        prompt.AppendLine();

        // Error Handling & Fallback Guidance
        prompt.AppendLine("## Error Handling & Fallback Guidance");
        prompt.AppendLine(
            "When analyzing the user goal and available agents, apply the following error handling strategies:");
        prompt.AppendLine();

        prompt.AppendLine("### Ambiguous Prompt Detection");
        prompt.AppendLine("If the user goal is unclear or ambiguous:");
        prompt.AppendLine("- Include a 'clarificationNeeded' node that identifies specific unclear aspects");
        prompt.AppendLine("- Suggest concrete questions to help clarify the user's intent");
        prompt.AppendLine("- Provide examples of how the goal could be interpreted");
        prompt.AppendLine();

        prompt.AppendLine("### Impossible Requirements Handling");
        prompt.AppendLine("If the user goal cannot be achieved with available agents:");
        prompt.AppendLine("- Create a 'requirementsAnalysis' node explaining what's missing");
        prompt.AppendLine("- Suggest alternative approaches using available agents");
        prompt.AppendLine("- Recommend breaking down complex goals into achievable sub-goals");
        prompt.AppendLine();

        prompt.AppendLine("### Partial Generation Strategy");
        prompt.AppendLine("When full automation isn't possible:");
        prompt.AppendLine("- Generate a partial workflow with completed sections");
        prompt.AppendLine("- Include 'manualCompletion' nodes for sections requiring human input");
        prompt.AppendLine("- Provide specific guidance on what manual steps are needed");
        prompt.AppendLine();

        prompt.AppendLine("### Fallback Templates");
        prompt.AppendLine("For complex or unclear goals, suggest template patterns:");
        prompt.AppendLine("- Include a 'templateSuggestion' node with similar workflow patterns");
        prompt.AppendLine("- Reference common workflow templates that might fit the user's needs");
        prompt.AppendLine("- Provide step-by-step guidance for manual workflow creation");
        prompt.AppendLine();

        prompt.AppendLine("### Error Recovery Instructions");
        prompt.AppendLine("Always include error recovery in your workflow design:");
        prompt.AppendLine("- Add retry mechanisms for critical workflow steps");
        prompt.AppendLine("- Include validation nodes to check intermediate results");
        prompt.AppendLine("- Design alternative execution paths for common failure scenarios");
        prompt.AppendLine("- Provide clear error messages that guide users toward solutions");
        prompt.AppendLine();

        // JSON format specification
        prompt.AppendLine("## JSON Format Specification");
        prompt.AppendLine("Please strictly follow the following JSON format output:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"generationStatus\": \"success|partial|template_recommendation|manual_guidance\",");
        prompt.AppendLine("  \"clarityScore\": 1-5,");
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
        prompt.AppendLine("        \"connectionType\": \"data_flow_or_sequence\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  },");
        prompt.AppendLine("  \"errorInfo\": {");
        prompt.AppendLine("    \"errorType\": \"prompt_ambiguity|insufficient_information|technical_limitation|null\",");
        prompt.AppendLine("    \"errorMessage\": \"Clear description of any issues\",");
        prompt.AppendLine("    \"actionableSteps\": [\"Step 1\", \"Step 2\", \"Step N\"]");
        prompt.AppendLine("  },");
        prompt.AppendLine("  \"completionPercentage\": 0-100,");
        prompt.AppendLine("  \"completionGuidance\": {");
        prompt.AppendLine("    \"suggestedNodes\": [\"node suggestions for completion\"],");
        prompt.AppendLine("    \"nextSteps\": [\"specific steps to complete workflow\"]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("**Required Fields for All Responses:**");
        prompt.AppendLine("- `generationStatus`: Always include (success/partial/template_recommendation/manual_guidance)");
        prompt.AppendLine("- `clarityScore`: Rate user goal clarity from 1 (very unclear) to 5 (very clear)");
        prompt.AppendLine("- `errorInfo`: null if successful, otherwise provide detailed error information");
        prompt.AppendLine();
        prompt.AppendLine("**Conditional Fields:**");
        prompt.AppendLine("- `completionPercentage` and `completionGuidance`: Include only for partial generation status");
        prompt.AppendLine("- For template_recommendation status: Include template suggestions in errorInfo.actionableSteps");
        prompt.AppendLine("- For manual_guidance status: Include step-by-step manual creation guide in errorInfo.actionableSteps");
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
            
            var chatResult = await ChatWithHistory(
                prompt, 
                history, 
                promptSettings, 
                cancellationToken, 
                context, 
                imageKeys);

            if (chatResult == null || chatResult.Count == 0)
            {
                Logger.LogWarning("AI returned empty response for workflow generation");
                return GetFallbackWorkflowJson("system_error", "AI service returned empty response", 
                    new[] { "Retry the workflow generation", "Check AI service availability", "Use manual workflow creation" });
            }

            var response = chatResult[0].Content;
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogWarning("AI returned empty content for workflow generation");
                return GetFallbackWorkflowJson("system_error", "AI service returned empty content",
                    new[] { "Retry with a more specific goal", "Verify AI service is functioning", "Create workflow manually" });
            }

            // 添加详细的调试日志记录AI返回的原始内容
            Logger.LogDebug("AI raw response content: {RawResponse}", response);
            Logger.LogDebug("AI response length: {Length} chars", response.Length);

            // Validate that the response contains required error handling fields
            if (!IsValidErrorHandlingResponse(response))
            {
                Logger.LogWarning("AI response missing required error handling fields, wrapping response. Raw content preview: {Preview}", 
                    response.Length > 200 ? response.Substring(0, 200) + "..." : response);
                return WrapLegacyResponse(response);
            }

            Logger.LogDebug("AI successfully generated workflow with response length: {Length}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling AI for workflow generation");
            return GetFallbackWorkflowJson("system_error", $"AI service error: {ex.Message}",
                new[] { "Check system logs for details", "Retry after a few minutes", "Contact system administrator if problem persists" });
        }
    }

    /// <summary>
    /// 验证AI响应是否包含必需的错误处理字段
    /// </summary>
    private bool IsValidErrorHandlingResponse(string response)
    {
        try
        {
            var json = JObject.Parse(response);
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
            var legacyJson = JObject.Parse(legacyResponse);
            
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
    private string GetFallbackWorkflowJson(string errorType = "system_error", string errorMessage = "AI service unavailable", string[] actionableSteps = null)
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
                        ["nodeType"] = "ManualCreationAgent",
                        ["extendedData"] = new JObject
                        {
                            ["description"] = "This node represents the need for manual workflow creation when automatic generation fails"
                        },
                        ["properties"] = new JObject
                        {
                            ["message"] = errorMessage,
                            ["guidance"] = "Please create your workflow manually using the workflow designer"
                        }
                    }
                },
                ["workflowNodeUnitList"] = new JArray()
            },
            ["errorInfo"] = new JObject
            {
                ["errorType"] = errorType,
                ["errorMessage"] = errorMessage,
                ["actionableSteps"] = actionableSteps != null ? new JArray(actionableSteps) : new JArray
                {
                    "Use the manual workflow designer",
                    "Start with a workflow template",
                    "Break down your goal into smaller steps",
                    "Contact support if you need assistance"
                }
            },
            ["systemInfo"] = new JObject
            {
                ["fallbackTriggered"] = true,
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["suggestedRetryTime"] = DateTime.UtcNow.AddMinutes(5).ToString("O")
            }
        };

        return fallbackJson.ToString();
    }
}