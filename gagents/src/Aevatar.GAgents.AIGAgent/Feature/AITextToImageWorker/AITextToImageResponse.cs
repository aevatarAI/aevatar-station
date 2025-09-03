using System.Collections.Generic;
using Aevatar.AI.Exceptions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.AITextToImageWorker;

[GenerateSerializer]
public class AITextToImageResponse
{
    [Id(0)] public TextToImageContextDto Context { get; set; }
    [Id(1)] public string? ErrorMessage { get; set; }
    [Id(2)] public AIExceptionEnum ErrorEnum { get; set; } =  AIExceptionEnum.None;
    [Id(3)] public TextToImageOption TextToImageOption { get; set; }
    [Id(4)] public List<TextToImageResponse>? ImageResponses = null;
}