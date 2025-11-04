using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private Kernel GetKernel_Plain()
    {
        var kernel = GetKernelFromBrain();
        if (kernel == null)
            throw new InvalidOperationException("Kernel is not configured for tool execution.");

        return kernel;
    }

    private void OnChatDoneAsync_Analyzer(ChatHistory chatHistory, int preChatHistoryLength)
    {
        LogEventDebug("Processing analyzer chat messages for AgentId={AgentId}, NewMessages={Count}", 
            AgentId, chatHistory.Count - preChatHistoryLength);
            
        var result = chatHistory.Last().Content ?? string.Empty;
        LogEventInfo("OnChatDoneAsync_Analyzer Result for AgentId={AgentId}: {Result}", 
            AgentId, result.Substring(0, Math.Min(200, result.Length)) + "...");
            
        if (result.Contains("ORCHESTRATOR") || result.Contains("SPECIALIZED"))
        {
            var jsonStartIndex = result.IndexOf('{');
            var jsonEndIndex = result.LastIndexOf('}');
            if (jsonStartIndex != -1 && jsonEndIndex != -1)
            {
                result = result.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
            }

            var realizationResult = JsonSerializer.Deserialize<RealizationResult>(result);
            if (realizationResult?.OperationMode == "ORCHESTRATOR")
            {
                RaiseEvent(new RealizationEvent
                {
                    RealizationStatus = RealizationStatus.Orchestrator,
                    Description = realizationResult?.Description ?? string.Empty // Orchestrator doesn't have tools.
                });
            }
            else if (realizationResult?.OperationMode == "SPECIALIZED")
            {
                var tools = new List<ToolDefinition>();
                foreach (var toolName in realizationResult.Tools)
                {
                    var kernelFunction = _kernelFunctionRegistry.GetToolByQualifiedName(toolName);
                    if (kernelFunction != null)
                    {
                        tools.Add(kernelFunction.ToToolDefinition());
                    }
                }

                RaiseEvent(new RealizationEvent()
                {
                    RealizationStatus = RealizationStatus.Specialized,
                    Description = realizationResult?.Description ?? string.Empty,
                    Tools = tools
                });
            }
        }
    }

    private string GetAllToolDefinitions()
    {
        var toolDefinitions = _kernelFunctionRegistry.GetAllToolDefinitions();
        return toolDefinitions.ToYaml();
    }
}