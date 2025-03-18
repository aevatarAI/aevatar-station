using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Application.Grains.Agents.ChatGAgentManager;

public interface IChatManagerGAgent : IGAgent
{
    Task<string> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, ExecutionPromptSettings promptSettings = null, CancellationToken cancellationToken = default);
    Task<List<SessionInfoDto>> GetSessionListAsync();
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId);
    Task DeleteSessionAsync(Guid sessionId);
    Task RenameSessionAsync(Guid sessionId, string title);
}