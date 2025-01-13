using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Investment;

[GenerateSerializer]
public class InvestmentAgentState : StateBase
{
    [Id(0)]  public List<string> Content { get; set; }
}