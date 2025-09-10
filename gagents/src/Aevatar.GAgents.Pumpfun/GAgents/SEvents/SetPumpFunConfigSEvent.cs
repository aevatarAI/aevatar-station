using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.PumpFun.Agent.GEvents;

[GenerateSerializer]
public class SetPumpFunConfigEvent : PumpfunSEventBase
{
    [Id(0)] public string ChatId { get; set; }
    
    [Id(1)] public string BotName { get; set; }
}