using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

/// <summary>
/// Weaviate service settings.
/// </summary>
internal sealed class WeaviateConfig
{
    public const string ConfigSectionName = "Weaviate";

    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
