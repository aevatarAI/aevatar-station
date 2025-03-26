using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

public interface IQuantumService
{
    Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt);
    Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null);
    Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId);
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId);
    Task DeleteSessionAsync(Guid userId, Guid sessionId);
    Task RenameSessionAsync(Guid userId, Guid sessionId, string title);
}

public class QuantumService : IQuantumService, ITransientDependency
{
    private readonly IClusterClient _clusterClient;

    public QuantumService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.CreateSessionAsync(systemLLM, prompt);
    }

    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM,
        string content,
        ExecutionPromptSettings promptSettings = null)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.ChatWithSessionAsync(sessionId, sysmLLM, content, promptSettings);
    }

    public async Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.GetSessionListAsync();
    }

    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.GetSessionMessageListAsync(sessionId);
    }

    public async Task DeleteSessionAsync(Guid userId, Guid sessionId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        await manager.DeleteSessionAsync(sessionId);
    }

    public async Task RenameSessionAsync(Guid userId, Guid sessionId, string title)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        await manager.RenameSessionAsync(sessionId, title);
    }
}