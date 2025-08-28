using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

/// <summary>
/// Mock implementation of documentation link service
/// This is a temporary implementation - other developers should replace with actual implementation
/// Currently always returns true for all URLs
/// </summary>
public class DocumentLinkService : IDocumentLinkService, ISingletonDependency
{
    /// <summary>
    /// Mock implementation that always returns true
    /// TODO: Other developers should implement actual logic to check documentation link status
    /// </summary>
    /// <param name="documentLink">The documentation URL to check</param>
    /// <returns>Always returns true in this mock implementation</returns>
    public async Task<bool> GetDocumentLinkStatusAsync(string documentLink)
    {
        // Mock implementation: always return true
        // TODO: Replace with actual implementation by other developers
        return true;
    }
}