using GroupChat.GAgent.Feature.Common;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(1);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
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
        return new ChatResponse
        {
            Continue = true,
            Skip = false,
            Content = "Started"
        };
    }
}