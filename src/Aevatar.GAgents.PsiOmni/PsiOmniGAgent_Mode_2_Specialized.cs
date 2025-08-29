using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private Kernel GetKernel_Specialized()
    {
        var kernel = GetKernelFromBrain();
                
        if (kernel == null)
            throw new InvalidOperationException("Kernel is not configured for tool execution.");
        
        var toolNames = State.Tools.Select(x => x.Name).ToList();
        if (toolNames != null)
        {
            var funcs = new List<KernelFunction>();
            foreach (var toolName in toolNames)
            {
                var func = _kernelFunctionRegistry.GetToolByQualifiedName(toolName);
                if (func != null)
                {
                    funcs.Add(func);
                }
            }

            if (funcs.Count > 0 && !kernel.Plugins.Contains("Tools"))
            {
                kernel.Plugins.AddFromFunctions("Tools", funcs);
            }
        }

        return kernel;
    }
    
    private void OnChatDoneAsync_Specialized(ChatHistory chatHistory, int preChatHistoryLength)
    {
        LogEventDebug("Processing specialized chat messages for AgentId={AgentId}, NewMessages={Count}", 
            AgentId, chatHistory.Count - preChatHistoryLength);
            
        var newMessages = chatHistory.Skip(preChatHistoryLength)
            .Select(m =>
            {
                if (m is OpenAIChatMessageContent mm)
                {
                    return ConvertOpenAiChatMessage(mm);
                }

                if (m.Role == AuthorRole.Tool)
                {
                    return ConvertToolCallMessage(m);
                }

                return new PsiOmniChatMessage(m.Role.ToString(), m.Content);
            })
            .ToList();

        RaiseEvent(new GrowChatHistoryEvent()
        {
            NewMessages = newMessages
        });
    }
}