// ABOUTME: This file implements the enhanced InputGAgentPlus that returns configured input as ChatResponse
// ABOUTME: Enhanced version that extends BusinessAgentBase for advanced workflow integration

using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Orleans.Providers;
using Aevatar.Core.Placement;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.InputGAgent.GAgent;

[SiloNamePatternPlacement("Projector")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(InputGAgentPlus))]
public class InputGAgentPlus : BusinessAgentBase<InputGAgentStatePlus, InputGAgentLogEvent, InputConfigDto>, IInputGAgentPlus
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Enhanced input agent that returns configured input text with advanced workflow integration");
    }


    protected override async Task PerformConfigAsync(InputConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);
        
        RaiseEvent(new SetInputLogEvent { Input = configuration.Input });
        await ConfirmEvents();
    }
    
    /// <summary>
    /// Override OnBusinessAgentEventForwardingEventHandlerAsync for input handling
    /// This ensures BusinessAgentBase validation runs first
    /// </summary>
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        try
        {
            // Assign the WorkUnitAgentId to represent this processing node
            workflowEvent.WorkUnitAgentId = this.GetPrimaryKey();
            
            workflowEvent.Message = State.Input ?? "No input configured";
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowInProgress;
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
        }
        catch (Exception ex)
        {
            workflowEvent.ErrorMessage = ex.Message;
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
        }
    }

    protected override void GAgentTransitionState(InputGAgentStatePlus state, StateLogEventBase<InputGAgentLogEvent> @event)
    {
        base.GAgentTransitionState(state, @event);

        switch (@event)
        {
            case SetInputLogEvent setInputEvent:
                state.Input = setInputEvent.Input;
                break;
        }
    }
}
