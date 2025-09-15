using System.Threading.Tasks;

namespace Aevatar.Service;

public interface IDocumentLinkService
{
    Task RefreshDocumentLinkStatusAsync();
    Task<bool> GetDocumentLinkStatusAsync(string documentLink);
}