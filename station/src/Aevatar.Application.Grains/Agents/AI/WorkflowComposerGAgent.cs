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
    {
        Logger.LogInformation("Starting workflow generation for goal: {UserGoal}", userGoal);

        try
        {
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

    private async Task<string> CallAIForWorkflowGenerationAsync(string userGoal)
    {
        try
        {
            Logger.LogDebug("Sending user goal to AI service: {UserGoal}", userGoal);

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

    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return GetFallbackWorkflowJson("empty_content", "AI returned empty content");

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