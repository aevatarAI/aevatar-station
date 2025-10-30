using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

public interface IChatAIGAgentPlus : IAIGAgent, IStateGAgentPlus<ChatAIGAgentStatePlus>
{
    Task<string> GetLastResponseAsync();
} 