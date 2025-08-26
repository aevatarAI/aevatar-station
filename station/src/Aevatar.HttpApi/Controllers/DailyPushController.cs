using System;
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
            
            await _dailyPushService.MarkPushAsReadAsync(userId, request.PushToken);
            
            _logger.LogInformation("Push marked as read for user {UserId} with token {TokenPrefix}...", 
                userId, request.PushToken[..Math.Min(8, request.PushToken.Length)]);
            
            // Follow GodGPT pattern: simple success result
            return Ok(new { result = true });
        }
        catch (Exception ex)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "Failed to mark push as read for token {TokenPrefix}...", 
                request.PushToken[..Math.Min(8, request.PushToken.Length)]);
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
                var language = HttpContext.GetGodGPTLanguage();
                var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.DeviceNotFound, language);
                // Follow GodGPT pattern: Ok with error for business logic failure
                return Ok(new
                {
                    error = new { code = 0, message = localizedMessage },
                    result = false
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
    [HttpPost("test/start")]
    public async Task<IActionResult> StartTestMode([FromQuery] string timezone = "Asia/Shanghai")
    {
        try
        {
            await _dailyPushService.StartTestModeAsync(timezone);
            
            // Follow GodGPT pattern: direct return with result data
            return Ok(new {
                result = true,
                message = $"Test mode started for timezone {timezone}",
                timezone = timezone,
                interval = "10 minutes",
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
}
