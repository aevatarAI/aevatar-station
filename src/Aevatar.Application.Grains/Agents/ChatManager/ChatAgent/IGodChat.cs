using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

public interface IGodChat : IGAgent
{
    Task<string> GodChatAsync(string llm, string message, ExecutionPromptSettings? promptSettings = null);
    Task InitAsync(Guid ChatManagerGuid);

    Task<string> GodStreamChatAsync(Guid sessionId,string llm, bool streamingModeEnabled, string message, String chatId, ExecutionPromptSettings? promptSettings = null);
    [ReadOnly]
    Task<List<ChatMessage>> GetChatMessageAsync();
    Task SetUserProfileAsync(UserProfileDto? userProfileDto);
    Task<UserProfileDto?> GetUserProfileAsync();
}