using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;

namespace Aevatar.GAgents.GroupChat.Test.GAgents;

public class LeaderGAgentGAgent : GroupMemberGAgentBase<LeaderState, LeaderEventLog, EventBase, GroupMemberConfigDto>, ILeaderGAgent
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

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse();
        RaiseEvent(new LeaderHandleMessageLogEvent()
        {
            PreWorkUnits = messages.Select(s => s.AgentName).ToList()
        });
        await ConfirmEvents();
        response.Continue = false;
        response.Content = "Discussion ended";
        return response;
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    protected override void GroupMemberTransitionState(LeaderState state, StateLogEventBase<LeaderEventLog> @event)
    {
        switch (@event)
        {
            case LeaderHandleMessageLogEvent handleMessageLogEvent:
                state.AgentNames = handleMessageLogEvent.PreWorkUnits;
                break;
        }
    }
}

public interface ILeaderGAgent : IStateGAgent<LeaderState>
{
}

[GenerateSerializer]
public class LeaderEventLog : StateLogEventBase<LeaderEventLog>
{
}

[GenerateSerializer]
public class LeaderHandleMessageLogEvent : LeaderEventLog
{
    [Id(0)] public List<string> PreWorkUnits { get; set; } = new List<string>();
}

[GenerateSerializer]
public class LeaderState : GroupMemberState
{
    [Id(0)] public List<string> AgentNames { get; set; } = new List<string>();
}