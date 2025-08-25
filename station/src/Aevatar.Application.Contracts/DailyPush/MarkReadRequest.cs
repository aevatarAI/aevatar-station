using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Request DTO for marking daily push as read
/// </summary>
public class MarkReadRequest
{
    /// <summary>
    /// Firebase push token to identify which device read the notification
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string PushToken { get; set; } = "";
}
