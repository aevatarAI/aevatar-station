using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Aevatar.Application.Grains.Agents.AI;

[GAgent("WorkflowComposer")]
public class WorkflowComposerGAgent : AIGAgentBase<WorkflowComposerState, WorkflowComposerEvent>,
    IWorkflowComposerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "AI workflow generation agent that creates complete workflow JSON from user goals and available agent descriptions. Analyzes user goals and available agent capabilities to generate optimized workflow configurations with proper node connections and data flow management.");
    }

    public async Task<string> GenerateWorkflowJsonAsync(string userGoal)
        => await CallAIForWorkflowGenerationAsync(userGoal);

    private async Task<string> CallAIForWorkflowGenerationAsync(string userGoal)
    {
        try
        {
            Logger.LogDebug("Sending user goal to AI service: {UserGoal}", userGoal);
            var chatResult = await ChatWithHistory(userGoal);
            
            // 使用AiAgentHelper统一处理AI响应并清理JSON
            var response = AiAgentHelper.ProcessAiChatResult(chatResult, Logger, 
                msg => GetFallbackWorkflowJson("ai_service_error", msg), "workflow generation");
            
            var cleanedJson = AiAgentHelper.CleanJsonContent(response);
            
            // 验证JSON有效性
            if (!AiAgentHelper.IsValidJson(cleanedJson))
            {
                Logger.LogWarning("AI returned invalid JSON content for workflow generation");
                return GetFallbackWorkflowJson("invalid_json", "AI returned invalid JSON content");
            }

            return cleanedJson;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while calling AI service for workflow generation");
            return GetFallbackWorkflowJson("ai_service_error", $"AI service error: {ex.Message}");
        }
    }

    private string GetFallbackWorkflowJson(string errorType = "system_error",
        string errorMessage = "AI service unavailable", string[]? actionableSteps = null)
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
                ["actionableSteps"] = actionableSteps != null
                    ? new JArray(actionableSteps)
                    : new JArray(
                        "Review the user goal for clarity",
                        "Check AI service availability",
                        "Try again with a simpler request",
                        "Contact support if the issue persists"
                    )
            },
            ["completionPercentage"] = 0,
            ["completionGuidance"] =
                "Please review the error information and take the suggested actions to resolve the issue."
        };

        return fallbackJson.ToString();
    }
}