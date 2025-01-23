using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Investment;

[GenerateSerializer]
public class InvestmentEvent : EventBase
{
    [Id(0)] public string ResponseContent { get; set; }
    [Description("Unique identifier for the target chat where the message will be sent.")]
    [Id(1)]  public string Content { get; set; }
}