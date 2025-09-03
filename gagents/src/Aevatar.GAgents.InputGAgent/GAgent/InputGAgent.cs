// ABOUTME: This file implements the InputGAgent that returns configured input as ChatResponse
// ABOUTME: Extends GroupMemberGAgentBase to integrate with group chat functionality

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Orleans.Providers;

namespace Aevatar.GAgents.InputGAgent.GAgent;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(InputGAgent))]
public class InputGAgent : MemberGAgentBase<InputGAgentState, InputGAgentLogEvent, EventBase, InputConfigDto>, IInputGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Input agent that returns configured input text");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(100);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse
        {
            Content = State.Input,
            Continue = true,
            Skip = false
        };

        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
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