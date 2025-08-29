using System;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;

public  class TransactionSuccessStateLogEvent : TransactionStateLogEvent
{
    [Id(1)] public Guid CreateTransactionGEventId { get; set; }
}