using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

public interface IGodChat : IGAgent
{
    Task<string> GodChatAsync(string llm, string message, ExecutionPromptSettings? promptSettings = null);
    Task<string> GodStreamChatAsync(string llm, bool streamingModeEnabled, string message, String chatId, ExecutionPromptSettings? promptSettings = null);
    Task<List<ChatMessage>> GetChatMessageAsync();
}