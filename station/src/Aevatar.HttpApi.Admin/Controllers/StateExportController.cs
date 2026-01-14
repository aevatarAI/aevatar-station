using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Aevatar.Admin.Services;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

/// <summary>
/// Simple State Export API - no complex task management
/// </summary>
[RemoteService]
[ControllerName("StateExport")]
[Route("api/admin/export")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class StateExportController : AevatarController
{
    private readonly StateExportService _exportService;
    private readonly ILogger<StateExportController> _logger;

    public StateExportController(
        StateExportService exportService,
        ILogger<StateExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Stream export - for large datasets, streams JSON directly
    /// GET /api/admin/export/stream?types=UserStatistics,UserQuota
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamExport([FromQuery] List<string>? types)
    {
        _logger.LogInformation("Stream export requested for types: {Types}", 
            types != null ? string.Join(", ", types) : "all");
        
        Response.ContentType = "application/json";
        Response.Headers["Content-Disposition"] = "attachment; filename=\"state_export.json\"";
        
        await _exportService.StreamExportAsync(Response.Body, types);
    }

    /// <summary>
    /// Direct export - returns all data synchronously (simpler, for smaller datasets)
    /// GET /api/admin/export/all?types=UserStatistics,UserQuota
    /// </summary>
    [HttpGet("all")]
    public async Task<ExportResultDto> ExportAll([FromQuery] List<string>? types)
    {
        _logger.LogInformation("Direct export requested for types: {Types}", 
            types != null ? string.Join(", ", types) : "all");
        
        return await _exportService.ExportAllAsync(types);
    }

    /// <summary>
    /// Get available state types
    /// GET /api/admin/export/types
    /// </summary>
    [HttpGet("types")]
    public async Task<AvailableTypesResponse> GetAvailableTypes()
    {
        var types = await _exportService.GetAvailableTypesAsync();
        return new AvailableTypesResponse { AvailableTypes = types };
    }
}
