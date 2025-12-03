using System;
using GroupChat.GAgent.GEvent;
using Orleans;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// ChatAIGAgentState inherits from AIGAgentStateBase
/// This provides BusinessAgentState + AI capabilities in the correct hierarchy
/// </summary>
[GenerateSerializer]
public class ChatAIGAgentStatePlus : AIGAgentStateBasePlus
{
    // ChatAI-specific state fields starting from Id(0)
    [Id(0)]
    public string LastResponse { get; set; } = "";
    
    [Id(1)]
    public DateTime LastActivityTime { get; set; }
    
    [Id(2)]
    public int TotalInteractions { get; set; } = 0;
} 