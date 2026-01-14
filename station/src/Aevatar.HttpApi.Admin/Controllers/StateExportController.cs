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
/// API controller for exporting GAgent state data
/// </summary>
[RemoteService]
[ControllerName("StateExport")]
[Route("api/admin/export")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class StateExportController : AevatarController
{
    private readonly IStateExportService _exportService;
    private readonly ILogger<StateExportController> _logger;

    public StateExportController(
        IStateExportService exportService,
        ILogger<StateExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Start an export task
    /// POST /api/admin/export/start
    /// </summary>
    [HttpPost("start")]
    public async Task<StartExportResponse> StartExport([FromBody] StartExportRequest? request)
    {
        _logger.LogInformation("Starting state export. Types: {Types}", 
            request?.Types != null ? string.Join(", ", request.Types) : "all");
        
        var taskId = await _exportService.StartExportAsync(request?.Types);
        
        return new StartExportResponse
        {
            TaskId = taskId,
            Status = "processing",
            Message = "Export task started. Use GET /api/admin/export/status/{taskId} to check progress."
        };
    }

    /// <summary>
    /// Query export task status
    /// GET /api/admin/export/status/{taskId}
    /// </summary>
    [HttpGet("status/{taskId}")]
    public ExportStatusResponse GetStatus(string taskId)
    {
        var task = _exportService.GetTaskStatus(taskId);
        
        if (task == null)
        {
            throw new UserFriendlyException($"Export task {taskId} not found");
        }
        
        return new ExportStatusResponse
        {
            TaskId = task.TaskId,
            Status = task.Status,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            Error = task.Error
        };
    }

    /// <summary>
    /// Download export data
    /// GET /api/admin/export/download/{taskId}
    /// </summary>
    [HttpGet("download/{taskId}")]
    public IActionResult Download(string taskId)
    {
        var task = _exportService.GetTaskStatus(taskId);
        
        if (task == null)
        {
            throw new UserFriendlyException($"Export task {taskId} not found");
        }
        
        if (task.Status != "completed")
        {
            throw new UserFriendlyException($"Export task {taskId} is not ready. Current status: {task.Status}");
        }
        
        var data = _exportService.GetAndRemoveTaskData(taskId);
        
        if (data == null)
        {
            throw new UserFriendlyException($"Export data for task {taskId} not available");
        }
        
        _logger.LogInformation("Download completed for task {TaskId}. Records: {Count}", taskId, data.Count);
        
        return Ok(new
        {
            taskId,
            exportedAt = task.CompletedAt,
            totalCount = data.Count,
            records = data
        });
    }

    /// <summary>
    /// Get list of available state types for export
    /// GET /api/admin/export/types
    /// </summary>
    [HttpGet("types")]
    public async Task<AvailableTypesResponse> GetAvailableTypes()
    {
        var types = await _exportService.GetAvailableTypesAsync();
        
        return new AvailableTypesResponse
        {
            AvailableTypes = types
        };
    }
}
