using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.HttpApi.Controllers;

/// <summary>
/// Daily push notification API controller
/// </summary>
[ApiController]
[Route("api/push")]
[Authorize]
public class DailyPushController : AbpControllerBase
{
    private readonly IDailyPushService _dailyPushService;
    private readonly ILogger<DailyPushController> _logger;

    public DailyPushController(IDailyPushService dailyPushService, ILogger<DailyPushController> logger)
    {
        _dailyPushService = dailyPushService;
        _logger = logger;
    }

    /// <summary>
    /// Register or update device for daily push notifications
    /// </summary>
    [HttpPost("device")]
    public async Task<IActionResult> RegisterDeviceAsync([FromBody] DeviceRequest request)
    {
        var userId = (Guid)CurrentUser.Id!;
        var language = HttpContext.GetGodGPTLanguage();
        
        // Use language from HTTP context if request doesn't specify one
        if (string.IsNullOrEmpty(request.PushLanguage))
        {
            request.PushLanguage = language.ToString();
        }
        
        var isNewRegistration = await _dailyPushService.RegisterOrUpdateDeviceAsync(userId, request);
        
        _logger.LogInformation("Device {DeviceId} registered/updated for user {UserId}", 
            request.DeviceId, userId);
        
        return Ok(new { 
            success = true, 
            isNewRegistration = isNewRegistration
        });
    }
    
    /// <summary>
    /// Mark daily push as read (platform independent)
    /// Called when user clicks push notification
    /// </summary>
    [HttpPost("read")]
    public async Task<IActionResult> MarkAsReadAsync([FromBody] MarkReadRequest request)
    {
        var userId = (Guid)CurrentUser.Id!;
        
        await _dailyPushService.MarkPushAsReadAsync(userId, request.PushToken);
        
        _logger.LogInformation("Push marked as read for user {UserId} with token {TokenPrefix}...", 
            userId, request.PushToken[..Math.Min(8, request.PushToken.Length)]);
        
        return Ok(new { success = true });
    }
    
    /// <summary>
    /// Query device status (timezone, push settings)
    /// Used for debugging and settings verification
    /// </summary>
    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetDeviceStatusAsync(string deviceId)
    {
        var userId = (Guid)CurrentUser.Id!;
        
        var response = await _dailyPushService.GetDeviceStatusAsync(userId, deviceId);
        
        if (response == null)
        {
            return NotFound(new { 
                success = false, 
                error = "Device not found",
                code = "DEVICE_NOT_FOUND"
            });
        }
        
        return Ok(new { 
            success = true, 
            data = response
        });
    }
}
