using Aevatar.Enum;

namespace Aevatar.WebHook.Deploy;

public interface IHostDeployManager
{
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyWebHookAsync(string appId, string version);
    Task RestartWebHookAsync(string appId,string version);

    // Methods that KubernetesHostManager actually implements
    Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    Task DestroyApplicationAsync(string appId, string version);
    Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    Task UpdateDeploymentImageAsync(string appId, string version, string newImage);
    Task RestartHostAsync(string appId, string version);
    /// <summary>
    /// Updates existing K8s ConfigMaps with the latest business configuration for specific host type
    /// </summary>
    /// <param name="hostId">Host identifier</param>
    /// <param name="version">Host version</param>
    /// <param name="hostType">Host type to update</param>
    Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType);
    
}