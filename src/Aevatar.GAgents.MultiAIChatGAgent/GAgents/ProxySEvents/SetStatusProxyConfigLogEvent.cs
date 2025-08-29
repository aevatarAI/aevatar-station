namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;

[GenerateSerializer]
public class SetStatusProxyConfigLogEvent : AIAgentStatusProxyLogEvent
{
    [Id(0)] public TimeSpan? RecoveryDelay { get; set; }
    [Id(1)] public Guid ParentId { get; set; }
}