using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.WebHook.Deploy;
using Aevatar.Kubernetes.Manager;
using Aevatar.Enum;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


public interface IDeveloperService
{
    Task CreateHostAsync(string HostId, string version, string corsUrls);
    Task DestroyHostAsync(string inputHostId, string inputVersion);

    Task UpdateDockerImageAsync(string appId, string version, string newImage);
    
    Task CopyHostAsync(string sourceClientId, string newClientId, string version, string corsUrls);
    
    /// <summary>
    /// Updates business configuration in existing K8s ConfigMaps for specific host type
    /// </summary>
    Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType);
}

public class DeveloperService: ApplicationService, IDeveloperService
{
    private readonly IHostDeployManager _hostDeployManager;
    private readonly IHostCopyManager _hostCopyManager;
    
    public DeveloperService(
        IHostDeployManager hostDeployManager,
        IHostCopyManager hostCopyManager)
    {
        _hostDeployManager = hostDeployManager;
        _hostCopyManager = hostCopyManager;
    }

    public async Task CreateHostAsync(string HostId, string version, string corsUrls)
    {
        await _hostDeployManager.CreateHostAsync(HostId, version,corsUrls);
    }

    public async Task DestroyHostAsync(string inputHostId, string inputVersion)
    {
        await _hostDeployManager.DestroyHostAsync(inputHostId, inputVersion);
    }

    public async Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        await _hostDeployManager.UpdateDockerImageAsync(appId, version,newImage);
    }

    public async Task CopyHostAsync(string sourceClientId, string newClientId, string version, string corsUrls)
    {
        await _hostCopyManager.CopyHostAsync(sourceClientId, newClientId, version, corsUrls);
    }

    public async Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType)
    {
        await _hostDeployManager.UpdateBusinessConfigurationAsync(hostId, version, hostType);
    }
}


