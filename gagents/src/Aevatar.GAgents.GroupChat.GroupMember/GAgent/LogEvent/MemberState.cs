// ABOUTME: This file implements the state class for group member agents
// ABOUTME: Contains the persistent state information for member agents including member name

using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.GEvent;

[GenerateSerializer]
public class MemberState : StateBase
{
    [Id(0)] public string MemberName { get; set; }
}