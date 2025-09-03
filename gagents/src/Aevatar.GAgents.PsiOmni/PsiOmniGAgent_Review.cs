using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private async Task<ReviewResult> RunReviewAsync(FinalResponse finalResponse)
    {
        LogEventDebug("Running orchestrator mode with {ChildCount} child agents", State.ChildAgents.Count);
        var kernel = GetKernel_Plain();
        if (kernel == null)
        {
            LogEventError(new InvalidOperationException("Cannot get kernel for Orchestrator mode"),
                "Failed to get kernel for Orchestrator mode");
            return new ReviewResult();
        }

        var systemPrompt = """
                           You are a reviewer that reviews the response of a specialized agent.
                           You will be given the response of the specialized agent and the task that the agent was supposed to complete.
                           You will need to determine if the response is correct and complete.
                           If the response is not correct or complete, you will need to provide a review of the response which will be used to prompt the agent to improve the response.

                           ## Output Format
                           Output a JSON object with the following fields:
                           - "Decision": the decision of the reviewer. Possible values are: APPROVED, NEEDS_FIXES, MAJOR_ISSUES.
                           - "Comment": the comment of the reviewer.

                           Examples:
                           <example1>
                           {"Decision": "APPROVED", "Comment": "The response is correct and complete."}
                           </example1>
                           <example2>
                           {"Decision": "NEEDS_FIXES", "Comment": "The response is not correct or complete. The response is missing the following information: {State.CurrentTask.AcceptanceCriteria}"}
                           </example2>
                           <example3>
                           {"Decision": "MAJOR_ISSUES", "Comment": "You missed the so and so parts of the requirements. Please fix it."}
                           </example3>
                           <example4>
                           {"Decision": "MAJOR_ISSUES", "Comment": "Your output format is not useable for users. The manual must include all needed information."}
                           </example4>
                           """;
        try
        {
            LogEventDebug("RunReviewAsync - Getting chat completion service");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var maxTokens = 4000; // 默认最大 token
            var temperature = 0.1; // 默认温度
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = maxTokens,
                Temperature = temperature
            };
            LogEventDebug("RunReviewAsync - Execution settings configured");

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddSystemMessage(systemPrompt);
            var artifacts = new List<string>();
            foreach (var artifact in finalResponse.Artifacts)
            {
                artifacts.Add($"<artifact name=\"{artifact.Name}\" format=\"{artifact.Format}\" >{artifact.Content}</artifact>\n");
            }
            var userMessage = $"<task>{State.CurrentTask.DetailedDescription}</task>\n" +
                              $"<proposed_response>{finalResponse.Response}</proposed_response>\n" +
                              $"<artifacts>{string.Join("\n", artifacts)}</artifacts>\n";
            chatHistory.AddUserMessage(userMessage);

            var preChatHistoryLength = chatHistory.Count;
            LogEventDebug("RunReviewAsync - Chat history prepared: Length={Count}", preChatHistoryLength);

            var result = await ExecuteWithRetryAsync(
                async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
                "GetChatMessageContent");

            chatHistory.Add(result);
            LogEventDebug("RunReviewAsync completed successfully");
            return JsonSerializer.Deserialize<ReviewResult>(result.ToString());
        }
        catch (Exception e)
        {
            LogEventError(e, "Error during RunCoreAsync: {Message}", e.Message);
            throw;
        }
    }
}