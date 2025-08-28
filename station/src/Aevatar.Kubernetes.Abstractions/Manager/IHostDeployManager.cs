using System.Threading.Tasks;

namespace Aevatar.Kubernetes.Abstractions.Manager;

public interface IHostDeployManager
{
    Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyApplicationAsync(string appId, string version);
    Task DestroyWebHookAsync(string appId, string version);
    Task UpdateDeploymentImageAsync(string appId, string version, string newImage);
    Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    Task RestartHostAsync(string appId, string version);
    Task RestartWebHookAsync(string appId, string version);
    Task CopyHostAsync(string sourceClientId, string newClientId, string version);
    Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType);
}