using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Combination.GEvents;

public class UpdateSubscribedEventInfoGEvent : CombinationAgentGEvent
{
    [Id(0)] public List<EventDescription> EventInfoList { get; set; }
}