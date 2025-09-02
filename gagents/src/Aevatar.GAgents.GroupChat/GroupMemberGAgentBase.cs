using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.GroupChat;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.GAgent;

public abstract class
    GroupMemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : GroupMemberState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : GroupMemberConfigDto
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "GroupMemberGAgentBase - Base class for workflow nodes and group chat participants. " +
            "Provides core functionality for responding to coordination events, evaluating interest in participation, " +
            "processing messages from upstream nodes, and contributing to workflow execution. " +
            $"Member Name: {State.MemberName ?? "Not configured"}"
        );
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEvent @event)
    {
        var score = await GetInterestValueAsync(@event.BlackboardId);

        await PublishAsync(new EvaluationInterestResponseEvent()
        {
            MemberId = this.GetPrimaryKey(),
            BlackboardId = @event.BlackboardId,
            InterestValue = score,
            ChatTerm = @event.ChatTerm
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatEvent @event)
    {
        if (@event.Speaker != this.GetPrimaryKey())
        {
            return;
        }

        // var history = await GetCareChatMessagesFromBlackboardAsync(@event.BlackboardId);
        var talkResponse = await ChatAsync(@event.BlackboardId, @event.CoordinatorMessages);
        await PublishAsync(new ChatResponseEvent
        {
            BlackboardId = @event.BlackboardId,
            MemberId = this.GetPrimaryKey(),
            MemberName = State.MemberName,
            ChatResponse = talkResponse,
            Term = @event.Term
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        await GroupChatFinishAsync(@event.BlackboardId);
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPingEvent @event)
    {
        if (await IgnoreBlackboardPingEvent(@event.BlackboardId) == false)
        {
            await PublishAsync(new CoordinatorPongEvent()
            {
                BlackboardId = @event.BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName
            });
        }
    }

    protected abstract Task<int> GetInterestValueAsync(Guid blackboardId);

    protected abstract Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages);

    protected virtual Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent(Guid blackboardId)
    {
        return Task.FromResult(false);
    }

    [GenerateSerializer]
    public class SetMemberNameLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public string MemberName { get; set; }
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await base.PerformConfigAsync(configuration);
        RaiseEvent(new SetMemberNameLogEvent { MemberName = configuration.MemberName });
        await ConfirmEvents();
    }

    protected override void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetMemberNameLogEvent @setMemberNameLogEvent:
                State.MemberName = @setMemberNameLogEvent.MemberName;
                return;
        }

        GroupMemberTransitionState(state, @event);
    }

    protected virtual void GroupMemberTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
    }

    protected async Task<List<ChatMessage>> GetMessageFromBlackboardAsync(Guid blackboardId)
    {
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(blackboardId);
        var history = await blackboard.GetContent();

        return history;
    }
}