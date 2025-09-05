using System;
using Orleans;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[GenerateSerializer]
public class VideoGenerationEvent : StateLogEventBase<VideoGenerationEvent>
{
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

/// <summary>
/// Combined event for starting video generation and creating the task - eliminates redundancy
/// </summary>
[GenerateSerializer]
public class VideoGenerationStartedEvent : VideoGenerationEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string Prompt { get; set; } = string.Empty;
    [Id(2)] public string? ImageUrl { get; set; }
    [Id(3)] public VideoGenerationConfigDto? Options { get; set; }
    [Id(4)] public string BytePlusTaskId { get; set; } = string.Empty; // Store BytePlus task ID
    [Id(5)] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class VideoGenerationProgressUpdatedEvent : VideoGenerationEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public int Progress { get; set; }
    [Id(2)] public string Status { get; set; } = string.Empty;
}

[GenerateSerializer]
public class VideoGenerationCompletedEvent : VideoGenerationEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string VideoUrl { get; set; } = string.Empty;
    [Id(2)] public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class VideoGenerationFailedEvent : VideoGenerationEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(2)] public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
