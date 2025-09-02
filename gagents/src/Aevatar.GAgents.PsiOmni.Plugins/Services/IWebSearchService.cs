using Aevatar.GAgents.PsiOmni.Plugins.Models;
using Microsoft.SemanticKernel.Plugins.Web.Google;

namespace Aevatar.GAgents.PsiOmni.Plugins.Services;

/// <summary>
/// Interface for web search services
/// Matches OpenManus WebSearch interface
/// </summary>
public interface IWebSearchService
{
    /// <summary>
    /// Performs a web search with the specified query and parameters
    /// Matches OpenManus execute method signature
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="numResults">Number of results to return (default: 5)</param>
    /// <param name="lang">Language code (default: "en")</param>
    /// <param name="country">Country code (default: "us")</param>
    /// <param name="fetchContent">Whether to fetch full content for results (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SearchResponse with results and metadata</returns>
    Task<SearchResponse> ExecuteAsync(
        string query,
        int numResults = 5,
        string? lang = null,
        string? country = null,
        bool fetchContent = false,
        CancellationToken cancellationToken = default);
}