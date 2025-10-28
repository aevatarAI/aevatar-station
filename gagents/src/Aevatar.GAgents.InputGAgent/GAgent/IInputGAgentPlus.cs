// ABOUTME: This file defines the interface for InputGAgent
// ABOUTME: Extends IGAgent to follow established patterns

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.Events;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;

namespace Aevatar.GAgents.InputGAgent.GAgent;

public interface IInputGAgentPlus : IStateGAgentPlus<InputGAgentStatePlus>
{

}