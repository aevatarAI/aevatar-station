using System;
using Orleans;

namespace Aevatar.GAgents.AI.Common;

[GenerateSerializer]
public class TokenUsageStatistics
{
    [Id(0)] public int InputToken { get; set; }
    [Id(1)] public int OutputToken { get; set; }
    [Id(2)] public int TotalUsageToken { get; set; }
    [Id(3)] public long CreateTime { get; set; }
}