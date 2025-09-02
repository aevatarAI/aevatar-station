// ABOUTME: This file defines external events for TestGAgentToolsAIGAgent used in GAgent tool testing
// ABOUTME: Represents external events that trigger state changes in test scenarios

using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Test.TestAgents.Events;

[GenerateSerializer]
public class TestGAgentToolsAIGAgentEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
    [Id(1)] public string EventType { get; set; } = "Test";
}