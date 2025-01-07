namespace Aevatar.AtomicAgent.Agent.GEvents;

[GenerateSerializer]
public class RegisterToGroupGEvent : AtomicAgentGEvent
{
    [Id(0)] public string GroupId { get; set; }
}