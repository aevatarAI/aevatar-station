using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents;

[GenerateSerializer]
public class MultiAIChatGAgentState : StateBase
{
    [Id(0)] public List<Guid> AIAgentIds { get; set; } = new List<Guid>();
    [Id(1)] public int MaxHistoryCount { get; set; } = 10;
    [Id(2)] public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    
}