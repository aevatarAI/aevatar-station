// ABOUTME: This file defines the state for InputGAgent
// ABOUTME: Extends GroupMemberState to store the input string

using GroupChat.GAgent.GEvent;

namespace Aevatar.GAgents.InputGAgent.GAgent.SEvent;

[GenerateSerializer]
public class InputGAgentState : MemberState
{
    [Id(1)] public string Input { get; set; } = string.Empty;
}