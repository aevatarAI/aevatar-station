using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Enum;
using Aevatar.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Host")]
[Route("api/host")]
[Authorize]
public class HostController
{
    private readonly ILogService _logService;
    private readonly KubernetesOptions _kubernetesOptions;

    public HostController(
        ILogService logService, 
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions
      )
    {
        _logService = logService;
        _kubernetesOptions = kubernetesOptions.Value;
    }
    
    [HttpGet("log")]
    public async Task<List<HostLogIndex>> GetLatestRealTimeLogs(string appId,HostTypeEnum hostType,int offset)
    {
        var indexName = _logService.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, appId + "-"+hostType.ToString().ToLower(), "1");
        return await _logService.GetHostLatestLogAsync(indexName, offset);
    }
   
    
    
}