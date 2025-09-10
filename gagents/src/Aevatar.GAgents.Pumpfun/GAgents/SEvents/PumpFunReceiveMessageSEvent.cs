using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.PumpFun.Agent.GEvents;
[GenerateSerializer]
public class PumpFunReceiveMessageGEvent : PumpfunSEventBase
{
    [Id(0)]  public string? ChatId { get; set; }
    [Id(1)]  public string? ReplyId { get; set; }
    [Id(2)] public string? RequestMessage { get; set; }
}

public class PumpfunSEventBase : StateLogEventBase<PumpfunSEventBase>
{
    
}

[GenerateSerializer]
public class PumpfunRequestSEvent : PumpfunSEventBase
{
    [Id(0)] public Guid RequestId { get; set; }
}

[GenerateSerializer]
public class PumpfunSocialResponseSEvent : PumpfunSEventBase
{
    [Id(0)] public Guid ResponseId { get; set; }
}