using System;
using System.Collections.Generic;

namespace Aevatar.Dto;

/// <summary>
/// DTO for collection of tracked traces.
/// </summary>
public class TrackedTracesDto
{
    public List<string> TraceIds { get; set; } = new();
    public int Count { get; set; }
}

/// <summary>
/// DTO for individual tracked trace information.
/// </summary>
public class TrackedTraceDto
{
    public string TraceId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// Request DTO for adding a trace to tracking.
/// </summary>
public class AddTrackedTraceRequest
{
    public string TraceId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
