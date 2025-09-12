using System;
using System.Collections.Generic;
using Orleans;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public class VideoGenerationState : AIGAgentStateBase
{
    [Id(0)] public Dictionary<string, VideoGenerationStatus> ActiveTasks { get; set; } = new();
    [Id(1)] public int TotalVideosGenerated { get; set; } = 0;
    [Id(2)] public DateTime LastGenerationTime { get; set; }
    [Id(3)] public string LastGeneratedVideoUrl { get; set; } = string.Empty;
    [Id(4)] public string LastGenerationPrompt { get; set; } = string.Empty;
    
    // Configuration fields
    [Id(5)] public string Instructions { get; set; } = "Create a high-quality video from the following description";
    [Id(6)] public string GenerationType { get; set; } = "text-to-video";
    [Id(7)] public int Duration { get; set; } = 5;
    [Id(8)] public string Resolution { get; set; } = "720p";
    [Id(9)] public string Style { get; set; } = "cinematic";
    [Id(10)] public string ImageUrl { get; set; } = string.Empty;
    [Id(11)] public bool AutoReturnResult { get; set; } = true;
}
