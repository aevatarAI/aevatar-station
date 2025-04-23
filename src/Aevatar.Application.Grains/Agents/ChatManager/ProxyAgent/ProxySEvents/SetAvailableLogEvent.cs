namespace Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.ProxySEvents;

[GenerateSerializer]
public class SetAvailableLogEvent : AIAgentStatusProxyLogEvent
{
    [Id(0)] public bool IsAvailable { get; set; }
    [Id(1)] public long ExceptionCount { get; set; }
}