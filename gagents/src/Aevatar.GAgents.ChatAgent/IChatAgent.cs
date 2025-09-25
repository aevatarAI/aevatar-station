using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.ChatAgent.GAgent;

public interface IChatAgent : IGAgent, IAIGAgent
{
    Task<List<ChatMessage>?> ChatAsync(string message,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null,
        List<string>? imageKeys = null);

    Task<bool> ChatWithStreamAsync(string message, AIChatContextDto aiChatContextDto,
        ExecutionPromptSettings? promptSettings = null, List<string>? imageKeys = null);
}