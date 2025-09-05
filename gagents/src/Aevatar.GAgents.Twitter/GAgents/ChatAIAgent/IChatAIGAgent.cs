using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGAgentState>
{
    Task<string> GetLastResponseAsync();
} 