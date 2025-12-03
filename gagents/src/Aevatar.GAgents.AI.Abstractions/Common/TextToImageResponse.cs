using Orleans;

namespace Aevatar.GAgents.AI.Common;

[GenerateSerializer]
public class TextToImageResponse
{
    [Id(0)] public TextToImageResponseType ResponseType { get; set; }
    [Id(1)] public string Url { get; set; }
    [Id(2)] public string Base64Content { get; set; }
    [Id(3)] public string? ImageType { get; set; }
}