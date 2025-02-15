using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.WebHook.Deploy;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


public interface IDeveloperService
{
    Task CreateHostAsync(string HostId, string version, List<string> corsUrls);
    Task DestroyHostAsync(string inputHostId, string inputVersion);
}

public class DeveloperService: ApplicationService, IDeveloperService
{
    private readonly IHostDeployManager _hostDeployManager;
    public DeveloperService(IHostDeployManager hostDeployManager
       )
    {
        _hostDeployManager = hostDeployManager;
    }

    public async Task CreateHostAsync(string HostId, string version, List<string> corsUrls)
    {
        await _hostDeployManager.CreateHostAsync(HostId, version,corsUrls);
    }

    public async Task DestroyHostAsync(string inputHostId, string inputVersion)
    {
        await _hostDeployManager.DestroyHostAsync(inputHostId, inputVersion);
    }
}


