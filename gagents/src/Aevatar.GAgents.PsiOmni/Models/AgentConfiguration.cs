namespace Aevatar.GAgents.PsiOmni.Models;

/// <summary>
/// Configuration for a configurable agent, defining its behavior and model settings
/// </summary>
[Serializable]
[GenerateSerializer]
public class AgentConfiguration
{
    /// <summary>
    /// Temperature for AI responses (0.0 = deterministic, 2.0 = very creative)
    /// </summary>
    [Id(0)]
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Maximum number of tokens for AI responses
    /// </summary>
    [Id(1)]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Model configuration for AI integration
    /// </summary>
    [Id(2)]
    public ModelConfiguration Model { get; set; } = new();
}

/// <summary>
/// Configuration for the AI model to use - supports both OpenAI and Azure OpenAI
/// </summary>
[Serializable]
[GenerateSerializer]
public class ModelConfiguration
{
    /// <summary>
    /// The model ID to use (e.g., "gpt-4o-mini") for OpenAI or deployment name for Azure OpenAI
    /// </summary>
    [Id(0)]
    public string ModelId { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// API key for the model provider (optional - can use environment variable)
    /// </summary>
    [Id(1)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for custom model providers (optional) - for standard OpenAI
    /// </summary>
    [Id(2)]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Azure OpenAI deployment name (required for Azure OpenAI)
    /// </summary>
    [Id(3)]
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Azure OpenAI endpoint URL (required for Azure OpenAI, e.g., "https://contoso.openai.azure.com/")
    /// </summary>
    [Id(4)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Azure OpenAI API version (optional, defaults to latest if not specified)
    /// </summary>
    [Id(5)]
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Determines if this configuration is for Azure OpenAI based on presence of Endpoint
    /// </summary>
    public bool IsAzureOpenAI => !string.IsNullOrEmpty(Endpoint);
} 