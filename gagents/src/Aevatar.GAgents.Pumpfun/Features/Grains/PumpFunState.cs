using System;
using Orleans;

namespace Aevatar.GAgents.PumpFun.Grains;
[GenerateSerializer]
public class PumpFunState
{
    [Id(0)] public  Guid Id { get; set; }
    [Id(1)] public string chatId { get; set; }
    [Id(2)] public string replyId { get; set; }
    [Id(3)] public  string replyMessage { get; set; }
}