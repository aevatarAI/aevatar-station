using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.GAgents.PsiOmni.Plugins.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// DuckDuckGo search engine implementation (fallback engine)
/// </summary>
public class DuckDuckGoSearchEngine : ISearchEngine
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DuckDuckGoSearchEngine> _logger;
    private const string DuckDuckGoApiUrl = "https://api.duckduckgo.com/";

    public string Name => "duckduckgo";

    public DuckDuckGoSearchEngine(HttpClient httpClient, ILogger<DuckDuckGoSearchEngine> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SearchResult>> PerformSearchAsync(
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // DuckDuckGo Instant Answer API - limited but free
            var searchUrl = $"{DuckDuckGoApiUrl}?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            
            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DuckDuckGo search failed with status: {StatusCode}", response.StatusCode);
                return new List<SearchResult>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var duckResponse = JsonSerializer.Deserialize<DuckDuckGoResponse>(responseContent);

            var results = new List<SearchResult>();

            // Add instant answer if available
            if (!string.IsNullOrEmpty(duckResponse?.AbstractText))
            {
                results.Add(new SearchResult
                {
                    Position = 1,
                    Url = duckResponse.AbstractURL ?? "https://duckduckgo.com",
                    Title = duckResponse.Heading ?? "DuckDuckGo Result",
                    Description = duckResponse.AbstractText,
                    Source = Name
                });
            }

            // Add related topics
            if (duckResponse?.RelatedTopics != null)
            {
                for (int i = 0; i < Math.Min(duckResponse.RelatedTopics.Length, numResults - results.Count); i++)
                {
                    var topic = duckResponse.RelatedTopics[i];
                    if (!string.IsNullOrEmpty(topic.Text))
                    {
                        results.Add(new SearchResult
                        {
                            Position = results.Count + 1,
                            Url = topic.FirstURL ?? "https://duckduckgo.com",
                            Title = ExtractTitle(topic.Text),
                            Description = topic.Text,
                            Source = Name
                        });
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing DuckDuckGo search for query: {Query}", query);
            return new List<SearchResult>();
        }
    }

    private static string ExtractTitle(string text)
    {
        // Extract title from DuckDuckGo format: "Title - Description"
        var parts = text.Split(" - ", 2);
        return parts.Length > 0 ? parts[0] : "DuckDuckGo Result";
    }

    private class DuckDuckGoResponse
    {
        [JsonPropertyName("AbstractText")]
        public string? AbstractText { get; set; }

        [JsonPropertyName("AbstractURL")]
        public string? AbstractURL { get; set; }

        [JsonPropertyName("Heading")]
        public string? Heading { get; set; }

        [JsonPropertyName("RelatedTopics")]
        public DuckDuckGoTopic[]? RelatedTopics { get; set; }
    }

    private class DuckDuckGoTopic
    {
        [JsonPropertyName("Text")]
        public string? Text { get; set; }

        [JsonPropertyName("FirstURL")]
        public string? FirstURL { get; set; }
    }
}