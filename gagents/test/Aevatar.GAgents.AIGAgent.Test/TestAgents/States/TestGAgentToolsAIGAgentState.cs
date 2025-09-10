// ABOUTME: This file defines the state class for TestGAgentToolsAIGAgent used in GAgent tool testing
// ABOUTME: Extends AIGAgentStateBase with test-specific state properties

using System.Collections.Generic;
using Aevatar.GAgents.AIGAgent.State;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Test.TestAgents.States;

[GenerateSerializer]
public class TestGAgentToolsAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> TestMessages { get; set; } = new();
    [Id(1)] public int TestEventCount { get; set; } = 0;
    [Id(2)] public Dictionary<string, object> TestData { get; set; } = new();
}