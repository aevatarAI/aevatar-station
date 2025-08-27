using System;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Device information for timezone debugging purposes
/// </summary>
public class TimezoneDeviceInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Device ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Push token (truncated for privacy)
    /// </summary>
    public string PushToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Device timezone ID
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;
    
    /// <summary>
    /// Push language preference
    /// </summary>
    public string PushLanguage { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether push is enabled for this device
    /// </summary>
    public bool PushEnabled { get; set; }
    
    /// <summary>
    /// Whether this user has enabled devices in the specified timezone
    /// </summary>
    public bool HasEnabledDeviceInTimezone { get; set; }
    
    /// <summary>
    /// Total device count for this user
    /// </summary>
    public int TotalDeviceCount { get; set; }
    
    /// <summary>
    /// Enabled device count for this user in the specified timezone
    /// </summary>
    public int EnabledDeviceCount { get; set; }
}
