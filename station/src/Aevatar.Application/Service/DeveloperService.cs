using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.WebHook.Deploy;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


public interface IDeveloperService
{
    Task CreateHostAsync(string HostId, string version, string corsUrls);
    Task DestroyHostAsync(string inputHostId, string inputVersion);

    Task UpdateDockerImageAsync(string appId, string version, string newImage);
}

public class DeveloperService: ApplicationService, IDeveloperService
{
    private readonly IHostDeployManager _hostDeployManager;
    public DeveloperService(IHostDeployManager hostDeployManager
       )
    {
        _hostDeployManager = hostDeployManager;
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

}


