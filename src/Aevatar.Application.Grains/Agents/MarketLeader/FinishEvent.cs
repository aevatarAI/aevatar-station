using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.MarketLeader;

[GenerateSerializer]
public class FinishEvent : EventBase
{
    [Description("Unique identifier for the target chat where the message will be sent.")]
    [Id(0)]  public string Id { get; set; }
    [Description("Text content of the message to be sent.")]
    [Id(1)]  public string Message { get; set; }
}