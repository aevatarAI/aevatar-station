using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

public sealed class GeminiConfig
{
    public const string ConfigSectionName = "Gemini";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}