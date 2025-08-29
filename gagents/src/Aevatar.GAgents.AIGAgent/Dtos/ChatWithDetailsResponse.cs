using System.Collections.Generic;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class ChatWithDetailsResponse
{
    [Id(0)] public string Response { get; set; } = string.Empty;
    [Id(1)] public List<ToolCallDetail> ToolCalls { get; set; } = new();
    [Id(2)] public long TotalDurationMs { get; set; }
}