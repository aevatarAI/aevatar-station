// ABOUTME: This file defines the state log events for InputGAgent
// ABOUTME: Includes events for setting the input value

using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.InputGAgent.GAgent.SEvent;

[GenerateSerializer]
public class InputGAgentLogEvent : StateLogEventBase<InputGAgentLogEvent>
{
}

[GenerateSerializer]
public class SetInputLogEvent : StateLogEventBase<InputGAgentLogEvent>
{
    [Id(0)] public string Input { get; set; } = string.Empty;
}