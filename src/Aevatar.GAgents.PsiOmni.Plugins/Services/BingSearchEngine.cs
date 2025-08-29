
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.GAgents.PsiOmni.Plugins.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Bing search engine implementation (fallback engine)
/// </summary>
public class BingSearchEngine : ISearchEngine
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BingSearchEngine> _logger;
    private readonly string? _apiKey;
    private const string BingSearchApiUrl = "https://api.bing.microsoft.com/v7.0/search";

    public string Name => "bing";

    public BingSearchEngine(HttpClient httpClient, ILogger<BingSearchEngine> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("BING_API_KEY");
    }

    public async Task<List<SearchResult>> PerformSearchAsync(
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Bing API key not configured, skipping Bing search");
            return new List<SearchResult>();
        }

        try
        {
            var searchUrl = BuildSearchUrl(query, numResults, lang, country);
            
            var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bing search failed with status: {StatusCode}", response.StatusCode);
                return new List<SearchResult>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var bingResponse = JsonSerializer.Deserialize<BingSearchResponse>(responseContent);

            if (bingResponse?.WebPages?.Value == null)
            {
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();
            for (int i = 0; i < Math.Min(bingResponse.WebPages.Value.Length, numResults); i++)
            {
                var item = bingResponse.WebPages.Value[i];
                results.Add(new SearchResult
                {
                    Position = i + 1,
                    Url = item.Url ?? string.Empty,
                    Title = item.Name ?? $"Result {i + 1}",
                    Description = item.Snippet ?? string.Empty,
                    Source = Name
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Bing search for query: {Query}", query);
            return new List<SearchResult>();
        }
    }

    private string BuildSearchUrl(string query, int numResults, string? lang, string? country)
    {
        var url = $"{BingSearchApiUrl}?q={Uri.EscapeDataString(query)}&count={Math.Min(numResults, 50)}";

        if (!string.IsNullOrEmpty(lang))
        {
            url += $"&setLang={lang}";
        }
        if (!string.IsNullOrEmpty(country))
        {
            url += $"&cc={country.ToUpper()}";
        }

        return url;
    }

    private class BingSearchResponse
    {
        [JsonPropertyName("webPages")]
        public BingWebPages? WebPages { get; set; }
    }

    private class BingWebPages
    {
        [JsonPropertyName("value")]
        public BingSearchItem[]? Value { get; set; }
    }

    private class BingSearchItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }
    }
}