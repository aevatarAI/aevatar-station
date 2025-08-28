using System.Threading.Tasks;

namespace Aevatar.Service;

/// <summary>
/// Service interface for managing documentation link status
/// Handles validation and status tracking of documentation URLs
/// </summary>
public interface IDocumentLinkService
{
    Task<bool> GetDocumentLinkStatusAsync(string documentLink);
}
