using Aevatar.GAgents.Router.GAgents.Features.Common;

namespace Aevatar.GAgents.Router.GAgents.SEvents;

[GenerateSerializer]
public class AddHistorySEvent : RouterGAgentSEvent
{
    [Id(0)] public Guid TaskId { get; set; }
    [Id(1)] public RouterRecord RouterRecord { get; set; }
}