using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    protected async Task<int> GetInterestValueAsync()
    {
        return await Task.FromResult(1);
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEvent @event)
    {
        var score = await GetInterestValueAsync();

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
        foreach (var msg in coordinatorMessages ?? new())
        {
            RaiseEventWithTracing(new ReceiveUserMessageEvent
            {
                Event = new UserMessageEvent
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    Content = msg.Content
                },
                BlackboardId = @event.BlackboardId
            });
        }

        await ConfirmEventsWithTracing();
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
                BlackboardId = @event.BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName
            });
        }
    }

    protected Task GroupChatFinishAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent()
    {
        return Task.FromResult(false);
    }
}