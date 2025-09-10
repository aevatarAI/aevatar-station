using System.Text.Json.Serialization;

namespace Aevatar.GAgents.PsiOmni.Plugins.Models;

/// <summary>
/// Configuration for search engines, matching OpenManus config structure
/// </summary>
public class SearchEngineConfiguration
{
    /// <summary>
    /// Preferred search engine (default: "google")
    /// </summary>
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "google";

    /// <summary>
    /// Fallback engines to try if preferred engine fails
    /// </summary>
    [JsonPropertyName("fallback_engines")]
    public List<string> FallbackEngines { get; set; } = new() { "duckduckgo", "bing" };

    /// <summary>
    /// Maximum retry attempts per engine
    /// </summary>
    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff (in seconds)
    /// </summary>
    [JsonPropertyName("retry_delay_base")]
    public double RetryDelayBase { get; set; } = 1.0;

    /// <summary>
    /// Maximum delay for exponential backoff (in seconds)
    /// </summary>
    [JsonPropertyName("retry_delay_max")]
    public double RetryDelayMax { get; set; } = 10.0;
}