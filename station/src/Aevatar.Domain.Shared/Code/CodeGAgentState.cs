using System;
using System.Collections.Generic;
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

    [Id(3)] public Dictionary<string, byte[]> CodeFiles { get; set; } = new();

    public void Apply(AddCodeAgentGEvent addCodeAgentGEvent)
    {
        WebhookId = addCodeAgentGEvent.WebhookId;
        WebhookVersion = addCodeAgentGEvent.WebhookVersion;
        CodeFiles = addCodeAgentGEvent.CodeFiles;
    }
}

[GenerateSerializer]
public class AddCodeAgentGEvent : CodeAgentGEvent
{
    [Id(0)] public string WebhookId { get; set; }
    [Id(1)] public string WebhookVersion { get; set; }
    [Id(3)] public Dictionary<string, byte[]> CodeFiles { get; set; }
}