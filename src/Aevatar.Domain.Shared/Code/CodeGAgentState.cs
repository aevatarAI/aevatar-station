using System;
using Aevatar.Code.GEvents;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Code;

[GenerateSerializer]
public class CodeGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    
    [Id(1)] public string WebhookId { get; set; }
    [Id(2)] public string WebhookVersion { get; set; }
    
    [Id(3)] public byte[] Code { get; set; }
    
    
    public void Apply(AddCodeAgentGEvent addCodeAgentGEvent)
    {
        WebhookId =addCodeAgentGEvent.WebhookId;
        WebhookVersion = addCodeAgentGEvent.WebhookVersion;
        Code = addCodeAgentGEvent.Code;
    }
}