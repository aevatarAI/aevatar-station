using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.MarketLeader;

public class MarketLeaderAgentState: StateBase
{
    [Id(0)]  public int Content { get; set; }
}