using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Orleans;

namespace GroupChat.Grain;

[GenerateSerializer]
public class CoordinatorLogEvent : StateLogEventBase<CoordinatorLogEvent>
{
}

public class Coordinator : CoordinatorGAgentBase<CoordinatorStateBase, CoordinatorLogEvent>
{
    protected override Task<bool> NeedCheckMemberInterestValue(List<GroupMember> members, Guid blackboardId)
    {
        return Task.FromResult(true);
    }
}