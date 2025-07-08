using System.Collections.Generic;
using Aevatar.Station.Feature.CreatorGAgent;
using Orleans;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class UpdateAvailableEventsGEvent : CreatorAgentGEvent
{
    [Id(0)] public List<EventDescription> EventInfoList { get; set; }
}