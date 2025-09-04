using Orleans;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[GenerateSerializer]
public class VideoGenerationEvent : StateLogEventBase<VideoGenerationEvent>
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string Prompt { get; set; } = string.Empty;
    [Id(2)] public string? ImageUrl { get; set; }
    [Id(3)] public VideoGenerationConfigDto? Options { get; set; }
}

[GenerateSerializer]
public class VideoGenerationConfigurationSetEvent : VideoGenerationEvent
{
    [Id(0)] public string Instructions { get; set; } = "Create a high-quality video from the following description";
    [Id(1)] public string GenerationType { get; set; } = "text-to-video";
    [Id(2)] public int Duration { get; set; } = 5;
    [Id(3)] public string Resolution { get; set; } = "720p";
    [Id(4)] public string Style { get; set; } = "cinematic";
    [Id(5)] public string ImageUrl { get; set; } = string.Empty;
    [Id(6)] public bool AutoReturnResult { get; set; } = true;
}
