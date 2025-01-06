using System;
using System.Collections.Generic;
using Aevatar.AI.Events;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.AI.State;

[GenerateSerializer]
public class AIGAgentState : StateBase
{
    [Id(0)] public string LLM { get; set; } = "AzureOpenAI";

    [Id(1)] public List<Guid> Stores { get; set; } = [];

    public void Apply(AIEventBase @event)
    {
        LLM = @event.LLM;
        Stores.Add(@event.Store);
    }
}