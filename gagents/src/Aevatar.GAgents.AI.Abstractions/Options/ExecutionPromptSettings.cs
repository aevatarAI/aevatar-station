using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class ExecutionPromptSettings
{
    [Id(0)] public string Temperature { get; set; }
    [Id(1)] public int MaxToken { get; set; }
}