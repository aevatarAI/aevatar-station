using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class SetSubAgentGEvent : CreatorAgentGEvent
{
    [Id(0)] public List<string> SubAgents { get; set; }
}