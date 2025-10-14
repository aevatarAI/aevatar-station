// ABOUTME: This file implements the InputGAgent that returns configured input as ChatResponse
// ABOUTME: Extends GroupMemberGAgentBase to integrate with group chat functionality

using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Orleans.Providers;
using Aevatar.Core.Placement;
using Orleans.Runtime;

[module: Interceptor]

namespace Aevatar.GAgents.InputGAgent.GAgent;

[SiloNamePatternPlacement("Projector")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(InputGAgent))]
public class InputGAgent : MemberGAgentBase<InputGAgentState, InputGAgentLogEvent, EventBase, InputConfigDto>, IInputGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Input agent that returns configured input text");
    }

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] {WorkflowIdProperty, RoundIdProperty, GrainIdProperty})]
    protected override Task<int> GetInterestValueAsync()
    {
        
        return Task.FromResult(100);
    }

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] {WorkflowIdProperty, RoundIdProperty, GrainIdProperty})]
    protected override Task<ChatResponse> ChatAsync(List<ChatMessage>? messages)
    {
        var response = new ChatResponse
        {
            Content = State.Input,
            Continue = true,
            Skip = false
        };

        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task PerformConfigAsync(InputConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);
        
        RaiseEvent(new SetInputLogEvent { Input = configuration.Input });
        await ConfirmEvents();
    }

    protected override void MemberTransitionState(InputGAgentState state, StateLogEventBase<InputGAgentLogEvent> @event)
    {
        switch (@event)
        {
            case SetInputLogEvent setInputEvent:
                state.Input = setInputEvent.Input;
                break;
        }
    }
}