using System.Threading.Tasks;

namespace Aevatar.Kubernetes.Manager;

/// <summary>
/// Interface for host copy operations
/// Responsible for copying host environments and their resources
/// </summary>
public interface IHostCopyManager
{
    /// <summary>
    /// Copy a host environment from source to target
    /// </summary>
    /// <param name="sourceClientId">Source client ID to copy from</param>
    /// <param name="newClientId">Target client ID to copy to</param>
    /// <param name="version">Version of the host environment</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CopyHostAsync(string sourceClientId, string newClientId, string version);
} 