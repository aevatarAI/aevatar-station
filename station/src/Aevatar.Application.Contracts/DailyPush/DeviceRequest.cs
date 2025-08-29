using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Request DTO for device registration/update in daily push system
/// </summary>
public class DeviceRequest
{
    /// <summary>
    /// Unique device identifier (client-generated persistent ID)
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string DeviceId { get; set; } = "";
    
    /// <summary>
    /// Firebase push token for this device
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string PushToken { get; set; } = "";
    
    /// <summary>
    /// Device timezone (e.g., "Asia/Shanghai", "America/New_York")
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 1)]

    public string TimeZoneId { get; set; } = "";
    
    /// <summary>
    /// Whether push notifications are enabled for this device
    /// </summary>
    public bool PushEnabled { get; set; } = true;
    

}
