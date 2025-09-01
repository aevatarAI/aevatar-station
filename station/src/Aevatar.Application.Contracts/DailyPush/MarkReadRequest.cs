using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Request DTO for marking daily push as read
/// </summary>
public class MarkReadRequest
{
    /// <summary>
    /// Device ID to identify which device read the notification
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string DeviceId { get; set; } = "";
}
