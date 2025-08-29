using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;

[GenerateSerializer]
public class TransactionStateLogEvent : StateLogEventBase <TransactionStateLogEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
}