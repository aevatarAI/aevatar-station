namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Response DTO for device status query
/// </summary>
public class DeviceStatusResponse
{
    /// <summary>
    /// Device identifier (null if device not found)
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Device timezone
    /// </summary>
    public string TimeZoneId { get; set; } = "";
    
    /// <summary>
    /// Whether push notifications are enabled
    /// </summary>
    public bool PushEnabled { get; set; }
    
    /// <summary>
    /// Preferred language for push notifications
    /// </summary>
    public string PushLanguage { get; set; } = "";
    
    /// <summary>
    /// Push token for Firebase notifications
    /// </summary>
    public string PushToken { get; set; } = "";
}
