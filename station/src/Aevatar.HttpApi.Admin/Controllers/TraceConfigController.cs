
using Microsoft.AspNetCore.Mvc;
using Aevatar.Service;
using Aevatar.Dto;
using Volo.Abp.AspNetCore.Mvc;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Aevatar.Controllers;

/// <summary>
/// RESTful controller for managing global trace configuration.
/// </summary>
[ApiController]
[Route("api/trace-config")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class TraceConfigController : AbpController
{
    private readonly ITraceManagementService _traceManagementService;

    public TraceConfigController(ITraceManagementService traceManagementService)
    {
        _traceManagementService = traceManagementService;
    }

    /// <summary>
    /// Gets the current trace configuration.
    /// </summary>
    /// <returns>The current trace configuration.</returns>
    [HttpGet]
    public ActionResult<TraceConfigDto> GetConfiguration()
    {
        var config = _traceManagementService.GetCurrentConfiguration();
        if (config == null)
        {
            return NotFound();
        }

        var dto = new TraceConfigDto
        {
            IsEnabled = _traceManagementService.IsTracingEnabled(),
            TrackedIds = _traceManagementService.GetTrackedIds(),
            Configuration = config
        };

        return Ok(dto);
    }

    /// <summary>
    /// Updates the global trace configuration.
    /// </summary>
    /// <param name="request">The new configuration.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut]
    public ActionResult<TraceConfigDto> UpdateConfiguration([FromBody] UpdateTraceConfigRequest request)
    {
        // Implementation depends on your service capabilities
        // This is a placeholder for when you add global config update functionality
        return Ok();
    }

    /// <summary>
    /// Clears the trace configuration and context.
    /// </summary>
    /// <returns>No content on success.</returns>
    [HttpDelete]
    public IActionResult ClearConfiguration()
    {
        _traceManagementService.Clear();
        return NoContent();
    }

    /// <summary>
    /// Gets the global tracing enabled status.
    /// </summary>
    /// <returns>The tracing status.</returns>
    [HttpGet("status")]
    public ActionResult<TracingStatusDto> GetStatus()
    {
        var isEnabled = _traceManagementService.IsTracingEnabled();
        var trackedCount = _traceManagementService.GetTrackedIds().Count;

        return Ok(new TracingStatusDto
        {
            IsEnabled = isEnabled,
            TrackedTraceCount = trackedCount
        });
    }
}
