using GroupChat.GAgent.Feature.Common;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(1);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        return Task.FromResult(new ChatResponse
        {
            Skip = true,
            Continue = false
        });
    }
}