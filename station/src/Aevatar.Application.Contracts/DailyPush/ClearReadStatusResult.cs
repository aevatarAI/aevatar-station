using System;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Result of clear read status operation
/// </summary>
public class ClearReadStatusResult
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public bool Cleared { get; set; }
    public DateTime Timestamp { get; set; }
}
