using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Request DTO for test push notification
/// </summary>
public class TestPushRequest
{
    /// <summary>
    /// Target timezone (e.g., "Asia/Shanghai")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TimeZoneId { get; set; } = "";

    /// <summary>
    /// Push notification title
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "";

    /// <summary>
    /// Push notification content/body
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = "";
}
