using Orleans;

namespace Aevatar.GAgents.AI.Common;

[GenerateSerializer]
public enum TextToImageResponseType
{
    Url,
    Base64Content
}