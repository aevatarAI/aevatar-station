using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.Application.Grains.Agents.AI;

public interface IWorkflowComposerGAgent : IAIGAgent, IStateGAgent<WorkflowComposerState>
{
    Task<string> GenerateWorkflowJsonAsync(string userGoal);
}