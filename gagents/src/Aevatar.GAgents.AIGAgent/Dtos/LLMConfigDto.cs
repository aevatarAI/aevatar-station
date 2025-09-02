using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.GAgents.AI.Options;
using Newtonsoft.Json;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class LLMConfigDto
{
    [Id(0)] public string? SystemLLM { get; set; }
    
    [Id(1)] public SelfLLMConfig? SelfLLMConfig { get; set; } = null;
}

[GenerateSerializer]
public class SelfLLMConfig
{
    [Required] [Id(0)] public LLMProviderEnum ProviderEnum { get; set; }
    [Required] [Id(1)] public ModelIdEnum ModelId { get; set; }
    [Id(2)] public string ModelName { get; set; }
    [Id(4)] public string ApiKey { get; set; }
    [Id(3)] public string Endpoint { get; set; }
    [Id(5)] public Dictionary<string, object>? Memo { get; set; } = null;

    public LLMConfig ConvertToLLMConfig()
    {
        return new LLMConfig()
        {
            ProviderEnum = ProviderEnum,
            ModelIdEnum = ModelId,
            ModelName = ModelName,
            ApiKey = ApiKey,
            Endpoint = Endpoint,
            Memo = Memo
        };
    }
}