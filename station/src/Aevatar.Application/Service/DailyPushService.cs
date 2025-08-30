using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using GodGPT.GAgents.DailyPush;

using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, Aevatar.Application.Contracts.DailyPush.DeviceRequest request, Domain.Shared.GodGPTChatLanguage language)
    {
        try
        {
            // Convert to internal enum type
            var languageEnum = ConvertGodGPTChatLanguageToGodGPTLanguage(language);
            
            // Simple timezone validation - let it throw if invalid
            if (!string.IsNullOrEmpty(request.TimeZoneId))
            {
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
                }
                catch (TimeZoneNotFoundException ex)
                {
                    throw new ArgumentException($"Invalid timezone ID: {request.TimeZoneId}", ex);
                }
                catch (InvalidTimeZoneException ex)
                {
                    throw new ArgumentException($"Invalid timezone format: {request.TimeZoneId}", ex);
                }
            }

            var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);

            // Check for existing device to detect language changes
            var existingDeviceResult = await chatManagerGAgent.GetDeviceStatusAsync(request.DeviceId);
            var existingDevice = existingDeviceResult as UserDeviceInfo;
            bool languageChanged = false;
            
            if (existingDevice != null)
            {
                // Convert enum to string for comparison with stored string value
                var newLanguageString = ConvertGodGPTLanguageToString(languageEnum);
                var existingLanguageString = existingDevice.PushLanguage;
                languageChanged = !string.Equals(existingLanguageString, newLanguageString, StringComparison.OrdinalIgnoreCase);
                
                if (languageChanged)
                {
                    _logger.LogInformation("Language changed for device {DeviceId} (User: {UserId}): {OldLanguage} → {NewLanguage}", 
                        request.DeviceId, userId, existingLanguageString, newLanguageString);
                }
            }
            
            // Call GAgent with basic types - convert enum to string
            var languageString = ConvertGodGPTLanguageToString(languageEnum);
            
            var isNewRegistration = await chatManagerGAgent.RegisterOrUpdateDeviceAsync(
                request.DeviceId,
                request.PushToken,
                request.TimeZoneId,
                request.PushEnabled,
                languageString
            );
            
            _logger.LogInformation("Device {DeviceId} registered/updated for user {UserId}, isNew: {IsNew}, languageChanged: {LanguageChanged}", 
                request.DeviceId, userId, isNewRegistration, languageChanged);
                
            return isNewRegistration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register/update device {DeviceId} for user {UserId}", 
                request.DeviceId, userId);
            throw;
        }
    }

    public async Task MarkPushAsReadAsync(Guid userId, string deviceId)
    {
        try
        {
            var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            await chatManagerGAgent.MarkPushAsReadAsync(deviceId);
            
            _logger.LogInformation("Push marked as read for user {UserId} with device {DeviceId}", userId, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark push as read for user {UserId} with device {DeviceId}", userId, deviceId);
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
                PushLanguage = deviceInfo.PushLanguage,
                PushToken = deviceInfo.PushToken
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device status for {DeviceId}, user {UserId}", deviceId, userId);
            throw;
        }
    }
    
    /// <summary>
    /// Convert GodGPTChatLanguage enum to GodGPTLanguage enum
    /// </summary>
    private static GodGPTLanguage ConvertGodGPTChatLanguageToGodGPTLanguage(Domain.Shared.GodGPTChatLanguage chatLanguage)
    {
        return chatLanguage switch
        {
            Domain.Shared.GodGPTChatLanguage.English => GodGPTLanguage.English,
            Domain.Shared.GodGPTChatLanguage.TraditionalChinese => GodGPTLanguage.TraditionalChinese,
            Domain.Shared.GodGPTChatLanguage.Spanish => GodGPTLanguage.Spanish,
            Domain.Shared.GodGPTChatLanguage.CN => GodGPTLanguage.CN,
            _ => GodGPTLanguage.English
        };
    }
    
    private static string ConvertGodGPTLanguageToString(GodGPTLanguage language)
    {
        return language switch
        {
            GodGPTLanguage.TraditionalChinese => "zh-tw",  // Traditional Chinese
            GodGPTLanguage.CN => "zh-cn",                  // Simplified Chinese 
            GodGPTLanguage.Spanish => "es",
            GodGPTLanguage.English => "en",
            _ => "en"
        };
    }
}
