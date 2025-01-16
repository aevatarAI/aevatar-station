using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Investment;


public class InvestmentLogEvent : StateLogEventBase<InvestmentLogEvent>
{
    [Id(0)] public Guid Id { get; set; }
}