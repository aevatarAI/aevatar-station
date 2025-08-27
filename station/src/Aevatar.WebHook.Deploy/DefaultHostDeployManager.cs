using Aevatar.Enum;

namespace Aevatar.WebHook.Deploy;

public class DefaultHostDeployManager : IHostDeployManager
{
    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return string.Empty;
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        return;
    }

    public async Task RestartWebHookAsync(string appId, string version)
    {
        return;
    }

    public async Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
    {
        return;
    }

    public async Task DestroyApplicationAsync(string appId, string version)
    {
        return;
    }

    public async Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
    {
        return;
    }

    public async Task UpdateDeploymentImageAsync(string appId, string version, string newImage)
    {
        return;
    }

    public async Task RestartHostAsync(string appId, string version)
    {
        return;
    }

    public async Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType)
    {
        // Default implementation does nothing - override in concrete implementations like KubernetesHostManager
        return;
    }
}