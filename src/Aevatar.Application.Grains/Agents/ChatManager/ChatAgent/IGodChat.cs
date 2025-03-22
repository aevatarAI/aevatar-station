using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

public interface IGodChat : IGAgent
{
    Task<string> GodChatAsync(string llm, string message, ExecutionPromptSettings? promptSettings = null);
    Task<List<ChatMessage>> GetChatMessageAsync();
}