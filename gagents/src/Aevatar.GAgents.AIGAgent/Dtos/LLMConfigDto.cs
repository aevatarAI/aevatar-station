using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.GAgents.AI.Options;
using Newtonsoft.Json;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class LLMConfigDto
{
    [Id(0)]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "System LLM must be between 1 and 100 characters")]
    public string? SystemLLM { get; set; } = "OpenAI";
    
    [Id(1)]
    public SelfLLMConfig? SelfLLMConfig { get; set; } = null;
}

[GenerateSerializer]
public class SelfLLMConfig
{
    [Required] [Id(0)] public LLMProviderEnum ProviderEnum { get; set; }
    
    [Required] [Id(1)] public ModelIdEnum ModelId { get; set; }
    
    [Id(2)] 
    [StringLength(200, ErrorMessage = "Model Name must not exceed 200 characters")]
    public string ModelName { get; set; }
    
    [Id(4)] 
    [StringLength(500, ErrorMessage = "API Key must not exceed 500 characters")]
    public string ApiKey { get; set; }
    
    [Id(3)] 
    [StringLength(500, ErrorMessage = "Endpoint must not exceed 500 characters")]
    [Url(ErrorMessage = "Endpoint must be a valid URL")]
    public string Endpoint { get; set; }
    
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