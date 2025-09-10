using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class TextToImageOption
{
    [Id(0)] public string ModelId { get; set; } 
    [Id(1)] public int With { get; set; } = 1024;
    [Id(2)] public int Height { get; set; } = 1024;
    [Id(3)] public int Count { get; set; } = 1;
    [Id(4)] public TextToImageStyleEnum StyleEnum { get; set; } = TextToImageStyleEnum.Vivid;
    [Id(5)] public TextToImageQualityEnum QualityEnum { get; set; } = TextToImageQualityEnum.Standard;
    [Id(6)] public TextToImageResponseType ResponseType { get; set; } = TextToImageResponseType.Base64Content;
}