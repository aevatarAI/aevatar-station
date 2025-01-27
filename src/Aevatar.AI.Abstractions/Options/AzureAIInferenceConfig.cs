using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

/// <summary>
/// AzureAI Inference service settings.
/// </summary>
public sealed class AzureAIInferenceConfig
{
    public const string ConfigSectionName = "AzureAIInference";

    [Required]
    public string ChatDeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
