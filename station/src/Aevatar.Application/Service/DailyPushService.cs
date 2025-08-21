using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.DailyPush;
using GodGPT.GAgents.ChatManager;
using GodGPT.GAgents.DailyPush;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Services;

namespace Aevatar.Application.Service;

/// <summary>
/// Service for daily push notification operations
/// </summary>
public class DailyPushService : ApplicationService, IDailyPushService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<DailyPushService> _logger;

    public DailyPushService(IClusterClient clusterClient, ILogger<DailyPushService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, Aevatar.Application.Contracts.DailyPush.DeviceRequest request)
    {
        try
        {
            var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            
            // Convert station DTO to godgpt DTO
            var godGptRequest = new GodGPT.GAgents.DailyPush.DeviceRequest
            {
                DeviceId = request.DeviceId,
                PushToken = request.PushToken,
                TimeZoneId = request.TimeZoneId,
                PushEnabled = request.PushEnabled,
                PushLanguage = string.IsNullOrEmpty(request.PushLanguage) ? "en" : request.PushLanguage
            };

            var isNewRegistration = await chatManagerGAgent.RegisterOrUpdateDeviceAsync(godGptRequest);
            
            _logger.LogInformation("Device {DeviceId} registered/updated for user {UserId}, isNew: {IsNew}", 
                request.DeviceId, userId, isNewRegistration);
                
            return isNewRegistration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register/update device {DeviceId} for user {UserId}", 
                request.DeviceId, userId);
            throw;
        }
    }

    public async Task MarkPushAsReadAsync(Guid userId, string pushToken)
    {
        try
        {
            var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            await chatManagerGAgent.MarkPushAsReadAsync(pushToken);
            
            _logger.LogInformation("Push marked as read for user {UserId} with token {TokenPrefix}...", 
                userId, pushToken[..Math.Min(8, pushToken.Length)]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark push as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DeviceStatusResponse?> GetDeviceStatusAsync(Guid userId, string deviceId)
    {
        try
        {
            var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            var deviceInfo = await chatManagerGAgent.GetDeviceStatusAsync(deviceId);
            
            if (deviceInfo == null)
            {
                _logger.LogDebug("Device {DeviceId} not found for user {UserId}", deviceId, userId);
                return null;
            }
            
            // Convert godgpt DTO to station DTO
            var response = new DeviceStatusResponse
            {
                DeviceId = deviceInfo.DeviceId,
                TimeZoneId = deviceInfo.TimeZoneId,
                PushEnabled = deviceInfo.PushEnabled,
                PushLanguage = deviceInfo.PushLanguage
            };
            
            _logger.LogDebug("Retrieved device status for {DeviceId}, user {UserId}", deviceId, userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device status for {DeviceId}, user {UserId}", deviceId, userId);
            throw;
        }
    }
}
