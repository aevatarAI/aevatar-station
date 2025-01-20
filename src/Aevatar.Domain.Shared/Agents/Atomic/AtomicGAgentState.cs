using System;
using System.Collections.Generic;
using Aevatar.Agents.Atomic.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Atomic;

[GenerateSerializer]
public class AtomicGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public Dictionary<string, string> Groups { get; set; } = new();
    [Id(3)] public string Type { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
    [Id(6)] public DateTime CreateTime { get; set; } 
}