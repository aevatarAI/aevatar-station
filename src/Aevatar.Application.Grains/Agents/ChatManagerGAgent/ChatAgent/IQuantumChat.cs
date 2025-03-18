using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Application.Grains.Agents.ChatGAgentManager.ChatAgent;

public interface IQuantumChat : IGAgent
{
    Task<string> QuantumChatAsync(string llm, string message, ExecutionPromptSettings? promptSettings = null);
    Task<List<ChatMessage>> GetChatMessageAsync();
}