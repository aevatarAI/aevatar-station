using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent;

[GenerateSerializer]
public class AIAgentStatusProxyState : AIGAgentStateBase
{
    [Id(0)] public bool IsAvailable { get; set; } = true;
    [Id(1)] public DateTime? UnavailableSince { get; set; }
    [Id(2)] public TimeSpan RecoveryDelay { get; set; } = TimeSpan.FromSeconds(60);
    [Id(3)] public long UnavailableCount { get; set; } = 0;
    [Id(4)] public long ExceptionCount { get; set; } = 0;
    [Id(5)] public Guid ParentId { get; set; }
}