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
using System.Diagnostics;


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
            _ => GodGPTLanguage.English
        };
    }
    
    private static string ConvertGodGPTLanguageToString(GodGPTLanguage language)
    {
        return language switch
        {
            GodGPTLanguage.TraditionalChinese => "zh-tw",  // Traditional Chinese
            GodGPTLanguage.Spanish => "es",
            GodGPTLanguage.English => "en",
            _ => "en"
        };
    }

    /// <summary>
    /// Send test push notification to all devices in specified timezone
    /// Bypasses all business logic restrictions (read status, deduplication, etc.)
    /// </summary>
    public async Task<TestPushResult> SendTestPushToTimezoneAsync(string timeZoneId, string title, string content)
    {
        var stopwatch = Stopwatch.StartNew();
        var executionTime = DateTime.UtcNow;
        
        _logger.LogInformation("Starting test push to timezone {TimeZone} with title: '{Title}'", timeZoneId, title);

        var result = new TestPushResult
        {
            TimeZoneId = timeZoneId,
            Title = title,
            Content = content,
            ExecutionTime = executionTime,
            Success = false
        };

        try
        {
            // Validate timezone
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid timezone ID: {timeZoneId}", ex);
            }

            // Get timezone index GAgent
            var timezoneIndexGAgent = _clusterClient.GetGrain<IPushSubscriberIndexGAgent>(DailyPushConstants.TimezoneToGuid(timeZoneId));

            // Get all users in timezone (use large batch to get all at once for testing)
            const int batchSize = 10000;
            var allUsers = new List<Guid>();
            int skip = 0;
            List<Guid> userBatch;

            do
            {
                userBatch = await timezoneIndexGAgent.GetActiveUsersInTimezoneAsync(skip, batchSize);
                allUsers.AddRange(userBatch);
                skip += batchSize;
            } while (userBatch.Count == batchSize);

            result.TotalUsers = allUsers.Count;
            _logger.LogInformation("Found {UserCount} users in timezone {TimeZone}", allUsers.Count, timeZoneId);

            if (allUsers.Count == 0)
            {
                result.Success = true;
                result.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Process each user's devices
            var totalDevices = 0;
            var successfulPushes = 0;
            var failedPushes = 0;
            var usersWithNoDevices = 0;

            foreach (var userId in allUsers)
            {
                try
                {
                    var chatManagerGAgent = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
                    
                    // Create test push data
                    var testPushData = new Dictionary<string, object>
                    {
                        ["type"] = "test_push",
                        ["timezone"] = timeZoneId,
                        ["timestamp"] = executionTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ["test_id"] = Guid.NewGuid().ToString()
                    };

                    // Send test push to this user - bypassing all daily push business logic
                    var userDeviceCount = await chatManagerGAgent.SendTestPushNotificationAsync(title, content, testPushData);
                    
                    if (userDeviceCount > 0)
                    {
                        totalDevices += userDeviceCount;
                        successfulPushes += userDeviceCount;
                    }
                    else
                    {
                        usersWithNoDevices++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send test push to user {UserId}", userId);
                    failedPushes++;
                }
            }

            stopwatch.Stop();

            result.TotalDevices = totalDevices;
            result.SuccessfulPushes = successfulPushes;
            result.FailedPushes = failedPushes;
            result.UsersWithNoDevices = usersWithNoDevices;
            result.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;
            result.Success = true;

            _logger.LogInformation(
                "Test push completed for timezone {TimeZone}: {TotalUsers} users, {TotalDevices} devices, {SuccessfulPushes} successful, {FailedPushes} failed, {UsersWithNoDevices} users with no devices. Duration: {Duration}ms",
                timeZoneId, result.TotalUsers, result.TotalDevices, result.SuccessfulPushes, result.FailedPushes, result.UsersWithNoDevices, result.ExecutionDurationMs);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Test push failed for timezone {TimeZone}: {ErrorMessage}", timeZoneId, ex.Message);
        }

        return result;
    }
}
