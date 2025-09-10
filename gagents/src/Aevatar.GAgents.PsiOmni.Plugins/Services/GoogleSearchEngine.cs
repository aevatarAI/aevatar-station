using Aevatar.GAgents.PsiOmni.Plugins.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Plugins.Web.Google;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Google search engine implementation using Microsoft.SemanticKernel.Plugins.Web.Google.GoogleTextSearch
/// </summary>
public class GoogleSearchEngine : ISearchEngine
{
    private readonly ILogger<GoogleSearchEngine> _logger;
    private readonly GoogleTextSearch? _googleTextSearch;

    public string Name => "google";

    public GoogleSearchEngine(ILogger<GoogleSearchEngine> logger)
    {
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        var searchEngineId = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_ENGINE_ID");

        if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(searchEngineId))
        {
            _googleTextSearch = new GoogleTextSearch(searchEngineId, apiKey);
            _logger.LogInformation("Google search engine initialized successfully");
        }
        else
        {
            _logger.LogWarning(
                "Google API credentials not configured. Missing GOOGLE_API_KEY or GOOGLE_SEARCH_ENGINE_ID");
        }
    }

    public async Task<List<SearchResult>> PerformSearchAsync(
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken = default)
    {
        if (_googleTextSearch == null)
        {
            _logger.LogWarning("Google search service not available - missing API credentials");
            return new List<SearchResult>();
        }

        try
        {
            _logger.LogInformation("Performing Google search for query: {Query} with {NumResults} results", query,
                numResults);

            // Use Microsoft.SemanticKernel.Plugins.Web.Google.GoogleTextSearch
            // Using simple query without options for now since the API is experimental
            var searchResults = await _googleTextSearch.GetTextSearchResultsAsync(
                query,
                null, // options
                cancellationToken);

            var results = new List<SearchResult>();
            var position = 1;

            // Convert KernelSearchResults to our SearchResult format
            var resultCount = 0;
            await foreach (var result in searchResults.Results)
            {
                if (resultCount >= numResults) break;

                results.Add(new SearchResult
                {
                    Position = position++,
                    Url = result.Link?.ToString() ?? string.Empty,
                    Title = result.Name ?? $"Result {position - 1}",
                    Description = result.Value ?? string.Empty,
                    Source = Name
                });
                resultCount++;
            }

            _logger.LogInformation("Google search completed successfully with {ResultCount} results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Google search for query: {Query}", query);
            return new List<SearchResult>();
        }
    }
}