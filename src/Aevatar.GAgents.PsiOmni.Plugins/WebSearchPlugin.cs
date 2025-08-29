using System.ComponentModel;
using System.Text.Json;
using Aevatar.GAgents.PsiOmni.Plugins.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni.Plugins;

/// <summary>
/// WebSearch plugin containing all web search functions
/// </summary>
public class WebSearchPlugin
{
    private readonly IWebSearchService _webSearchService;
    private readonly IWebContentFetcher _webContentFetcher;
    private readonly ILogger<WebSearchPlugin> _logger;

    public WebSearchPlugin(
        IWebSearchService webSearchService,
        IWebContentFetcher webContentFetcher,
        ILogger<WebSearchPlugin> logger)
    {
        _webSearchService = webSearchService;
        _webContentFetcher = webContentFetcher;
        _logger = logger;
    }

    /// <summary>
    /// Perform a web search with the specified query and parameters. Matches OpenManus WebSearch.execute method.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="numResults">Number of results to return (default: 5)</param>
    /// <param name="lang">Language code (default: 'en')</param>
    /// <param name="country">Country code (default: 'us')</param>
    /// <param name="fetchContent">Whether to fetch full content for results (default: false)</param>
    /// <returns>JSON string containing search results</returns>
    [KernelFunction("Execute")]
    [Description("Perform a web search with the specified query and parameters. Matches OpenManus WebSearch.execute method.")]
    public async Task<string> ExecuteAsync(
        [Description("The search query")] string query,
        [Description("Number of results to return (default: 5)")] int numResults = 5,
        [Description("Language code (default: 'en')")] string? lang = null,
        [Description("Country code (default: 'us')")] string? country = null,
        [Description("Whether to fetch full content for results (default: false)")] bool fetchContent = false)
    {
        try
        {
            _logger.LogInformation("Performing web search for query: {Query}", query);
            
            var searchResponse = await _webSearchService.ExecuteAsync(
                query, 
                numResults, 
                lang,
                country,
                fetchContent);

            return JsonSerializer.Serialize(searchResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in web search function for query: {Query}", query);
            return $"Search error: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetch and process content from a specific webpage URL
    /// </summary>
    /// <param name="url">The URL to fetch content from</param>
    /// <param name="maxLength">Maximum length of content to return (default: 10000)</param>
    /// <returns>JSON string containing fetched content</returns>
    [KernelFunction("FetchContent")]
    [Description("Fetch and process content from a specific webpage URL")]
    public async Task<string> FetchContentAsync(
        [Description("The URL to fetch content from")] string url,
        [Description("Maximum length of content to return (default: 10000)")] int maxLength = 10000)
    {
        try
        {
            _logger.LogInformation("Fetching content from URL: {Url}", url);
            
            var content = await _webContentFetcher.FetchContentAsync(url, maxLength);
            
            if (string.IsNullOrEmpty(content))
            {
                return $"Failed to fetch content from URL: {url}";
            }

            var response = new
            {
                url = url,
                content = content,
                content_length = content.Length,
                timestamp = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content from URL: {Url}", url);
            return $"Error fetching content: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetch and process content from multiple webpage URLs concurrently
    /// </summary>
    /// <param name="urls">Array of URLs to fetch content from</param>
    /// <param name="maxLength">Maximum length of content per URL (default: 10000)</param>
    /// <returns>JSON string containing fetched content from all URLs</returns>
    [KernelFunction("FetchMultipleContent")]
    [Description("Fetch and process content from multiple webpage URLs concurrently")]
    public async Task<string> FetchMultipleContentAsync(
        [Description("Array of URLs to fetch content from")] string[] urls,
        [Description("Maximum length of content per URL (default: 10000)")] int maxLength = 10000)
    {
        try
        {
            _logger.LogInformation("Fetching content from {Count} URLs", urls.Length);
            
            var contentResults = await _webContentFetcher.FetchMultipleContentAsync(urls, maxLength);
            
            var results = contentResults.Select(kvp => new
            {
                url = kvp.Key,
                content = kvp.Value,
                content_length = kvp.Value?.Length ?? 0,
                success = kvp.Value != null,
                timestamp = DateTime.UtcNow
            }).ToList();

            var response = new
            {
                total_urls = urls.Length,
                successful_fetches = results.Count(r => r.success),
                results = results
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content from multiple URLs");
            return $"Error fetching content: {ex.Message}";
        }
    }
}