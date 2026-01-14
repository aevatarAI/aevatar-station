using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Admin.Models;

namespace Aevatar.Admin.Services;

/// <summary>
/// Service interface for exporting GAgent state data
/// </summary>
public interface IStateExportService
{
    /// <summary>
    /// Start an async export task
    /// </summary>
    /// <param name="types">State types to export, null means all</param>
    /// <returns>Task ID for tracking</returns>
    Task<string> StartExportAsync(List<string>? types);
    
    /// <summary>
    /// Get export task status
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>Task status or null if not found</returns>
    ExportTask? GetTaskStatus(string taskId);
    
    /// <summary>
    /// Get export data and remove task from memory
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>Exported records or null if not available</returns>
    List<ExportedRecord>? GetAndRemoveTaskData(string taskId);
    
    /// <summary>
    /// Get list of available state types for export
    /// </summary>
    /// <returns>List of type names</returns>
    Task<List<string>> GetAvailableTypesAsync();
}
