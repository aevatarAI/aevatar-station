using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager;

public interface IChatManagerGAgent : IGAgent
{
    Task<Guid> CreateSessionAsync(string systemLLM, string prompt);
    Task<Tuple<string,string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content, ExecutionPromptSettings promptSettings = null);
    
    [Orleans.Concurrency.ReadOnly]
    Task<List<SessionInfoDto>> GetSessionListAsync();
    
    [Orleans.Concurrency.ReadOnly]
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId);
    
    Task DeleteSessionAsync(Guid sessionId);
    Task RenameSessionAsync(Guid sessionId, string title);
    Task ClearAllAsync();
}