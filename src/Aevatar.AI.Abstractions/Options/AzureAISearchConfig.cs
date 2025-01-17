using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

/// <summary>
/// Azure AI Search service settings.
/// </summary>
internal sealed class AzureAISearchConfig
{
    public const string ConfigSectionName = "AzureAISearch";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
