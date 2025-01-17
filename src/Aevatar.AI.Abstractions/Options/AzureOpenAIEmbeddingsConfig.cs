using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

/// <summary>
/// Azure OpenAI Embeddings service settings.
/// </summary>
public class AzureOpenAIEmbeddingsConfig
{
    public const string ConfigSectionName = "AzureOpenAIEmbeddings";

    [Required]
    public string DeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
