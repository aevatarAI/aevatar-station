using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;
using OpenAI.Chat;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.PsiOmni;

using Microsoft.SemanticKernel.Connectors.OpenAI;

public partial class PsiOmniGAgent
{
    private void ScheduleTask(Func<Task> action)
    {
        // Orleans RegisterTimer ensures the callback runs in the Grain's context.
        this.RegisterGrainTimer(action, new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.Zero, // Trigger immediately
            Period = TimeSpan.FromMilliseconds(-1), // Only once
            Interleave = false,
            KeepAlive = false
        });
    }

    private static PsiOmniChatMessage ConvertOpenAiChatMessage(OpenAIChatMessageContent content)
    {
        var json = JsonSerializer.Serialize(content);
        var serialized = new SerializedChatMessageContent()
        {
            TypeFullName = typeof(OpenAIChatMessageContent).FullName,
            Json = json
        };
        TokenUsage? tokenUsage = null;
        if (content.InnerContent is ChatCompletion cc)
        {
            tokenUsage = new TokenUsage
            {
                PromptTokens = cc.Usage.InputTokenCount,
                CompletionTokens = cc.Usage.OutputTokenCount,
                TotalTokens = cc.Usage.TotalTokenCount
            };
        }

        return new PsiOmniChatMessage(content.Role.ToString(), content.Content)
        {
            Serialized = serialized,
            TokenUsage = tokenUsage
        };
    }

    private static PsiOmniChatMessage ConvertToolCallMessage(ChatMessageContent m)
    {
        var message = new PsiOmniChatMessage(m.Role.ToString(), m.Content);
        var functionResult = m.Items.OfType<FunctionResultContent>().FirstOrDefault();
        if (functionResult == null) return message;
        message.Metadata[OpenAIChatMessageContent.ToolIdProperty] = functionResult.CallId ?? string.Empty;
        message.Metadata["FunctionName"] = functionResult.FunctionName ?? string.Empty;
        return message;
    }
}