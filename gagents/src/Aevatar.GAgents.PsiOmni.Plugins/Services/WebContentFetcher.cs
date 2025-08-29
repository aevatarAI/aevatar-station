using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Service for fetching and processing web page content
/// Matches OpenManus WebContentFetcher implementation
/// </summary>
public class WebContentFetcher : IWebContentFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebContentFetcher> _logger;

    // Matches OpenManus unwanted tags exactly
    private static readonly string[] UnwantedTags = { "script", "style", "header", "footer", "nav" };
    
    public WebContentFetcher(HttpClient httpClient, ILogger<WebContentFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Exact user agent from OpenManus implementation
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// Fetch content from URL - matches OpenManus fetch_content function exactly
    /// </summary>
    public async Task<string?> FetchContentAsync(
        string url, 
        int maxLength = 10000, 
        int timeout = 10, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            var response = await _httpClient.GetAsync(url, cts.Token);
            
            // Check status code like OpenManus
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(cts.Token);
            return ProcessHtmlContent(html, maxLength);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error fetching content from {Url}: {Error}", url, ex.Message);
            return null;
        }
    }

    public async Task<Dictionary<string, string?>> FetchMultipleContentAsync(
        IEnumerable<string> urls,
        int maxLength = 10000,
        int timeout = 10,
        CancellationToken cancellationToken = default)
    {
        var tasks = urls.Select(async url => new
        {
            Url = url,
            Content = await FetchContentAsync(url, maxLength, timeout, cancellationToken)
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Url, r => r.Content);
    }

    /// <summary>
    /// Process HTML content exactly like OpenManus implementation
    /// </summary>
    private string? ProcessHtmlContent(string html, int maxLength)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove unwanted elements exactly like OpenManus: script, style, header, footer, nav
            foreach (var tagName in UnwantedTags)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{tagName}");
                if (nodes != null)
                {
                    foreach (var node in nodes.ToList())
                    {
                        node.Remove();
                    }
                }
            }

            // Get text exactly like BeautifulSoup's get_text() with separator="\n", strip=True
            var text = doc.DocumentNode.InnerText;
            
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Clean whitespace like OpenManus: " ".join(text.split())
            text = string.Join(" ", text.Split(new char[0], StringSplitOptions.RemoveEmptyEntries));

            // Truncate to maxLength exactly like OpenManus: text[:10000]
            return text.Length > maxLength ? text[..maxLength] : text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing HTML content");
            return null;
        }
    }

}