using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.AI.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public required string LLM { get; set; }

    [Id(1)] public required string PromptTemplate { get; set; }
}