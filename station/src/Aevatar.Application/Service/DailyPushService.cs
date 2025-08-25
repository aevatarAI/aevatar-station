using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.DailyPush;
using Aevatar.Application.Grains.Agents.ChatManager;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Services;

namespace Aevatar.Application.Service;

// TODO: Remove these temporary interfaces once godgpt NuGet package is updated
// Temporary interface placeholder for new GAgent methods
public interface IChatManagerGAgentExtensions : IChatManagerGAgent
{
    Task<bool> RegisterOrUpdateDeviceAsync(string deviceId, string pushToken, string timeZoneId, bool? pushEnabled, string pushLanguage);
    Task MarkPushAsReadAsync(string pushToken);
    Task<dynamic> GetDeviceStatusAsync(string deviceId);
}

// Temporary interface placeholder for TimezoneSchedulerGAgent
public interface ITimezoneSchedulerGAgent : IGrainWithStringKey
{
    Task StartTestModeAsync();
    Task StopTestModeAsync();
    Task<(bool IsActive, DateTime StartTime, int RoundsCompleted, int MaxRounds)> GetTestStatusAsync();
}

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
            
            // Call GAgent with basic types - no DTO conversion needed
            var isNewRegistration = await chatManagerGAgent.RegisterOrUpdateDeviceAsync(
                request.DeviceId,
                request.PushToken,
                request.TimeZoneId,
                request.PushEnabled,
                string.IsNullOrEmpty(request.PushLanguage) ? "en" : request.PushLanguage
            );
            
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
    
    // Test mode methods - TODO: Remove before production
    
    /// <summary>
    /// Start test mode for rapid push testing in specified timezone
    /// </summary>
    public async Task StartTestModeAsync(string timezone)
    {
        try
        {
            _logger.LogInformation("Starting test mode for timezone {Timezone}", timezone);
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(timezone);
            await timezoneScheduler.StartTestModeAsync();
            
            _logger.LogInformation("Test mode started successfully for timezone {Timezone}", timezone);
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
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(timezone);
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
            
            var timezoneScheduler = _clusterClient.GetGrain<ITimezoneSchedulerGAgent>(timezone);
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
}
