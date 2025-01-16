using System;
using System.Collections.Generic;
using Aevatar.Agents.Atomic.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Agents.Atomic;

public class AtomicGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserAddress { get; set; }
    [Id(2)] public List<string> Groups { get; set; } = new List<string>();
    [Id(3)] public string Type { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
}