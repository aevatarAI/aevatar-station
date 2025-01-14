using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Investment.Dtos;

[GenerateSerializer]
public class InvestmentInitializeDto : InitializeDtoBase
{
    [Id(0)] public string InvestmentContent { get; set; }
    [Id(1)] public int Number { get; set; }
}