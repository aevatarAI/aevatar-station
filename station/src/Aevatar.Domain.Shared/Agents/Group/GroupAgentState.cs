using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Group;

[GenerateSerializer]
public class GroupAgentState : StateBase
{
    [Id(0)]  public int RegisteredAgents { get; set; } = 0;
}