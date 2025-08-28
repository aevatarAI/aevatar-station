namespace Aevatar.Dto;

/// <summary>
/// DTO for trace information.
/// </summary>
public class TraceDto
{
    public string TraceId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request DTO for updating trace status.
/// </summary>
public class UpdateTraceRequest
{
    public bool IsEnabled { get; set; }
}
