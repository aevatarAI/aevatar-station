using Orleans;

namespace Aevatar.Code.GEvents;

[GenerateSerializer]
public class AddCodeAgentGEvent : CodeAgentGEvent
{
    [Id(1)] public string WebhookId { get; set; }
    [Id(2)] public string WebhookVersion { get; set; }
    
    [Id(3)] public byte[] Code { get; set; }
}