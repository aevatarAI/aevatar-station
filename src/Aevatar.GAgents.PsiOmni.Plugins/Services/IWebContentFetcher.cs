namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Interface for fetching and processing web page content
/// </summary>
public interface IWebContentFetcher
{
    /// <summary>
    /// Fetches and processes content from a web page URL
    /// </summary>
    /// <param name="url">The URL to fetch content from</param>
    /// <param name="maxLength">Maximum length of content to return (default: 10000)</param>
    /// <param name="timeout">Request timeout in seconds (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed text content or null if failed</returns>
    Task<string?> FetchContentAsync(
        string url, 
        int maxLength = 10000, 
        int timeout = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches content from multiple URLs concurrently
    /// </summary>
    /// <param name="urls">Collection of URLs to fetch</param>
    /// <param name="maxLength">Maximum length of content per URL</param>
    /// <param name="timeout">Request timeout in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping URLs to their content (null if failed)</returns>
    Task<Dictionary<string, string?>> FetchMultipleContentAsync(
        IEnumerable<string> urls,
        int maxLength = 10000,
        int timeout = 10,
        CancellationToken cancellationToken = default);
}