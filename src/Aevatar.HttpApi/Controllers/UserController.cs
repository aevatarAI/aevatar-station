using System.Threading.Tasks;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Users")]
[Route("api/users")]
public class UserController :  AevatarController

{
    private readonly IUserAppService _userAppService;
    public UserController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }
    
    [HttpPost("registerClient")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task RegisterClientAuthentication(string clientId,string clientSecret)
    {
        await _userAppService.RegisterClientAuthentication(clientId, clientSecret);
    }
    
}