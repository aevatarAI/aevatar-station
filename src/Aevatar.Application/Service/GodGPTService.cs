using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.Quantum;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

public interface IGodGPTService
{
    Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt);
    Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null);
    Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId);
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId);
    Task DeleteSessionAsync(Guid userId, Guid sessionId);
    Task RenameSessionAsync(Guid userId, Guid sessionId, string title);
    
    Task<string> GetSystemPromptAsync();
    Task UpdateSystemPromptAsync(GodGPTConfigurationDto godGptConfigurationDto);

}

public class GodGPTService : IGodGPTService, ITransientDependency
{
    private readonly IClusterClient _clusterClient;

    public GodGPTService(IClusterClient clusterClient)
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

    public Task<string> GetSystemPromptAsync()
    {
        var configurationAgent = _clusterClient.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
        return  configurationAgent.GetPrompt();
    }

    public Task UpdateSystemPromptAsync(GodGPTConfigurationDto godGptConfigurationDto)
    {
        var configurationAgent = _clusterClient.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
        return  configurationAgent.UpdateSystemPromptAsync(godGptConfigurationDto.SystemPrompt);
    }
}