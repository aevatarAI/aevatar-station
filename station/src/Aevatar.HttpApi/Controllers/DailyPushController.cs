using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Application.Contracts.Services;
using Aevatar.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
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
    private readonly ILocalizationService _localizationService;

    public DailyPushController(
        IDailyPushService dailyPushService, 
        ILogger<DailyPushController> logger,
        ILocalizationService localizationService)
    {
        _dailyPushService = dailyPushService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Register or update device for daily push notifications
    /// </summary>
    [HttpPost("device")]
    public async Task<IActionResult> RegisterDeviceAsync([FromBody] DeviceRequest request)
    {
        try
        {
            var userId = (Guid)CurrentUser.Id!;
            var language = HttpContext.GetGodGPTLanguage();
            
            // Always use language from HTTP header, following system convention
            var isNewRegistration = await _dailyPushService.RegisterOrUpdateDeviceAsync(userId, request, language);
            
            _logger.LogInformation("Device {DeviceId} registered/updated for user {UserId}", 
                request.DeviceId, userId);
            
            // Follow GodGPT pattern: direct return with result data
            return Ok(new { 
                result = true,
                isNewRegistration = isNewRegistration
            });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("timezone"))
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidTimezone, language);
            _logger.LogWarning(ex, "Invalid timezone for device registration: {DeviceId}", request.DeviceId);
            return BadRequest(new
            {
                error = new { code = 1, message = localizedMessage },
                result = false
            });
        }
        catch (ArgumentException ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidRequest, language);
            _logger.LogWarning(ex, "Invalid request for device registration: {DeviceId}", request.DeviceId);
            return BadRequest(new
            {
                error = new { code = 1, message = localizedMessage },
                result = false
            });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "Failed to register/update device {DeviceId}", request.DeviceId);
            return StatusCode(500, new { error = localizedMessage });
        }
    }
    
    /// <summary>
    /// Mark daily push as read (platform independent)
    /// Called when user clicks push notification
    /// </summary>
    [HttpPost("read")]
    public async Task<IActionResult> MarkAsReadAsync([FromBody] MarkReadRequest request)
    {
        try
        {
            var userId = (Guid)CurrentUser.Id!;
            
            await _dailyPushService.MarkPushAsReadAsync(userId, request.DeviceId);
            
            _logger.LogInformation("Push marked as read for user {UserId} with device {DeviceId}", 
                userId, request.DeviceId);
            
            // Follow GodGPT pattern: simple success result
            return Ok(new { result = true });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "Failed to mark push as read for device {DeviceId}", 
                request.DeviceId);
            return StatusCode(500, new { error = localizedMessage });
        }
    }
    
    /// <summary>
    /// Query device status (timezone, push settings)
    /// Used for debugging and settings verification
    /// </summary>
    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetDeviceStatusAsync(string deviceId)
    {
        try
        {
            var userId = (Guid)CurrentUser.Id!;
            
            var response = await _dailyPushService.GetDeviceStatusAsync(userId, deviceId);
            
            if (response == null)
            {
                // Return empty object with pushEnabled=true for non-existent devices
                return Ok(new
                {
                    result = true,
                    deviceId = deviceId,
                    timeZoneId = "",
                    pushEnabled = true,
                    pushLanguage = "",
                    pushToken = ""
                });
            }
            
            // Follow GodGPT pattern: direct return of data
            return Ok(response);
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "Failed to get device status for device {DeviceId}", deviceId);
            return StatusCode(500, new { error = localizedMessage });
        }
    }
    
    // Test mode APIs - TODO: Remove before production
    
    /// <summary>
    /// Start test mode for rapid push testing in specified timezone
    /// </summary>
    /// <param name="timezone">Target timezone (e.g., Asia/Shanghai)</param>
    /// <param name="intervalSeconds">Push interval in seconds (default: 600 = 10 minutes, min: 10, max: 3600)</param>
    [HttpPost("test/start")]
    public async Task<IActionResult> StartTestMode([FromQuery] string timezone = "Asia/Shanghai", [FromQuery] int intervalSeconds = 600)
    {
        try
        {
            // Validate interval range
            if (intervalSeconds < 10 || intervalSeconds > 3600)
            {
                var language = HttpContext.GetGodGPTLanguage();
                var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidRequest, language);
                return BadRequest(new {
                    error = new { code = 1, message = $"{localizedMessage}: intervalSeconds must be between 10 and 3600 seconds" },
                    result = false
                });
            }
            
            await _dailyPushService.StartTestModeAsync(timezone, intervalSeconds);
            
            // Follow GodGPT pattern: direct return with result data
            return Ok(new {
                result = true,
                message = $"Test mode started for timezone {timezone}",
                timezone = timezone,
                intervalSeconds = intervalSeconds,
                intervalDescription = $"{intervalSeconds} seconds ({intervalSeconds / 60.0:F1} minutes)",
                maxRounds = 6
            });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.TestModeStartFailed, language);
            _logger.LogError(ex, "Failed to start test mode for timezone {Timezone}", timezone);
            return BadRequest(new {
                error = new { code = 1, message = localizedMessage, details = ex.Message },
                result = false
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
                result = true,
                message = $"Test mode stopped for timezone {timezone}",
                timezone = timezone
            });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.TestModeStopFailed, language);
            _logger.LogError(ex, "Failed to stop test mode for timezone {Timezone}", timezone);
            return BadRequest(new {
                error = new { code = 1, message = localizedMessage, details = ex.Message },
                result = false
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
            // Ensure timezone is not empty (defensive programming)
            if (string.IsNullOrEmpty(timezone))
            {
                timezone = "Asia/Shanghai";
            }
            
            var status = await _dailyPushService.GetTestStatusAsync(timezone);
            
            // Follow GodGPT pattern: direct return of data object
            return Ok(new {
                timezone = timezone,
                isActive = status.IsActive,
                startTime = status.StartTime,
                roundsCompleted = status.RoundsCompleted,
                maxRounds = status.MaxRounds,
                nextRoundIn = status.IsActive ? "10 minutes from last execution" : "N/A"
            });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.TestModeStatusFailed, language);
            _logger.LogError(ex, "Failed to get test status for timezone {Timezone}", timezone);
            return BadRequest(new {
                error = new { code = 1, message = localizedMessage, details = ex.Message },
                result = false
            });
        }
    }
    
    /// <summary>
    /// Get all devices in specified timezone for debugging - TODO: Remove before production
    /// </summary>
    [HttpGet("test/devices")]
    public async Task<IActionResult> GetDevicesInTimezone([FromQuery] string timezone = "Asia/Shanghai")
    {
        try
        {
            // Defensive check for empty timezone
            if (string.IsNullOrEmpty(timezone))
            {
                timezone = "Asia/Shanghai";
            }
            
            var devices = await _dailyPushService.GetDevicesInTimezoneAsync(timezone);
            return Ok(new
            {
                result = new
                {
                    timezone = timezone,
                    totalUsers = devices.Select(d => d.UserId).Distinct().Count(),
                    totalDevices = devices.Count,
                    enabledDevices = devices.Count(d => d.PushEnabled),
                    devices = devices.Select(d => new
                    {
                        userId = d.UserId,
                        deviceId = d.DeviceId,
                        pushToken = d.PushToken, // Already truncated for privacy
                        timeZoneId = d.TimeZoneId,
                        pushLanguage = d.PushLanguage,
                        pushEnabled = d.PushEnabled,
                        hasEnabledDeviceInTimezone = d.HasEnabledDeviceInTimezone,
                        totalDeviceCount = d.TotalDeviceCount,
                        enabledDeviceCount = d.EnabledDeviceCount
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = "Failed to get devices in timezone"; // TODO: Add proper localization
            _logger.LogError(ex, "Failed to get devices in timezone {Timezone}", timezone);
            return BadRequest(new {
                error = new { code = 1, message = localizedMessage, details = ex.Message },
                result = false
            });
        }
    }
}
