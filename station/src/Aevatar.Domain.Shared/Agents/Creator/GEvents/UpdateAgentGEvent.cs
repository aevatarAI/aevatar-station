using Orleans;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class UpdateAgentGEvent : CreatorAgentGEvent
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Properties { get; set; }
}