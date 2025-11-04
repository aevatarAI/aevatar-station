using System.Collections.Generic;
using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class SystemLLMConfigOptions
{
    [Id(0)] public Dictionary<string, LLMConfig>? SystemLLMConfigs { get; set; }
}