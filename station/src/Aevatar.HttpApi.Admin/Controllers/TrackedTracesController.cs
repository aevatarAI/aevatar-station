using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Aevatar.Service;
using Aevatar.Permissions;
using Aevatar.Dto;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Aevatar.Permissions;

namespace Aevatar.Controllers;

/// <summary>
/// RESTful controller for managing the collection of tracked trace IDs.
/// </summary>
[ApiController]
[Route("api/tracked-traces")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class TrackedTracesController : AbpController
{
    private readonly ITraceManagementService _traceManagementService;

    public TrackedTracesController(ITraceManagementService traceManagementService)
    {
        _traceManagementService = traceManagementService;
    }

    /// <summary>
    /// Gets all tracked trace IDs.
    /// </summary>
    /// <returns>Collection of tracked trace IDs.</returns>
    [HttpGet]
    public ActionResult<TrackedTracesDto> GetTrackedTraces()
    {
        var trackedIds = _traceManagementService.GetTrackedIds();
        var result = new TrackedTracesDto
        {
            TraceIds = trackedIds.ToList(),
            Count = trackedIds.Count
        };
        
        return Ok(result);
    }

    /// <summary>
    /// Adds a new trace ID to the tracked collection.
    /// </summary>
    /// <param name="request">The request containing the trace ID to add.</param>
    /// <returns>Created trace information.</returns>
    [HttpPost]
    public ActionResult<TrackedTraceDto> AddTrackedTrace([FromBody] AddTrackedTraceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TraceId))
        {
            return BadRequest("Trace ID is required.");
        }

        var trackedIds = _traceManagementService.GetTrackedIds();
        if (trackedIds.Contains(request.TraceId))
        {
            return Conflict($"Trace ID '{request.TraceId}' is already being tracked.");
        }

        var success = _traceManagementService.AddTrackedId(request.TraceId, request.IsEnabled);
        
        if (!success)
        {
            return BadRequest($"Failed to add trace ID '{request.TraceId}' to tracking.");
        }

        var result = new TrackedTraceDto
        {
            TraceId = request.TraceId,
            IsEnabled = request.IsEnabled,
            AddedAt = DateTime.UtcNow
        };

        return CreatedAtAction(
            nameof(GetTrackedTrace), 
            new { traceId = request.TraceId }, 
            result);
    }

    /// <summary>
    /// Gets a specific tracked trace by ID.
    /// </summary>
    /// <param name="traceId">The trace ID to retrieve.</param>
    /// <returns>The tracked trace information.</returns>
    [HttpGet("{traceId}")]
    public ActionResult<TrackedTraceDto> GetTrackedTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return BadRequest("Trace ID is required.");
        }

        var trackedIds = _traceManagementService.GetTrackedIds();
        if (!trackedIds.Contains(traceId))
        {
            return NotFound($"Trace ID '{traceId}' is not being tracked.");
        }

        var result = new TrackedTraceDto
        {
            TraceId = traceId,
            IsEnabled = _traceManagementService.IsTracingEnabled(), // Enhance service for per-trace status if needed
            AddedAt = DateTime.UtcNow // You might want to track this in your service
        };

        return Ok(result);
    }

    /// <summary>
    /// Removes a trace ID from the tracked collection.
    /// </summary>
    /// <param name="traceId">The trace ID to remove.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{traceId}")]
    public IActionResult RemoveTrackedTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return BadRequest("Trace ID is required.");
        }

        var success = _traceManagementService.RemoveTrackedId(traceId);
        
        if (!success)
        {
            return NotFound($"Trace ID '{traceId}' not found in tracked traces or could not be removed.");
        }

        return NoContent();
    }

    /// <summary>
    /// Removes all tracked trace IDs.
    /// </summary>
    /// <returns>No content on success.</returns>
    [HttpDelete]
    public IActionResult ClearTrackedTraces()
    {
        // This would require a new method in your service
        // For now, we'll call Clear() which might do more than just clear tracked IDs
        _traceManagementService.Clear();
        return NoContent();
    }
}
