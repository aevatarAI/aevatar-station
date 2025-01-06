using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.AI.Events;

[GenerateSerializer]
public class AIEventBase : GEventBase
{
    [Id(0)] public string LLM { get; set; } = "AzureOpenAI";
    [Id(1)] public Guid Store { get; set; }
}