using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.GroupGAgent;

[GenerateSerializer]
public class GroupGAgentState : StateBase
{
    [Id(0)]  public int RegisteredGAgents { get; set; } = 0;
}