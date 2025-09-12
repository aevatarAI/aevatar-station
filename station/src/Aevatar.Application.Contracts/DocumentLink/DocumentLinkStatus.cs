using System;

namespace Aevatar.Service;

public class DocumentLinkStatus
{
    public string Url { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
    public string? Error { get; set; }
} 