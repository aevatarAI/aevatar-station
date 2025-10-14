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
        var score = await GetInterestValueAsync();

        await PublishAsync(new EvaluationInterestResponseEvent()
        {
            MemberId = this.GetPrimaryKey(),
            BlackboardId = BlackboardId,
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

        try
        {
            var talkResponse = await ChatAsync(@event.CoordinatorMessages);
            await PublishAsync(new ChatResponseEvent
            {
                BlackboardId = BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName,
                ChatResponse = talkResponse,
                Term = @event.Term
            });
        }
        catch (Exception e)
        {
            Logger.LogError($"[GroupMemberGAgentBase] Handler ChatEvent fail: {e.Message}");
            await PublishAsync(new ChatResponseEvent()
            {
                BlackboardId = BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName,
                FailureSummary = e.ToString(),
                Term = @event.Term
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
            {
                BlackboardId = BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName
            });
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
    /// Workflow ID for this group member instance, used by InterceptorAttribute for workflow logging
    /// </summary>
    public virtual string? WorkflowId { get; protected set; }

    /// <summary>
    /// Round identifier for current workflow execution cycle. Used by InterceptorAttribute as contextual field.
    /// </summary>
    public virtual long? RoundId { get; protected set; }

    /// <summary>
    /// Grain ID for this group member instance, used by InterceptorAttribute for workflow logging
    /// </summary>
    public virtual string? GrainId { get; protected set; }

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

    /// <summary>
    /// Workflow context property constants for interceptor
    /// </summary>
    protected const string WorkflowLogCategory = "WORKFLOW";
    protected const string WorkflowIdProperty = "WorkflowId";

    /// <summary>
    /// Round context property constant for interceptor
    /// </summary>
    protected const string RoundIdProperty = "RoundId";

    /// <summary>
    /// Grain context property constant for interceptor
    /// </summary>
    protected const string GrainIdProperty = "GrainId";

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
            
            Logger.LogInformation("[GroupMemberGAgentBase] Set WorkflowId property from ResourceContext: WorkflowId={WorkflowId}", 
                WorkflowId);
        }

        // Extract RoundId from metadata if available and set as instance property
        if (context.Metadata.TryGetValue("RoundId", out var roundIdObj))
        {
            if (long.TryParse(roundIdObj?.ToString(), out var parsedRoundId))
            {
                RoundId = parsedRoundId;
                Logger.LogInformation("[GroupMemberGAgentBase] Set RoundId property from ResourceContext: RoundId={RoundId}", 
                    RoundId);
            }
        }

        // Set GrainId for logging context
        GrainId = this.GrainReference.GrainId.ToString();
        Logger.LogInformation("[GroupMemberGAgentBase] Set GrainId property: GrainId={GrainId}", GrainId);
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