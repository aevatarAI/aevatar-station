using Aevatar.Core.Abstractions;

namespace Aevatar.Station.Feature.CreatorGAgent;


[GenerateSerializer]
public class GroupAgentState : StateBase
{
    [Id(0)]  public int RegisteredAgents { get; set; } = 0;
}