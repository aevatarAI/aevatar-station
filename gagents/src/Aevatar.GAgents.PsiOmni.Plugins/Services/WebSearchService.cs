using Aevatar.GAgents.PsiOmni.Plugins.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Web search service implementation exactly matching OpenManus WebSearch class with _try_all_engines
/// </summary>
public class WebSearchService : IWebSearchService
{
    private readonly ILogger<WebSearchService> _logger;
    private readonly IWebContentFetcher _contentFetcher;
    private readonly Dictionary<string, ISearchEngine> _searchEngines;
    private readonly SearchEngineConfiguration _config;

    public WebSearchService(
        ILogger<WebSearchService> logger,
        IWebContentFetcher contentFetcher,
        IEnumerable<ISearchEngine> searchEngines)
    {
        _logger = logger;
        _contentFetcher = contentFetcher;
        _searchEngines = searchEngines.ToDictionary(e => e.Name, e => e);
        
        // Default configuration matching OpenManus
        _config = new SearchEngineConfiguration
        {
            Engine = "google",
            FallbackEngines = new List<string> { "duckduckgo", "bing" },
            MaxRetries = 3,
            RetryDelayBase = 1.0,
            RetryDelayMax = 10.0
        };
    }

    /// <summary>
    /// Execute web search - matches OpenManus execute method with _try_all_engines
    /// </summary>
    public async Task<SearchResponse> ExecuteAsync(
        string query,
        int numResults = 5,
        string? lang = null,
        string? country = null,
        bool fetchContent = false,
        CancellationToken cancellationToken = default)
    {
        // Set defaults exactly like OpenManus
        lang ??= "en";
        country ??= "us";

        var metadata = new SearchMetadata
        {
            Query = query,
            NumResults = numResults,
            Lang = lang,
            Country = country,
            FetchContent = fetchContent
        };

        var response = new SearchResponse
        {
            Metadata = metadata
        };

        try
        {
            // Try all engines with fallback - matches OpenManus _try_all_engines
            var searchResults = await TryAllEnginesAsync(query, numResults, lang, country, cancellationToken);
            
            if (searchResults.Any())
            {
                // Set the search engine that succeeded
                response.Metadata.SearchEngine = searchResults.First().Source;
                
                // Fetch content if requested
                if (fetchContent)
                {
                    await FetchContentForResultsAsync(searchResults, cancellationToken);
                }

                response.Results = searchResults;
                response.TotalResults = searchResults.Count;
                response.Success = true;
            }
            else
            {
                response.Success = false;
                response.Error = "All search engines failed to return results";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web search for query: {Query}", query);
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    /// <summary>
    /// Try all engines with fallback - exact implementation of OpenManus _try_all_engines
    /// </summary>
    private async Task<List<SearchResult>> TryAllEnginesAsync(
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken)
    {
        var engineOrder = GetEngineOrder();
        var failedEngines = new List<string>();

        foreach (var engineName in engineOrder)
        {
            if (!_searchEngines.TryGetValue(engineName, out var engine))
            {
                continue;
            }

            _logger.LogInformation("🔎 Attempting search with {Engine}...", engineName.ToUpper());

            var searchResults = await PerformSearchWithEngineAsync(
                engine, query, numResults, lang, country, cancellationToken);

            if (searchResults.Any())
            {
                _logger.LogInformation("✅ Search successful with {Engine}, found {Count} results", 
                    engineName.ToUpper(), searchResults.Count);
                return searchResults;
            }

            failedEngines.Add(engineName);
            _logger.LogWarning("❌ Search failed with {Engine}", engineName.ToUpper());
        }

        _logger.LogError("All search engines failed: {FailedEngines}", string.Join(", ", failedEngines));
        return new List<SearchResult>();
    }

    /// <summary>
    /// Get engine order exactly like OpenManus _get_engine_order
    /// </summary>
    private List<string> GetEngineOrder()
    {
        var preferred = _config.Engine.ToLower();
        var fallbacks = _config.FallbackEngines.Select(e => e.ToLower()).ToList();

        // Create engine order: preferred first, then fallbacks, then remaining engines
        var engineOrder = new List<string>();
        
        if (_searchEngines.ContainsKey(preferred))
        {
            engineOrder.Add(preferred);
        }

        foreach (var fallback in fallbacks)
        {
            if (_searchEngines.ContainsKey(fallback) && !engineOrder.Contains(fallback))
            {
                engineOrder.Add(fallback);
            }
        }

        // Add any remaining engines
        foreach (var engineName in _searchEngines.Keys)
        {
            if (!engineOrder.Contains(engineName))
            {
                engineOrder.Add(engineName);
            }
        }

        return engineOrder;
    }

    /// <summary>
    /// Perform search with retry logic - matches OpenManus @retry decorator
    /// </summary>
    private async Task<List<SearchResult>> PerformSearchWithEngineAsync(
        ISearchEngine engine,
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                var results = await engine.PerformSearchAsync(query, numResults, lang, country, cancellationToken);
                if (results.Any())
                {
                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search attempt {Attempt}/{MaxRetries} failed for {Engine}", 
                    attempt, _config.MaxRetries, engine.Name);

                if (attempt < _config.MaxRetries)
                {
                    // Exponential backoff like OpenManus: wait_exponential(multiplier=1, min=1, max=10)
                    var delay = Math.Min(
                        _config.RetryDelayBase * Math.Pow(2, attempt - 1),
                        _config.RetryDelayMax);
                    
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                }
            }
        }

        return new List<SearchResult>();
    }

    /// <summary>
    /// Fetch content for results if requested
    /// </summary>
    private async Task FetchContentForResultsAsync(List<SearchResult> results, CancellationToken cancellationToken)
    {
        var fetchTasks = results.Where(r => !string.IsNullOrEmpty(r.Url))
            .Select(async result =>
            {
                result.RawContent = await _contentFetcher.FetchContentAsync(
                    result.Url, 
                    timeout: 10, 
                    cancellationToken: cancellationToken);
            });

        await Task.WhenAll(fetchTasks);
    }
}