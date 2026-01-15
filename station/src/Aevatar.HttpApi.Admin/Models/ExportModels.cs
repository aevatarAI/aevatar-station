using System;
using System.Collections.Generic;

namespace Aevatar.Admin.Models;

/// <summary>
/// Export task status model
/// </summary>
public class ExportTask
{
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// Task status: pending, processing, completed, failed
    /// </summary>
    public string Status { get; set; } = "pending";
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public string? Error { get; set; }
    public List<ExportedRecord>? Data { get; set; }
}

/// <summary>
/// Single exported record
/// </summary>
public class ExportedRecord
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? State { get; set; }
}

/// <summary>
/// Start export request
/// </summary>
public class StartExportRequest
{
    /// <summary>
    /// State types to export, empty means export all
    /// </summary>
    public List<string>? Types { get; set; }
}

/// <summary>
/// Start export response
/// </summary>
public class StartExportResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Export status query response
/// </summary>
public class ExportStatusResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Available types response
/// </summary>
public class AvailableTypesResponse
{
    public List<string> AvailableTypes { get; set; } = new();
}
