using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("Users")]
[Route("api/users")]
public class UserController :  AevatarController

{
    private readonly IUserAppService _userAppService;
    private readonly IDeveloperService _developerService;
    public UserController(IUserAppService userAppService,IDeveloperService developerService)
    {
        _userAppService = userAppService;
        _developerService = developerService;
    }

    [HttpPost("registerClient")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RegisterClientAuthentication(string clientId, string clientSecret, string corsUrls)
    {
        await _userAppService.RegisterClientAuthentication(clientId, clientSecret);
        await _developerService.CreateHostAsync(clientId, "1",corsUrls);
    }
    
    [HttpPost("CreateHost")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public  async Task CreateHost(string clientId,string corsUrls)
    {
        await _developerService.CreateHostAsync(clientId, "1",corsUrls);
    }
    
    [HttpPost("destroyHost")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyHostAsync(string clientId)
    {
        await _developerService.DestroyHostAsync(clientId, "1");
    }
    
}