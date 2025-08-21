using System;
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
    /// <returns>True if this is a new device registration, false if update</returns>
    Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, DeviceRequest request);
    
    /// <summary>
    /// Mark daily push as read for specific device
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pushToken">Push token to identify device</param>
    Task MarkPushAsReadAsync(Guid userId, string pushToken);
    
    /// <summary>
    /// Get device status by device ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Device status or null if not found</returns>
    Task<DeviceStatusResponse?> GetDeviceStatusAsync(Guid userId, string deviceId);
}
