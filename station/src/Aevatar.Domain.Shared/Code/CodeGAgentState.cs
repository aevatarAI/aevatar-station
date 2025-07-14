using System;
using Aevatar.Code.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Code;

[GenerateSerializer]
public class CodeGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
}