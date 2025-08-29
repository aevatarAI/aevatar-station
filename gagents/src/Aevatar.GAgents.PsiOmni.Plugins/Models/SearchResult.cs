using System.Text.Json.Serialization;

namespace Aevatar.GAgents.PsiOmni.Plugins.Models;

/// <summary>
/// Represents a single search result from a web search operation
/// Matches the OpenManus SearchResult structure
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Position in search results
    /// </summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>
    /// URL of the search result
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Title of the search result
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description or snippet
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The search engine that provided this result
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Raw content from the search result page if available
    /// </summary>
    [JsonPropertyName("raw_content")]
    public string? RawContent { get; set; }
}

/// <summary>
/// Metadata about the search operation
/// Matches OpenManus SearchMetadata structure
/// </summary>
public class SearchMetadata
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("num_results")]
    public int NumResults { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "us";

    [JsonPropertyName("search_engine")]
    public string SearchEngine { get; set; } = string.Empty;

    [JsonPropertyName("fetch_content")]
    public bool FetchContent { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Structured response from web search
/// Matches OpenManus SearchResponse structure
/// </summary>
public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();

    [JsonPropertyName("metadata")]
    public SearchMetadata Metadata { get; set; } = new();

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("total_results")]
    public long TotalResults { get; set; }
}