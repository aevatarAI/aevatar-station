using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Models;
using Aevatar.Services;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Aevatar.StateExport;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;

namespace Aevatar.Controllers;

/// <summary>
/// Paged State Export API - short requests, no timeout issues
/// </summary>
[RemoteService]
[ControllerName("StateExport")]
[Route("api/admin/export")]
// [Authorize(Policy = AevatarPermissions.AdminPolicy)] // Temporarily disabled for testing
public class StateExportController : AevatarController
{
    private readonly StateExportService _exportService;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<StateExportController> _logger;

    public StateExportController(
        StateExportService exportService,
        IClusterClient clusterClient,
        ILogger<StateExportController> logger)
    {
        _exportService = exportService;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Get export summary - shows total counts per type
    /// GET /api/admin/export/summary?types=UserStatistics,UserQuota
    /// </summary>
    [HttpGet("summary")]
    public async Task<ExportSummaryDto> GetSummary([FromQuery] List<string>? types)
    {
        _logger.LogInformation("Getting export summary for types: {Types}", 
            types != null ? string.Join(", ", types) : "all");
        
        return await _exportService.GetExportSummaryAsync(types);
    }

    /// <summary>
    /// Step 2: Export single type with pagination (via Silo Grain - proper deserialization)
    /// GET /api/admin/export/grain?collection=StreamgodgptXxx&skip=0&limit=1000
    /// </summary>
    [HttpGet("grain")]
    public async Task<StateExportResult> ExportViaGrain(
        [FromQuery] string collection,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 1000)
    {
        if (string.IsNullOrEmpty(collection))
        {
            throw new UserFriendlyException("collection parameter is required");
        }
        
        if (limit > 5000)
        {
            limit = 5000;
        }
        
        _logger.LogInformation("Exporting via Grain: {Collection} skip={Skip} limit={Limit}", 
            collection, skip, limit);
        
        var grain = _clusterClient.GetGrain<IStateExportGrain>("state-export");
        return await grain.ExportAsync(collection, skip, limit);
    }

    /// <summary>
    /// Get available collections via Grain
    /// GET /api/admin/export/collections
    /// </summary>
    [HttpGet("collections")]
    public async Task<List<StateCollectionInfo>> GetCollections()
    {
        var grain = _clusterClient.GetGrain<IStateExportGrain>("state-export");
        return await grain.GetCollectionsAsync();
    }

    /// <summary>
    /// Step 2 (fallback): Export with string extraction only
    /// GET /api/admin/export/page?collection=StreamgodgptXxx&skip=0&limit=1000
    /// </summary>
    [HttpGet("page")]
    public async Task<PagedExportDto> ExportPage(
        [FromQuery] string collection,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 1000)
    {
        if (string.IsNullOrEmpty(collection))
        {
            throw new UserFriendlyException("collection parameter is required");
        }
        
        if (limit > 5000)
        {
            limit = 5000;
        }
        
        return await _exportService.ExportTypePagedAsync(collection, skip, limit);
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
