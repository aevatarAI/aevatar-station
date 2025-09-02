// ABOUTME: This file defines state log events for TestMCPAIGAgent used in MCP tool testing
// ABOUTME: Handles state transitions and event sourcing for test scenarios

using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Test.TestAgents.Events;

[GenerateSerializer]
public class TestMCPAIGAgentStateLogEvent : StateLogEventBase<TestMCPAIGAgentStateLogEvent>
{
    [Id(0)] public string? PromptTemplate { get; set; }
    [Id(1)] public string? SystemLLM { get; set; }
    [Id(2)] public string? LLMConfigKey { get; set; }
    [Id(3)] public bool EnableMCPTools { get; set; }
    [Id(4)] public List<string> TestMessages { get; set; } = new();
    [Id(5)] public Dictionary<string, object> TestData { get; set; } = new();
}