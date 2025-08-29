using Aevatar.GAgents.PsiOmni.Plugins.Models;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Interface for individual search engines, matching OpenManus WebSearchEngine
/// </summary>
public interface ISearchEngine
{
    /// <summary>
    /// Name of the search engine
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Perform search with the specified parameters
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="numResults">Number of results to return</param>
    /// <param name="lang">Language code</param>
    /// <param name="country">Country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of search results</returns>
    Task<List<SearchResult>> PerformSearchAsync(
        string query,
        int numResults,
        string? lang,
        string? country,
        CancellationToken cancellationToken = default);
}