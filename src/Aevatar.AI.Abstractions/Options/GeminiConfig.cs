using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

public sealed class GeminiConfig
{
    public const string ConfigSectionName = "Gemini";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}