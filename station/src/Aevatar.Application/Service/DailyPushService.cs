using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using GodGPT.GAgents.DailyPush;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Generic;
using Volo.Abp.Application.Services;


namespace Aevatar.Application.Service;

// TODO: Remove these temporary interfaces once godgpt NuGet package is updated  
// Note: These interfaces are for compilation only until godgpt NuGet package is updated

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
            
            _logger.LogInformation("📱 Device registration: DeviceId={DeviceId}, User={UserId}, Language={LanguageEnum}→{LanguageString}", 
                request.DeviceId, userId, languageEnum, languageString);
                
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
            
            _logger.LogDebug("Retrieved device status for {DeviceId}, user {UserId}", deviceId, userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device status for {DeviceId}, user {UserId}", deviceId, userId);
            throw;
        }
    }
    
    // Test mode methods - TODO: Remove before production
    
    /// <summary>
    /// Start test mode for rapid push testing in specified timezone
    /// </summary>
    /// <param name="timezone">Target timezone</param>
    /// <param name="intervalSeconds">Push interval in seconds (default: 600 = 10 minutes)</param>
    public async Task StartTestModeAsync(string timezone, int intervalSeconds = 600)
    {
        try
        {
            _logger.LogInformation("Starting test mode for timezone {Timezone} with {IntervalSeconds}s interval", 
                timezone, intervalSeconds);
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(DailyPushConstants.TimezoneToGuid(timezone));
            await timezoneScheduler.InitializeAsync(timezone);
            await timezoneScheduler.StartTestModeAsync(intervalSeconds);
            
            _logger.LogInformation("Test mode started successfully for timezone {Timezone} with {IntervalSeconds}s interval", 
                timezone, intervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start test mode for timezone {Timezone}", timezone);
            throw;
        }
    }
    
    /// <summary>
    /// Stop test mode and cleanup test reminders for specified timezone
    /// </summary>
    public async Task StopTestModeAsync(string timezone)
    {
        try
        {
            _logger.LogInformation("Stopping test mode for timezone {Timezone}", timezone);
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(DailyPushConstants.TimezoneToGuid(timezone));
            await timezoneScheduler.InitializeAsync(timezone);
            await timezoneScheduler.StopTestModeAsync();
            
            _logger.LogInformation("Test mode stopped successfully for timezone {Timezone}", timezone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop test mode for timezone {Timezone}", timezone);
            throw;
        }
    }
    
    /// <summary>
    /// Get test mode status for specified timezone
    /// </summary>
    public async Task<(bool IsActive, DateTime StartTime, int RoundsCompleted, int MaxRounds)> GetTestStatusAsync(string timezone)
    {
        try
        {
            _logger.LogDebug("Getting test status for timezone {Timezone}", timezone);
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(DailyPushConstants.TimezoneToGuid(timezone));
            await timezoneScheduler.InitializeAsync(timezone);
            var status = await timezoneScheduler.GetTestStatusAsync();
            
            _logger.LogDebug("Retrieved test status for timezone {Timezone}: Active={IsActive}, Rounds={RoundsCompleted}/{MaxRounds}", 
                timezone, status.IsActive, status.RoundsCompleted, status.MaxRounds);
                
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get test status for timezone {Timezone}", timezone);
            throw;
        }
    }
    
    /// <summary>
    /// Get all devices in specified timezone for debugging - TODO: Remove before production
    /// </summary>
    public async Task<List<TimezoneDeviceInfo>> GetDevicesInTimezoneAsync(string timezone = "Asia/Shanghai")
    {
        try
        {
            _logger.LogDebug("Getting devices in timezone {Timezone}", timezone);
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(DailyPushConstants.TimezoneToGuid(timezone));
            await timezoneScheduler.InitializeAsync(timezone);
            var devices = await timezoneScheduler.GetDevicesInTimezoneAsync();
            
            _logger.LogDebug("Retrieved {DeviceCount} devices in timezone {Timezone}", devices.Count, timezone);
                
            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get devices in timezone {Timezone}", timezone);
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
            GodGPTLanguage.TraditionalChinese => "zh",
            GodGPTLanguage.CN => "zh-sc",  // Use hyphen to match GodGPT project
            GodGPTLanguage.Spanish => "es",
            GodGPTLanguage.English => "en",
            _ => "en"
        };
    }
}
