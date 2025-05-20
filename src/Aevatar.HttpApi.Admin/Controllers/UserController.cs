using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Kubernetes.Enum;
using Aevatar.Permissions;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("Users")]
[Route("api/users")]
public class UserController :  AevatarController

{
    private readonly IUserAppService _userAppService;
    private readonly IDeveloperService _developerService;
    private readonly ILogger<UserController> _logger;
    public UserController(IUserAppService userAppService,
        IDeveloperService developerService,
        ILogger<UserController> logger)
    {
        _userAppService = userAppService;
        _developerService = developerService;
        _logger = logger;
    }
    
    [HttpPost("registerClient")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task RegisterClientAuthentication(string clientId, string clientSecret, string corsUrls)
    {
        await _userAppService.RegisterClientAuthentication(clientId, clientSecret);
        await _developerService.CreateHostAsync(clientId, "1",corsUrls);
    }
    
    [HttpPost("grantClientPermissions")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task GrantClientPermissionsAsync(string clientId)
    {
        await _userAppService.GrantClientPermissionsAsync(clientId);
    }
    
    [HttpPost("CreateHost")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public  async Task CreateHost(string clientId,string corsUrls)
    {
        await _developerService.CreateHostAsync(clientId, "1",corsUrls);
    }
    
    [HttpPost("destroyHost")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task DestroyHostAsync(string clientId)
    {
        await _developerService.DestroyHostAsync(clientId, "1");
    }
    
    [Authorize]
    [HttpPost("updateDockerImage")]
    public async Task UpdateDockerImageAsync(HostTypeEnum hostType,string imageName)
    {
        var clientId =  CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;
        if (! clientId.IsNullOrEmpty() && clientId.Contains("Aevatar"))
        {
            _logger.LogWarning($"UpdateDockerImageAsync unSupport client {clientId} ");
            throw new UserFriendlyException("unSupport client");
        }
        await _developerService.UpdateDockerImageAsync(clientId + "-" + hostType, "1",
            imageName);
    }
    
    [Authorize(Policy = "OnlyAdminAccess")]
    [HttpPost("updateDockerImageByAdmin")]
    public async Task UpdateDockerImageByAdminAsync(string hostId,HostTypeEnum hostType,string imageName)
    {
        await _developerService.UpdateDockerImageAsync(hostId + "-" + hostType, "1",
            imageName);
    }
}