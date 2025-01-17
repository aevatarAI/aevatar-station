using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.AI.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public string LLM { get; set; } = string.Empty;

    [Id(1)] public string PromptTemplate { get; set; } = string.Empty;
}