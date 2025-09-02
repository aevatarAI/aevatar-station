using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private Kernel GetKernel_Specialized()
    {
        var kernel = _kernelFactory.CreateKernel(
            State.Configuration!,
            State.Tools.Select(x => x.Name).ToList()
        ); // Orchestrator doesn't have specialized tools.
        if (kernel == null)
            throw new InvalidOperationException("Kernel is not configured for tool execution.");

        return kernel;
    }

    private void OnChatDoneAsync_Specialized(ChatHistory chatHistory, int preChatHistoryLength)
    {
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