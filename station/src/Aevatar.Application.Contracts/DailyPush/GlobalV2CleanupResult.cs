using System;
using System.Collections.Generic;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Result of global V2 device data cleanup operation
/// </summary>
public class GlobalV2CleanupResult
{
    /// <summary>
    /// Total number of timezones processed
    /// </summary>
    public int TimezonesProcessed { get; set; }
    
    /// <summary>
    /// Total number of users processed across all timezones
    /// </summary>
    public int UsersProcessed { get; set; }
    
    /// <summary>
    /// Total number of V2 devices cleared across all users
    /// </summary>
    public int TotalDevicesCleared { get; set; }
    
    /// <summary>
    /// Number of users that had V2 devices cleared
    /// </summary>
    public int UsersWithClearedDevices { get; set; }
    
    /// <summary>
    /// Breakdown by timezone
    /// </summary>
    public Dictionary<string, TimezoneCleanupStats> TimezoneBreakdown { get; set; } = new();
    
    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
    
    /// <summary>
    /// Cleanup timestamp
    /// </summary>
    public DateTime CleanupTimestamp { get; set; }
    
    /// <summary>
    /// List of any errors encountered during cleanup
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Cleanup statistics for a specific timezone
/// </summary>
public class TimezoneCleanupStats
{
    /// <summary>
    /// Timezone ID
    /// </summary>
    public string TimezoneId { get; set; } = "";
    
    /// <summary>
    /// Number of users in this timezone
    /// </summary>
    public int UserCount { get; set; }
    
    /// <summary>
    /// Number of devices cleared in this timezone
    /// </summary>
    public int DevicesCleared { get; set; }
    
    /// <summary>
    /// Number of users with cleared devices in this timezone
    /// </summary>
    public int UsersWithClearedDevices { get; set; }
    
    /// <summary>
    /// Processing time for this timezone
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}
