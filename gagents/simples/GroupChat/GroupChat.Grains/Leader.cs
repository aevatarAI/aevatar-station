using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;

namespace GroupChat.Grain;

public class Leader : GroupMemberGAgentBase<GroupMemberState, LeaderEventLog, EventBase, GroupMemberConfigDto>, ILeader
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Leader");
    }

    protected override async Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        var messages = await GetMessageFromBlackboardAsync(blackboardId);
        if (messages.Count > 10)
        {
            return 100;
        }

        return 0;
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse();
        Console.WriteLine($"{State.MemberName} Can Speak");
        if (messages.Count() < 10)
        {
            response.Skip = true;
            return Task.FromResult(response);
        }

        response.Continue = false;
        response.Content = "Discussion ended";
        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        Console.WriteLine($"{State.MemberName} receive finish message");
        return Task.CompletedTask;
    }
}

public interface ILeader : IGAgent
{
}

[GenerateSerializer]
public class LeaderEventLog : StateLogEventBase<LeaderEventLog>
{
}