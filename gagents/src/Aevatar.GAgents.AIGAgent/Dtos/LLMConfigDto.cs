using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Aevatar.GAgents.AI.Options;
using Newtonsoft.Json;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class LLMConfigDto
{
    [Id(0)]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "System LLM must be between 1 and 100 characters")]
    [Description("The system-level LLM configuration name to use, references a pre-configured LLM setup")]
    public string? SystemLLM { get; set; } = "OpenAI";
    
    [Id(1)]
    [Description("Self-contained LLM configuration with provider, model, and API settings")]
    public SelfLLMConfig? SelfLLMConfig { get; set; } = null;
}

[GenerateSerializer]
public class SelfLLMConfig
{
    [Required] 
    [Id(0)] 
    [Description("The LLM provider to use (e.g., OpenAI, Azure, Anthropic)")]
    public LLMProviderEnum ProviderEnum { get; set; }
    
    [Required] 
    [Id(1)] 
    [Description("The specific model ID to use from the selected provider")]
    public ModelIdEnum ModelId { get; set; }
    
    [Id(2)] 
    [StringLength(200, ErrorMessage = "Model Name must not exceed 200 characters")]
    [Description("Custom model name, used when the standard ModelId doesn't match the actual model name")]
    public string ModelName { get; set; }
    
    [Id(4)] 
    [StringLength(500, ErrorMessage = "API Key must not exceed 500 characters")]
    [Description("The API key for authenticating with the LLM provider")]
    public string ApiKey { get; set; }
    
    [Id(3)] 
    [StringLength(500, ErrorMessage = "Endpoint must not exceed 500 characters")]
    [Url(ErrorMessage = "Endpoint must be a valid URL")]
    [Description("The API endpoint URL for the LLM provider service")]
    public string Endpoint { get; set; }
    
    [Id(5)] 
    [Description("Additional configuration parameters and metadata for the LLM setup")]
    public Dictionary<string, object>? Memo { get; set; } = null;

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