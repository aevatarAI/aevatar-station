using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents.ChatSEvents;

namespace Aevatar.GAgents.MultiAIChatGAgent.Test.GAgents;


public interface IMultiAIChatGAgentTest : IMultiAIChatGAgent
{
    
}

public class MultiAIChatGAgentTest : MultiAIChatGAgent<MultiAIChatGAgentState, MultiAIChatGAgentLogEvent, EventBase, MultiAIChatConfig>, IMultiAIChatGAgentTest
{
    
}

