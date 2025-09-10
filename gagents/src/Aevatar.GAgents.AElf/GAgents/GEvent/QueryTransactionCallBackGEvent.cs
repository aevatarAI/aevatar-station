using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.Event;

[GenerateSerializer]
public  class QueryTransactionCallBackGEvent : EventBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string ChainId { get; set; }
    [Id(2)] public string TransactionId { get; set; }
    [Id(3)] public Guid CreateTransactionGEventId { get; set; }
    [Id(4)] public bool IsSuccess   { get; set; }
    [Id(5)] public string  Error  { get; set; }
    
    
}