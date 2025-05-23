using Aevatar.Core.Abstractions;
using GroupChat.GAgent;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.GroupChat;

[GAgent(nameof(GroupMemberTest))]
public class GroupMemberTest : GroupMemberGAgentBase<GroupMemberState, GroupMemberTestLogEvent, EventBase,
    GroupMemberConfigDto>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("GroupMemberTest");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(0);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        Logger.LogDebug($"[GroupMemberGAgentBase] grainId:{this.GetGrainId().ToString()} say");
        return Task.FromResult(new ChatResponse() { Content = "ddatata" });
    }
}

public class GroupMemberTestLogEvent : StateLogEventBase<GroupMemberTestLogEvent>
{
}