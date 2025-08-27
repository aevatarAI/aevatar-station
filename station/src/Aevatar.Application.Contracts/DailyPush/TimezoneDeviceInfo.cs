using System;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Device information in timezone for debugging - TODO: Remove before production
/// </summary>
public class TimezoneDeviceInfo
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public string PushToken { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public string PushLanguage { get; set; } = "";
    public bool PushEnabled { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastTokenUpdate { get; set; }
    public bool HasEnabledDeviceInTimezone { get; set; }
    public int TotalDeviceCount { get; set; }
    public int EnabledDeviceCount { get; set; }
}
