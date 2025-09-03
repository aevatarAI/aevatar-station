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
/// Daily push notification TEST API controller
/// For debugging and testing JWT lifecycle fixes
/// </summary>
[ApiController]
[Route("api/push/test")]
[Authorize] // Keep authorization for security
public class DailyPushTestController : AbpControllerBase
{
    private readonly IDailyPushService _dailyPushService;
    private readonly ILogger<DailyPushTestController> _logger;
    private readonly ILocalizationService _localizationService;

    public DailyPushTestController(
        IDailyPushService dailyPushService,
        ILogger<DailyPushTestController> logger,
        ILocalizationService localizationService)
    {
        _dailyPushService = dailyPushService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Send test push notification to all devices in specified timezone
    /// Bypasses all business logic restrictions for testing purposes
    /// Used primarily for testing RSA lifecycle fixes and Firebase token generation
    /// </summary>
    /// <param name="timeZoneId">Target timezone (e.g., "Asia/Shanghai")</param>
    /// <param name="request">Test push request with title and content</param>
    /// <returns>Detailed test results with statistics</returns>
    [HttpPost("{timeZoneId}")]
    public async Task<IActionResult> SendTestPushAsync(string timeZoneId, [FromBody] TestPushRequest request)
    {
        try
        {
            _logger.LogInformation("Test push requested for timezone {TimeZone} with title: '{Title}'", timeZoneId, request.Title);

            var result = await _dailyPushService.SendTestPushToTimezoneAsync(timeZoneId, request.Title, request.Content);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Test push completed for timezone {TimeZone}: {TotalUsers} users, {SuccessfulPushes} successful pushes, {FailedPushes} failures",
                    timeZoneId, result.TotalUsers, result.SuccessfulPushes, result.FailedPushes);

                return Ok(new
                {
                    result = true,
                    data = result
                });
            }
            else
            {
                return BadRequest(new
                {
                    result = false,
                    error = result.ErrorMessage,
                    data = result
                });
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("timezone"))
        {
            var language = HttpContext.GetGodGPTLanguage();
            var localizedMessage =
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidTimezone, language);
            _logger.LogWarning(ex, "Invalid timezone for test push: {TimeZone}", timeZoneId);
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
            _logger.LogError(ex, "Test push failed for timezone {TimeZone}: {ErrorMessage}", timeZoneId, ex.Message);
            return StatusCode(500, new { error = localizedMessage });
        }
    }

    /// <summary>
    /// Get available timezones for testing
    /// Helper endpoint to see which timezones have active users
    /// </summary>
    [HttpGet("timezones")]
    public IActionResult GetAvailableTimezones()
    {
        // Common timezones for testing
        var commonTimezones = new[]
        {
            "Asia/Shanghai",
            "Asia/Tokyo", 
            "America/New_York",
            "America/Los_Angeles",
            "Europe/London",
            "Europe/Paris",
            "Australia/Sydney",
            "UTC"
        };

        return Ok(new
        {
            result = true,
            data = new
            {
                timezones = commonTimezones,
                note = "These are common timezones. Use any valid IANA timezone identifier."
            }
        });
    }
}
