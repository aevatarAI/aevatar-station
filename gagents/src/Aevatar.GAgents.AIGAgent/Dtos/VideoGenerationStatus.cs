using System;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class VideoGenerationStatus
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string Status { get; set; } = string.Empty; // pending, processing, completed, failed
    [Id(2)] public string VideoUrl { get; set; } = string.Empty;
    [Id(3)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(4)] public int Progress { get; set; } = 0; // 0-100
    [Id(5)] public DateTime CreatedAt { get; set; }
    [Id(6)] public DateTime? CompletedAt { get; set; }
}
