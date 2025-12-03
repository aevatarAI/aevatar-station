using Aevatar.GAgents.Router.GAgents.Features.Common;

namespace Aevatar.GAgents.Router.GAgents.SEvents;

[GenerateSerializer]
public class AddAgentDescriptionSEvent : RouterGAgentSEvent
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public AgentDescriptionInfo AgentDescriptionInfo { get; set; }
}