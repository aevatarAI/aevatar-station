namespace Aevatar.GAgents.Router.GAgents.SEvents;

[GenerateSerializer]
public class RemoveTaskSEvent: RouterGAgentSEvent
{
    [Id(0)] public Guid TaskId { get; set; }
}