using System;
using System.Threading.Tasks;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


public interface IDaippService
{
    Task CreateDaippAsync(string daippId, string version);
    Task DestroyDaippAsync(string inputDaippId, string inputVersion);
}

public class DaippService: ApplicationService, IDaippService
{
    private readonly IClusterClient _clusterClient;
    private readonly IWebhookDeployManager _DaippDeployManager;
    private readonly DaippDeployOptions _DaippDeployOptions;
    public DaippService(IClusterClient clusterClient,IWebhookDeployManager DaippDeployManager,
        IOptions<DaippDeployOptions> DaippDeployOptions)
    {
        _clusterClient = clusterClient;
        _DaippDeployManager = DaippDeployManager;
        _DaippDeployOptions = DaippDeployOptions.Value;
    }

    public async Task CreateDaippAsync(string daippId, string version)
    {
        await _DaippDeployManager.CreateNewDaippAsync(daippId, version);
    }

    public async Task DestroyDaippAsync(string inputDaippId, string inputVersion)
    {
        await _DaippDeployManager.DestroyDaippAsync(inputDaippId, inputVersion);
    }
}


