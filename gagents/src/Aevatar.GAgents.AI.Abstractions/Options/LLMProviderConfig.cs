using System.ComponentModel.DataAnnotations;
using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class LLMProviderConfig
{
    [Required]
    [Id(0)] public LLMProviderEnum ProviderEnum { get; set; }
    
    [Required]
    [Id(1)] public ModelIdEnum ModelIdEnum { get; set; }
}