using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Investment.Dtos;

[GenerateSerializer]
public class InvestmentConfiguration : ConfigurationBase
{
    [Id(0)] public string InvestmentContent { get; set; }
    [Id(1)] public int Number { get; set; }
}