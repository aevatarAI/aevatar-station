using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

public interface IConfigurationGAgent : IGAgent
{
    Task<string> GetSystemLLM();
    Task<bool> GetStreamingModeEnabled();
    Task<string> GetPrompt();
    Task UpdateSystemPromptAsync(String systemPrompt);
}