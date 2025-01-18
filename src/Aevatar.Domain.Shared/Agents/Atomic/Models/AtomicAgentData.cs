using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Atomic.Models;

[GenerateSerializer]
public class AtomicAgentData
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public Dictionary<string, string> Groups { get; set; }
    [Id(3)] public string Type { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
}