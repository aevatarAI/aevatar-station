namespace Aevatar.CombinationAgent.Agent.GEvents;

public class UpdateAgentGEvent : CombinationAgentGEvent
{
    [Id(2)] public string Name { get; set; }
    [Id(4)] public List<string> AgentComponent { get; set; }
}