using System.Collections.Generic;
using Aevatar.Core.Interception.Configurations;

namespace Aevatar.Dto;

/// <summary>
/// DTO for trace configuration information.
/// </summary>
public class TraceConfigDto
{
    public bool IsEnabled { get; set; }
    public HashSet<string> TrackedIds { get; set; } = new();
    public TraceConfig? Configuration { get; set; }
}

/// <summary>
/// Request DTO for updating trace configuration.
/// </summary>
public class UpdateTraceConfigRequest
{
    public bool IsEnabled { get; set; }
    // Add other configuration properties as needed
}

/// <summary>
/// DTO for tracing status information.
/// </summary>
public class TracingStatusDto
{
    public bool IsEnabled { get; set; }
    public int TrackedTraceCount { get; set; }
}
