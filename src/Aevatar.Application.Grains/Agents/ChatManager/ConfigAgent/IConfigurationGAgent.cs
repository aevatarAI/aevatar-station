using Aevatar.Core.Abstractions;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

public interface IConfigurationGAgent : IGAgent
{
    [ReadOnly]
    Task<string> GetSystemLLM();
    
    [ReadOnly]
    Task<bool> GetStreamingModeEnabled();
    [ReadOnly]
    Task<string> GetPrompt();
    
    Task UpdateSystemPromptAsync(String systemPrompt);
}