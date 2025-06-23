using System.Collections.Generic;
using Orleans;
using Aevatar.Station.Feature.CreatorGAgent;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class UpdateAvailableEventsGEvent : CreatorAgentGEvent
{
    [Id(0)] public List<EventDescription> EventInfoList { get; set; }
}