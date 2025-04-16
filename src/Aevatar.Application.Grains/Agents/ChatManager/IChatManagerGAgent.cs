using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager;

public interface IChatManagerGAgent : IGAgent
{
    Task<Guid> CreateSessionAsync(string systemLLM, string prompt, UserProfileDto? userProfile = null);
    Task<Tuple<string,string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, ExecutionPromptSettings promptSettings = null);
    [ReadOnly]
    Task<List<SessionInfoDto>> GetSessionListAsync();
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId);
    Task DeleteSessionAsync(Guid sessionId);
    Task RenameSessionAsync(Guid sessionId, string title);
    Task ClearAllAsync();
    Task SetUserProfileAsync(string gender, DateTime birthDate, string birthPlace, string fullName);
    Task<UserProfileDto> GetLastSessionUserProfileAsync();

    Task RenameChatTitleAsync(RenameChatTitleEvent @event);
}