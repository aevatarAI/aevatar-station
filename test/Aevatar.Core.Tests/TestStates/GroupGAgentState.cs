using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestStates;

[GenerateSerializer]
public class GroupGAgentState : StateBase
{
    [Id(0)]  public int RegisteredGAgents { get; set; } = 0;
}