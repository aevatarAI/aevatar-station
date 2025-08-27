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
/// RESTful controller for managing individual trace resources.
/// </summary>
[ApiController]
[Route("api/traces")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class TracesController : AbpController
{
    private readonly ITraceManagementService _traceManagementService;

    public TracesController(ITraceManagementService traceManagementService)
    {
        _traceManagementService = traceManagementService;
    }

    /// <summary>
    /// Gets all traces with their status.
    /// </summary>
    /// <returns>List of all traces and their enabled status.</returns>
    [HttpGet]
    public ActionResult<List<TraceDto>> GetTraces()
    {
        var trackedIds = _traceManagementService.GetTrackedIds();
        var traces = trackedIds.Select(id => new TraceDto
        {
            TraceId = id,
            IsEnabled = _traceManagementService.IsTracingEnabled() // You might need to enhance service to check per-trace status
        }).ToList();
        
        return Ok(traces);
    }

    /// <summary>
    /// Gets a specific trace by ID.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <returns>The trace information.</returns>
    [HttpGet("{traceId}")]
    public ActionResult<TraceDto> GetTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return BadRequest("Trace ID is required.");
        }

        var trackedIds = _traceManagementService.GetTrackedIds();
        if (!trackedIds.Contains(traceId))
        {
            return NotFound($"Trace ID '{traceId}' not found in tracked traces.");
        }

        var trace = new TraceDto
        {
            TraceId = traceId,
            IsEnabled = _traceManagementService.IsTracingEnabled() // Enhance service for per-trace status if needed
        };

        return Ok(trace);
    }

    /// <summary>
    /// Updates a trace's enabled status.
    /// </summary>
    /// <param name="traceId">The trace ID to update.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated trace information.</returns>
    [HttpPut("{traceId}")]
    public ActionResult<TraceDto> UpdateTrace(string traceId, [FromBody] UpdateTraceRequest request)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return BadRequest("Trace ID is required.");
        }

        bool success;
        if (request.IsEnabled)
        {
            success = _traceManagementService.EnableTracing(traceId, request.IsEnabled);
        }
        else
        {
            success = _traceManagementService.DisableTracing(traceId);
        }

        if (!success)
        {
            return BadRequest($"Failed to update trace '{traceId}'.");
        }

        var updatedTrace = new TraceDto
        {
            TraceId = traceId,
            IsEnabled = request.IsEnabled
        };

        return Ok(updatedTrace);
    }

    /// <summary>
    /// Removes a trace from tracking.
    /// </summary>
    /// <param name="traceId">The trace ID to remove.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{traceId}")]
    public IActionResult DeleteTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return BadRequest("Trace ID is required.");
        }

        var success = _traceManagementService.RemoveTrackedId(traceId);
        
        if (!success)
        {
            return NotFound($"Trace ID '{traceId}' not found or could not be removed.");
        }

        return NoContent();
    }
}
