// ABOUTME: This file implements the base class for group member agents
// ABOUTME: Provides common functionality for handling group chat events and member interactions

using Aevatar.Core;
using Aevatar.Core.Interception;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.GAgent;

public abstract partial class
    MemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : MemberState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MemberConfigDto
{
    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEvent @event)
    {
        var score = await GetInterestValueAsync();

        await PublishAsync(new EvaluationInterestResponseEvent()
        {
            MemberId = this.GetPrimaryKey(), BlackboardId = BlackboardId, InterestValue = score,
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

        try
        {
            var talkResponse = await ChatAsync(@event.CoordinatorMessages);
            await PublishAsync(new ChatResponseEvent()
            {
                BlackboardId = BlackboardId, MemberId = this.GetPrimaryKey(), MemberName = State.MemberName,
                ChatResponse = talkResponse, Term = @event.Term
            });
        }
        catch (Exception e)
        {
            Logger.LogError($"[MemberGAgentBase] Handler ChatEvent fail: {e.Message}");
            await PublishAsync(new ChatResponseEvent()
            {
                BlackboardId = BlackboardId, MemberId = this.GetPrimaryKey(), MemberName = State.MemberName,
                FailureSummary = e.ToString(), Term = @event.Term
            });
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        await GroupChatFinishAsync();
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPingEvent @event)
    {
        if (await IgnoreBlackboardPingEvent() == false)
        {
            await PublishAsync(new CoordinatorPongEvent()
            { BlackboardId = BlackboardId, MemberId = this.GetPrimaryKey(), MemberName = State.MemberName });
        }
    }

    protected abstract Task<int> GetInterestValueAsync();

    protected abstract Task<ChatResponse> ChatAsync(List<ChatMessage>? coordinatorMessages);

    protected virtual Task GroupChatFinishAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent()
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Workflow ID for this member instance, used by InterceptorAttribute for workflow logging
    /// </summary>
    public virtual string? WorkflowId { get; protected set; }

    /// <summary>
    /// BlackboardId as Guid, computed from WorkflowId
    /// </summary>
    public virtual Guid BlackboardId 
    { 
        get 
        {
            if (string.IsNullOrEmpty(WorkflowId)) return Guid.Empty;
            return Guid.TryParse(WorkflowId, out var guid) ? guid : Guid.Empty;
        }
    }

    // Workflow logging configuration constants
    /// <summary>
    /// Log category constant for workflow interceptor
    /// </summary>
    protected const string WorkflowLogCategory = "WORKFLOW";

    /// <summary>
    /// Workflow context property constants for interceptor
    /// </summary>
    protected const string WorkflowIdProperty = "WorkflowId";

    /// <summary>
    /// Override to automatically extract WorkflowId from ResourceContext metadata
    /// and set it as instance property for use by InterceptorAttribute
    /// </summary>
    protected override async Task OnPrepareResourceContextAsync(ResourceContext context)
    {
        await base.OnPrepareResourceContextAsync(context);

        // Extract WorkflowId from metadata if available and set as instance property
        if (context.Metadata.TryGetValue("WorkflowId", out var workflowIdObj))
        {
            WorkflowId = workflowIdObj?.ToString();
            
            Logger.LogInformation("[MemberGAgentBase] Set WorkflowId property from ResourceContext: WorkflowId={WorkflowId}", 
                WorkflowId);
        }
    }

    [GenerateSerializer]
    public class SetMemberNameLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public string MemberName { get; set; }
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        RaiseEvent(new SetMemberNameLogEvent() { MemberName = configuration.MemberName });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetMemberNameLogEvent @setMemberNameLogEvent:
                state.MemberName = @setMemberNameLogEvent.MemberName;
                return;
        }

        MemberTransitionState(state, @event);
    }

    protected virtual void MemberTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
    }

    protected async Task<List<ChatMessage>> GetMessageFromBlackboardAsync(Guid blackboardId)
    {
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(blackboardId);
        var history = await blackboard.GetContent();

        return history;
    }
}