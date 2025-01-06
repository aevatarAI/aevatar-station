using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

/// <summary>
/// Azure OpenAI service settings.
/// </summary>
public sealed class AzureOpenAIConfig
{
    public const string ConfigSectionName = "AzureOpenAI";

    [Required]
    public string ChatDeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
