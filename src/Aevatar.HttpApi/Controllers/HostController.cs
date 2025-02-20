using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Kubernetes.Enum;
using Aevatar.Options;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly IDeveloperService _developerService;
    private readonly HostOptions _hostOptions;
    private readonly ILogger<HostController> _logger;

    public HostController(
        ILogService logService, 
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IOptionsSnapshot<HostOptions> hostOptions,
        IDeveloperService developerService,
        ILogger<HostController> logger)
    {
        _logService = logService;
        _kubernetesOptions = kubernetesOptions.Value;
        _hostOptions = hostOptions.Value;
        _developerService = developerService;
        _logger = logger;
    }
    
    [HttpGet("log")]
    public async Task<List<HostLogIndex>> GetLatestRealTimeLogs(string appId,HostTypeEnum hostType,int offset)
    {
        var indexName = _logService.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, appId + "-"+hostType.ToString().ToLower(), "1");
        return await _logService.GetHostLatestLogAsync(indexName, offset);
    }
    
    [HttpPost("updateDockerImage")]
    public async Task UpdateDockerImageAsync(HostTypeEnum hostType,string imageName)
    {
        if (_hostOptions.HostId.IsNullOrEmpty())
        {
            _logger.LogWarning("updateDockerImage HostId isEmpty ");
        }

        await _developerService.UpdateDockerImageAsync(_hostOptions.HostId + "-" + hostType, _hostOptions.Version,
            _hostOptions.DockerImagePrefix + imageName);
    }
}