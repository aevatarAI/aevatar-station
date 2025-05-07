namespace Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.ProxySEvents;

[GenerateSerializer]
public class SetStatusProxyConfigLogEvent : AIAgentStatusProxyLogEvent
{
    [Id(0)] public TimeSpan? RecoveryDelay { get; set; }
    [Id(1)] public Guid ParentId { get; set; }
}