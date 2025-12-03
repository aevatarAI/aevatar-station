using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.Workflow;

/// <summary>
/// Interface for WorkflowProxyAgent
/// </summary>
public interface IWorkflowProxyAgent : ICoreStateGAgent<WorkflowProxyAgentState>
{
}

/// <summary>
/// Simple proxy agent state extending CoreStateBase (minimal state needed)
/// </summary>
[GenerateSerializer]
public class WorkflowProxyAgentState : CoreStateBase;

/// <summary>
/// Simple state log event for proxy agent
/// </summary>
[GenerateSerializer]
public abstract class WorkflowProxyAgentStateLogEvent : StateLogEventBase<WorkflowProxyAgentStateLogEvent>;

/// <summary>
/// Proxy agent that inherits directly from CoreGAgentBase for P2P event publishing
/// This agent acts as an intermediary to publish events to workflow agents using SendEventToAgentAsync
/// </summary>
[GAgent("WorkflowProxyAgent", "Aevatar.GAgents.Workflow")]
public class WorkflowProxyAgent : CoreGAgentBase<WorkflowProxyAgentState, WorkflowProxyAgentStateLogEvent>, IWorkflowProxyAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Proxy agent using CoreGAgentBase SendEventToAgentAsync for P2P workflow event communication");
    }

    // No custom methods needed - CoreGAgentBase already provides SendEventToAgentAsync for P2P event communication
}
