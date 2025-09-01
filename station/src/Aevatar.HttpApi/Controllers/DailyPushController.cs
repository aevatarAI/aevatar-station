using System;
using System.Threading.Tasks;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Application.Contracts.Services;
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
            return Ok(new
            {
                result = true,
                isNewRegistration = isNewRegistration
            });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("timezone"))
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidTimezone, language);
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
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidRequest, language);
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
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
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
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
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
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "Failed to get device status for device {DeviceId}", deviceId);
            return StatusCode(500, new { error = localizedMessage });
        }
    }
}