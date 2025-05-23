using Aevatar.Core.Abstractions;

namespace MessagingGAgent.Grains.Agents.Group;

[GenerateSerializer]
public class GroupAgentState : StateBase
{
    [Id(0)]  public int RegisteredAgents { get; set; } = 0;
}