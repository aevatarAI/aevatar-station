using System.Threading.Tasks;
using Aevatar.AppleAuth;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("AppleAuth")]
[Route("api/apple")]
public class AppleAuthController : AevatarController
{
    private readonly IAppleAuthService _appleAuthService;

    public AppleAuthController(IAppleAuthService appleAuthService)
    {
        _appleAuthService = appleAuthService;
    }

    [HttpPost("{platform}/callback")]
    public async Task<IActionResult> CallbackAsync(string platform, [FromForm] AppleAuthCallbackDto appleAuthCallbackDto)
    {
        
        var redirectUrl = await _appleAuthService.CallbackAsync(platform, appleAuthCallbackDto);
        return Redirect(redirectUrl);
    }
}