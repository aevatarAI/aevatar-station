using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Investment.Dtos;

public class InvestmentInitializeDto : InitializeDtoBase
{
    [Id(0)] public string Properties { get; set; }
}