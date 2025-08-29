using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class TextToImageContextDto
{
    [Id(0)] public string Context { get; set; }
}