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
        
        // Always use language from HTTP header, following system convention
        var isNewRegistration = await _dailyPushService.RegisterOrUpdateDeviceAsync(userId, request, language);
        
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
    
    // Test mode APIs - TODO: Remove before production
    
    /// <summary>
    /// Start test mode for rapid push testing in specified timezone
    /// </summary>
    [HttpPost("test/start")]
    public async Task<IActionResult> StartTestMode([FromQuery] string timezone = "Asia/Shanghai")
    {
        try
        {
            await _dailyPushService.StartTestModeAsync(timezone);
            
            return Ok(new {
                success = true,
                message = $"Test mode started for timezone {timezone}",
                timezone = timezone,
                interval = "10 minutes",
                maxRounds = 6
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start test mode for timezone {Timezone}", timezone);
            return BadRequest(new {
                success = false,
                error = "Failed to start test mode",
                details = ex.Message
            });
        }
    }
    
    /// <summary>
    /// Stop test mode and cleanup test reminders for specified timezone
    /// </summary>
    [HttpPost("test/stop")]
    public async Task<IActionResult> StopTestMode([FromQuery] string timezone = "Asia/Shanghai")
    {
        try
        {
            await _dailyPushService.StopTestModeAsync(timezone);
            
            return Ok(new {
                success = true,
                message = $"Test mode stopped for timezone {timezone}",
                timezone = timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop test mode for timezone {Timezone}", timezone);
            return BadRequest(new {
                success = false,
                error = "Failed to stop test mode",
                details = ex.Message
            });
        }
    }
    
    /// <summary>
    /// Get test mode status for specified timezone
    /// </summary>
    [HttpGet("test/status")]
    public async Task<IActionResult> GetTestStatus([FromQuery] string timezone = "Asia/Shanghai")
    {
        try
        {
            var status = await _dailyPushService.GetTestStatusAsync(timezone);
            
            return Ok(new {
                success = true,
                data = new {
                    timezone = timezone,
                    isActive = status.IsActive,
                    startTime = status.StartTime,
                    roundsCompleted = status.RoundsCompleted,
                    maxRounds = status.MaxRounds,
                    nextRoundIn = status.IsActive ? "10 minutes from last execution" : "N/A"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get test status for timezone {Timezone}", timezone);
            return BadRequest(new {
                success = false,
                error = "Failed to get test status",
                details = ex.Message
            });
        }
    }
}
