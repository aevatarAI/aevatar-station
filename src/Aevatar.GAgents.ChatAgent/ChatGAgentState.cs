using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.GAgents.ChatAgent.GAgent.State;

[GenerateSerializer]
public class ChatGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    [Id(1)] public int MaxHistoryCount { get; set; } = 10;
}