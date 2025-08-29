namespace Aevatar.GAgents.AIGAgent.Dtos;

/// <summary>
/// Represents a tool call history entry in state
/// </summary>
[GenerateSerializer]
public class ToolCallHistoryEntry
{
    [Id(0)] public List<ToolCallDetail> ToolCalls { get; set; } = new();
    [Id(1)] public DateTime Timestamp { get; set; }
    [Id(2)] public string RequestId { get; set; } = string.Empty;
} 