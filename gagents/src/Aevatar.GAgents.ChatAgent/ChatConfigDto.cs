using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
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
    [Description("System instructions that define the AI assistant's behavior and capabilities")]
    public string Instructions { get; set; } = "You are a helpful AI assistant";

    [Id(1)] 
    [Required(ErrorMessage = "LLM Configuration is required")]
    [Description("Configuration for the Large Language Model to be used by the chat agent")]
    public LLMConfigDto LLMConfig { get; set; }

    [Id(2)]
    [Range(1, 100, ErrorMessage = "Max History Count must be between 1 and 100")]
    [Description("Maximum number of conversation messages to keep in memory for context")]
    public int MaxHistoryCount { get; set; } = 20;

    [Id(3)]
    [Description("Enables streaming mode for real-time response generation")]
    public bool StreamingModeEnabled { get; set; } = true;

    [Id(4)] 
    [Description("Configuration settings for streaming mode behavior and performance")]
    public StreamingConfig StreamingConfig { get; set; }
}