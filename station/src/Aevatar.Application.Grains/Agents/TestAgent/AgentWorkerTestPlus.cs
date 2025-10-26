// ABOUTME: This file implements the enhanced AgentWorkerTestPlus that handles test scenarios
// ABOUTME: Enhanced version that extends BusinessAgentBase for advanced workflow integration

using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.Core;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Orleans.Providers;
using Volo.Abp;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentWorkerTestPlus")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(AgentWorkerTestPlus))]
public class AgentWorkerTestPlus : BusinessAgentBase<AgentWorkerTestStatePlus, AgentWorkerTestEventLog, AgentWorkerTestConfigDto>, IAgentWorkerTestPlus
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Enhanced test worker agent with advanced workflow integration");
    }

    public Task<AgentWorkerTestStatePlus> GetState()
    {
        return Task.FromResult(State);
    }

    protected override async Task PerformConfigAsync(AgentWorkerTestConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);
        
        if (!configuration.FailureSummary.IsNullOrEmpty())
        {
            RaiseEvent(new AgentWorkerTestFailureLogEvent
            {
                FailureSummary = configuration.FailureSummary
            });
            await ConfirmEvents();
        }
    }

    /// <summary>
    /// ✅ Override OnBusinessAgentEventForwardingEventHandlerAsync to handle workflow events
    /// This ensures BusinessAgentBase validation runs first
    /// </summary>
    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] { WorkflowIdProperty, RoundIdProperty, GrainIdProperty })]
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        try
        {
            // Assign the WorkUnitAgentId to represent this processing node
            workflowEvent.WorkUnitAgentId = this.GetGrainId().ToString();
            
            // Check if failure is configured
            if (!State.FailureSummary.IsNullOrEmpty())
            {
                throw new UserFriendlyException(State.FailureSummary);
            }

            // ✅ CRITICAL: Set TaskResult (Agent's output)
            // Message is the input from upstream, TaskResult is this agent's output
            workflowEvent.TaskResult = $"{State.Name} processed the message successfully";
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowInProgress;
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AgentWorkerTestPlus failed to process workflow event");
            workflowEvent.ErrorMessage = ex.Message;
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Failed;
        }
    }

    protected override void GAgentTransitionState(AgentWorkerTestStatePlus state, StateLogEventBase<AgentWorkerTestEventLog> @event)
    {
        base.GAgentTransitionState(state, @event);

        switch (@event)
        {
            case AgentWorkerTestFailureLogEvent workerFailureLogEvent:
                state.FailureSummary = workerFailureLogEvent.FailureSummary;
                break;
        }
    }
}

public interface IAgentWorkerTestPlus : IStateGAgent<AgentWorkerTestStatePlus>
{
}

[GenerateSerializer]
public class AgentWorkerTestStatePlus : BusinessAgentState
{
    [Id(0)] public string FailureSummary { get; set; } = string.Empty;
}

