using System;
using System.Threading.Tasks;
using Orleans;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.AIGAgent.Agent;

public interface IVideoGenerationGAgent : IAIGAgent, IStateGAgent<VideoGenerationState>
{
    Task<string> GenerateVideoFromTextAsync(string prompt, VideoGenerationConfigDto? options = null);
    Task<string> GenerateVideoFromImageAsync(string imageUrl, string prompt, VideoGenerationConfigDto? options = null);
    Task<VideoGenerationStatus> GetVideoStatusAsync(string taskId);
}


