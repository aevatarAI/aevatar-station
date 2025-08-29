using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.AITextToImageWorker;

[GenerateSerializer]
public class AITextToImageRequest
{
    [Id(0)] public LLMConfig LlmConfig { get; set; }
    [Id(1)] public string Prompt { get; set; }
    [Id(2)] public TextToImageOption TextToImageOption { get; set; }
    [Id(3)] public TextToImageContextDto Context { get; set; }
}