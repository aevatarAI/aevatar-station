namespace Aevatar.GAgents.Router.GAgents.SEvents;

[GenerateSerializer]
public class SetTaskInfoSEvent : RouterGAgentSEvent
{
    [Id(0)] public Guid TaskId { get; set; }
    [Id(1)] public string TaskDescription { get; set; }
}