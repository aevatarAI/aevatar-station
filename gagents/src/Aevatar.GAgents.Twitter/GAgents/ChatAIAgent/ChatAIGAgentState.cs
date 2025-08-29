using System;
using GroupChat.GAgent.GEvent;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentState : GroupMemberState
{
    [Id(0)]
    public string LastResponse { get; set; } = "";
    
    [Id(1)]
    public DateTime LastActivityTime { get; set; }
    
    [Id(2)]
    public int TotalInteractions { get; set; } = 0;
} 