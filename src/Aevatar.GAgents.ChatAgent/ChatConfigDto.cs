using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.ChatAgent.Dtos;

[GenerateSerializer]
public class ChatConfigDto : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "Instructions are required")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Instructions must be between 1 and 2000 characters")]
    public string Instructions { get; set; } = "You are a helpful AI assistant";

    [Id(1)] 
    [Required(ErrorMessage = "LLM Configuration is required")]
    public LLMConfigDto LLMConfig { get; set; }

    [Id(2)]
    [Range(1, 100, ErrorMessage = "Max History Count must be between 1 and 100")]
    public int MaxHistoryCount { get; set; } = 20;

    [Id(3)]
    public bool StreamingModeEnabled { get; set; } = true;

    [Id(4)] 
    public StreamingConfig StreamingConfig { get; set; }
}