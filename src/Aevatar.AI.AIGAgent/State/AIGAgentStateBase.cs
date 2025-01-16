using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.AI.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public string LLM { get; set; } = "AzureOpenAI";

    [Id(1)] public List<Guid> Stores { get; set; } = [];
}