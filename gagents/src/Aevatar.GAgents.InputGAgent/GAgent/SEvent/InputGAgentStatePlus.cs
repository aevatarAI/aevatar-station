// ABOUTME: This file defines the state for InputGAgent with AI capabilities
// ABOUTME: Extends BusinessAgentState to provide AI and workflow coordination features

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.InputGAgent.GAgent.SEvent;

[GenerateSerializer]
public class InputGAgentStatePlus : BusinessAgentState
{
    [Id(0)] public string Input { get; set; } = string.Empty;
}