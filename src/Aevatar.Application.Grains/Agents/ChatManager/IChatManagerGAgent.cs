using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Application.Grains.Agents.ChatManager;

public interface IChatManagerGAgent : IGAgent
{
    Task<Guid> CreateSessionAsync(string systemLLM, string prompt, UserProfileDto? userProfile = null);
    Task<Tuple<string,string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, ExecutionPromptSettings promptSettings = null);
    Task<List<SessionInfoDto>> GetSessionListAsync();
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId);
    Task DeleteSessionAsync(Guid sessionId);
    Task RenameSessionAsync(Guid sessionId, string title);
    Task ClearAllAsync();
    Task SetUserProfileAsync(string gender, DateTime birthDate, string birthPlace);
    Task<UserProfileDto> GetUserProfileAsync();
}