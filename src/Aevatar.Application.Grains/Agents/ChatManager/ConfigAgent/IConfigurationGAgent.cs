using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;

public interface IConfigurationGAgent : IGAgent
{
    Task<string> GetSystemLLM();
    Task<string> GetPrompt();
}