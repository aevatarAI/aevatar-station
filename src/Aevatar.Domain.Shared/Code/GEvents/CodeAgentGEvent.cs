using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Code.GEvents;
[GenerateSerializer]
public class CodeAgentGEvent : StateLogEventBase<CodeAgentGEvent>
{
    [Id(0)]  public override Guid Id { get; set; } = Guid.NewGuid();
}