using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    protected async Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return await Task.FromResult(1);
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

        var coordinatorMessages = @event.CoordinatorMessages;
        var blackboardId = @event.BlackboardId;
        foreach (var msg in coordinatorMessages ?? new())
        {
            RaiseEventWithTracing(new ReceiveUserMessageEvent
            {
                Event = new UserMessageEvent
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    Content = msg.Content
                },
                BlackboardId = blackboardId
            });
        }

        await ConfirmEventsWithTracing();
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

    protected Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent(Guid blackboardId)
    {
        return Task.FromResult(false);
    }
}