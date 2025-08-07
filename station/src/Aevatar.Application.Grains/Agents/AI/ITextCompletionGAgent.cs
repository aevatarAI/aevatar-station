using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.Application.Grains.Agents.AI;

public interface ITextCompletionGAgent : IAIGAgent, IStateGAgent<TextCompletionState>
{
    Task<List<string>> GenerateCompletionsAsync(string inputText);
}