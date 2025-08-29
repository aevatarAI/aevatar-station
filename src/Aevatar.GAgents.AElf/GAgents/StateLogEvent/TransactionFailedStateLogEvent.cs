using System;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;
[GenerateSerializer]
public  class TransactionFailedStateLogEvent : CreateTransactionStateLogEvent
{
    [Id(1)] public Guid CreateTransactionGEventId { get; set; }
    [Id(2)] public string Error { get; set; }
}