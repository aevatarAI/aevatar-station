using System.Collections.Generic;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class ToolCallDetail
{
    [Id(0)] public string ToolName { get; set; } = string.Empty;
    [Id(1)] public string ServerName { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object?> Arguments { get; set; } = new();
    [Id(3)] public string Result { get; set; } = string.Empty;
    [Id(4)] public bool Success { get; set; }
    [Id(5)] public long DurationMs { get; set; }
    [Id(6)] public string Timestamp { get; set; } = string.Empty;
}