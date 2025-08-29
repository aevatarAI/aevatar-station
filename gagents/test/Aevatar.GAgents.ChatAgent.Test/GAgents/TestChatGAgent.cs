using Aevatar.Core.Abstractions;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.ChatAgent.GAgent.SEvent;
using Aevatar.GAgents.ChatAgent.GAgent.State;

namespace Aevatar.GAgents.ChatAgent.Test.GAgents;

public interface ITestChatGAgent : IChatAgent, IStateGAgent<ChatGAgentState>
{
    
}

[GAgent]
public class TestChatGAgent :
    ChatGAgentBase<ChatGAgentState, ChatGAgentLogEventBase, EventBase, ChatConfigDto>,ITestChatGAgent
{
}