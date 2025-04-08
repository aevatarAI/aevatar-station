using System.Threading.Tasks;
using Aevatar.AppleAuth;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("AppleAuth")]
[Microsoft.AspNetCore.Components.Route("api/apple")]
public class AppleAuthController : AevatarController
{
    private readonly IAppleAuthService _appleAuthService;

    public AppleAuthController(IAppleAuthService appleAuthService)
    {
        _appleAuthService = appleAuthService;
    }

    [HttpPost("callback")]
    public async Task<IActionResult> CallbackAsync([FromForm] AppleAuthCallbackDto appleAuthCallbackDto)
    {
        var redirectUrl = await _appleAuthService.CallbackAsync(appleAuthCallbackDto);
        return Redirect(redirectUrl);
    }
}