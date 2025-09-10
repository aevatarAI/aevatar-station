using System.Text.Json;
using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private Kernel GetKernel_Orchestrator()
    {
        var kernel = GetKernelFromBrain();
        if (kernel == null)
            throw new InvalidOperationException("Kernel is not configured.");

        // Add the orchestrator-specific functions as a plugin
        if(!kernel.Plugins.Contains("AgentServices"))
            kernel.Plugins.AddFromObject(this, "AgentServices");

        return kernel;
    }

    private void OnChatDoneAsync_Orchestrator(ChatHistory chatHistory, int preChatHistoryLength)
    {
        LogEventDebug("Processing orchestrator chat messages for AgentId={AgentId}, NewMessages={Count}", 
            AgentId, chatHistory.Count - preChatHistoryLength);
            
        List<AgentDescriptor> FishAgentCreationEvents(IList<PsiOmniChatMessage> newMessages)
        {
            bool IsAgentCreation(PsiOmniChatMessage message)
            {
                return message.Role == "tool" &&
                       message.Metadata.TryGetValue("FunctionName", out var funcNameObject) &&
                       funcNameObject is string funcName && funcName == "create_agent" &&
                       message.Content.IndexOf('{') >= 0;
            }

            return newMessages.Where(IsAgentCreation).Select(message =>
                JsonSerializer.Deserialize<AgentDescriptor>(
                    message.Content.Substring(message.Content.IndexOf('{'))
                )
            ).Where(x => x != null).Select(x => x!).ToList();
        }

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

        var newAgents = FishAgentCreationEvents(newMessages);
        if (newAgents.Count > 0)
        {
            LogEventInfo("Found {Count} new agent creation events for AgentId={AgentId}", newAgents.Count, AgentId);
            RaiseEvent(new NewAgentsCreatedEvent
            {
                NewAgents = newAgents
            });
        }

        RaiseEvent(new GrowChatHistoryEvent()
        {
            NewMessages = newMessages
        });
    }

    private string GetAllChildAgents()
    {
        var children = State.ChildAgents.Values.ToList();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(children);
    }
}