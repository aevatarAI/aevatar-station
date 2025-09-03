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
    
    /// <summary>
    /// Send test push notification to all devices in specified timezone
    /// Bypasses all business logic restrictions (read status, deduplication, etc.)
    /// </summary>
    /// <param name="timeZoneId">Target timezone (e.g., "Asia/Shanghai")</param>
    /// <param name="title">Custom push title</param>
    /// <param name="content">Custom push content</param>
    /// <returns>Test push result with statistics</returns>
    Task<TestPushResult> SendTestPushToTimezoneAsync(string timeZoneId, string title, string content);
    
}
