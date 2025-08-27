using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Service interface for daily push notification operations
/// </summary>
public interface IDailyPushService
{
    /// <summary>
    /// Register or update device for daily push notifications
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Device registration request</param>
    /// <param name="language">Language from HTTP context</param>
    /// <returns>True if this is a new device registration, false if update</returns>
    Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, DeviceRequest request, Domain.Shared.GodGPTChatLanguage language);
    
    /// <summary>
    /// Mark daily push as read for specific device
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device ID to identify device</param>
    Task MarkPushAsReadAsync(Guid userId, string deviceId);
    
    /// <summary>
    /// Get device status by device ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Device status or null if not found</returns>
    Task<DeviceStatusResponse?> GetDeviceStatusAsync(Guid userId, string deviceId);
    
    // Test mode methods - TODO: Remove before production
    
    /// <summary>
    /// Start test mode for rapid push testing in specified timezone
    /// </summary>
    /// <param name="timezone">Timezone identifier (e.g., "Asia/Shanghai")</param>
    /// <param name="intervalSeconds">Push interval in seconds (default: 600 seconds = 10 minutes)</param>
    Task StartTestModeAsync(string timezone, int intervalSeconds = 600);
    
    /// <summary>
    /// Stop test mode and cleanup test reminders for specified timezone
    /// </summary>
    /// <param name="timezone">Timezone identifier (e.g., "Asia/Shanghai")</param>
    Task StopTestModeAsync(string timezone);
    
    /// <summary>
    /// Get test mode status for specified timezone
    /// </summary>
    /// <param name="timezone">Timezone identifier (e.g., "Asia/Shanghai")</param>
    /// <returns>Test mode status information</returns>
    Task<(bool IsActive, DateTime StartTime, int RoundsCompleted, int MaxRounds)> GetTestStatusAsync(string timezone);
    
    /// <summary>
    /// Get devices in specified timezone for debugging purposes
    /// </summary>
    /// <param name="timezone">Timezone identifier (e.g., "Asia/Shanghai")</param>
    /// <returns>List of device information in the timezone</returns>
    Task<List<TimezoneDeviceInfo>> GetDevicesInTimezoneAsync(string timezone);
}
