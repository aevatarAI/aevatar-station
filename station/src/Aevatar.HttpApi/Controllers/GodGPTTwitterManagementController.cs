using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Aevatar.GodGPT.Dtos;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;

namespace Aevatar.Controllers;

/// <summary>
/// Controller for Twitter system management operations (monitoring and rewards)
/// These are system-level operations that require administrative privileges
/// </summary>
[RemoteService]
[ControllerName("TwitterManagement")]
[Route("api/godgpt/twitter-management")]
[Authorize]
public class GodGPTTwitterManagementController : AevatarController
{
    private readonly ILogger<GodGPTTwitterManagementController> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IGodGPTService _godGptService;

    public GodGPTTwitterManagementController(
        IClusterClient clusterClient, 
        ILogger<GodGPTTwitterManagementController> logger,
        IGodGPTService godGptService)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _godGptService = godGptService;
    }

    // Twitter Monitor Management Endpoints

    /// <summary>
    /// Manually trigger tweets fetching with default configuration
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpPost("monitor/fetch-manually")]
    public async Task<TwitterOperationResultDto> FetchTweetsManuallyAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][FetchTweetsManuallyAsync] Starting manual tweet fetch");
        
        var result = await _godGptService.FetchTweetsManuallyAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][FetchTweetsManuallyAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Refetch tweets by specified time range
    /// </summary>
    /// <param name="startTimeUtcSecond">Start time as UTC timestamp in seconds</param>
    /// <param name="endTimeUtcSecond">End time as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    [HttpPost("monitor/refetch-by-time-range")]
    public async Task<TwitterOperationResultDto> RefetchTweetsByTimeRangeAsync(
        [FromQuery] long startTimeUtcSecond, 
        [FromQuery] long endTimeUtcSecond)
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][RefetchTweetsByTimeRangeAsync] Starting refetch tweets from {StartTime} to {EndTime}",
            startTimeUtcSecond, endTimeUtcSecond);
        
        var result = await _godGptService.RefetchTweetsByTimeRangeAsync(startTimeUtcSecond, endTimeUtcSecond);
        
        _logger.LogDebug("[GodGPTTwitterManagementController][RefetchTweetsByTimeRangeAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Start automatic tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpPost("monitor/start")]
    public async Task<TwitterOperationResultDto> StartTweetMonitoringAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][StartTweetMonitoringAsync] Starting tweet monitoring task");
        
        var result = await _godGptService.StartTweetMonitoringAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][StartTweetMonitoringAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Stop automatic tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpPost("monitor/stop")]
    public async Task<TwitterOperationResultDto> StopTweetMonitoringAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][StopTweetMonitoringAsync] Stopping tweet monitoring task");
        
        var result = await _godGptService.StopTweetMonitoringAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][StopTweetMonitoringAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Get current status of tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpGet("monitor/status")]
    public async Task<TwitterOperationResultDto> GetTweetMonitoringStatusAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[GodGPTTwitterManagementController][GetTweetMonitoringStatusAsync] Getting tweet monitoring status");
        
        var result = await _godGptService.GetTweetMonitoringStatusAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][GetTweetMonitoringStatusAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    // Twitter Reward Management Endpoints

    /// <summary>
    /// Manually trigger reward calculation for specific date
    /// </summary>
    /// <param name="targetDateUtcSeconds">Target date as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    [HttpPost("rewards/trigger-calculation")]
    public async Task<TwitterOperationResultDto> TriggerRewardCalculationAsync([FromQuery] long targetDateUtcSeconds)
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][TriggerRewardCalculationAsync] Starting manual reward calculation for date: {TargetDate}",
            targetDateUtcSeconds);
        
        var result = await _godGptService.TriggerRewardCalculationAsync(targetDateUtcSeconds);
        
        _logger.LogDebug("[GodGPTTwitterManagementController][TriggerRewardCalculationAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Clear reward records for specific date (for testing purposes)
    /// </summary>
    /// <param name="targetDateUtcSeconds">Target date as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    [HttpDelete("rewards/clear-by-day")]
    public async Task<TwitterOperationResultDto> ClearRewardByDayAsync([FromQuery] long targetDateUtcSeconds)
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][ClearRewardByDayAsync] Starting clear reward records for date: {TargetDate}",
            targetDateUtcSeconds);
        
        var result = await _godGptService.ClearRewardByDayAsync(targetDateUtcSeconds);
        
        _logger.LogDebug("[GodGPTTwitterManagementController][ClearRewardByDayAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Start automatic reward calculation task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpPost("rewards/start")]
    public async Task<TwitterOperationResultDto> StartRewardCalculationAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][StartRewardCalculationAsync] Starting reward calculation task");
        
        var result = await _godGptService.StartRewardCalculationAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][StartRewardCalculationAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Stop automatic reward calculation task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    [HttpPost("rewards/stop")]
    public async Task<TwitterOperationResultDto> StopRewardCalculationAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][StopRewardCalculationAsync] Stopping reward calculation task");
        
        var result = await _godGptService.StopRewardCalculationAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][StopRewardCalculationAsync] completed with success: {Success}, duration: {Duration}ms",
            result.IsSuccess, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Get user rewards by user ID (returns dateKey and filtered UserRewardRecordDto list)
    /// </summary>
    /// <param name="userId">User ID to retrieve rewards for</param>
    /// <returns>TwitterApiResultDto containing dictionary of date keys and reward records</returns>
    [HttpGet("rewards/user/{userId}")]
    public async Task<Dictionary<string, List<ManagerUserRewardRecordDto>>> GetUserRewardsByUserIdAsync([FromRoute] string userId)
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][GetUserRewardsByUserIdAsync] Getting user rewards for user ID: {UserId}", userId);
        
        var result = await _godGptService.GetUserRewardsByUserIdAsync(userId);
        
        _logger.LogDebug("[GodGPTTwitterManagementController][GetUserRewardsByUserIdAsync] completed with success: {Success}, duration: {Duration}ms",
            true, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Get full calculation history list
    /// </summary>
    /// <returns>List of ManagerRewardCalculationHistoryDto</returns>
    [HttpGet("rewards/calculation-history")]
    public async Task<List<ManagerRewardCalculationHistoryDto>> GetCalculationHistoryListAsync()
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][GetCalculationHistoryListAsync] Getting calculation history list");
        
        var result = await _godGptService.GetCalculationHistoryListAsync();
        
        _logger.LogDebug("[GodGPTTwitterManagementController][GetCalculationHistoryListAsync] completed with {Count} records, duration: {Duration}ms",
            result?.Count ?? 0, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Reset awakening state for a specific user for testing purposes
    /// </summary>
    /// <param name="userId">User ID to reset awakening state for</param>
    /// <returns>Operation result with success status</returns>
    [HttpPost("awakening/reset-for-testing")]
    public async Task<TwitterOperationResultDto> ResetAwakeningStateForTestingAsync([FromQuery] Guid userId)
    {
        await BeforeCheckUserIsManager();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GodGPTTwitterManagementController][ResetAwakeningStateForTestingAsync] Starting reset for userId: {UserId}", userId);
        
        try
        {
            var resetSuccess = await _godGptService.ResetAwakeningStateForTestingAsync(userId);
            
            _logger.LogDebug("[GodGPTTwitterManagementController][ResetAwakeningStateForTestingAsync] completed with success: {Success}, duration: {Duration}ms",
                resetSuccess, stopwatch.ElapsedMilliseconds);
            
            return new TwitterOperationResultDto
            {
                IsSuccess = resetSuccess
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTTwitterManagementController][ResetAwakeningStateForTestingAsync] Error occurred during reset for userId: {UserId}", userId);
            return new TwitterOperationResultDto
            {
                IsSuccess = false,
                ErrorMessage = $"Error occurred: {ex.Message}"
            };
        }
    }

    private async Task BeforeCheckUserIsManager()
    {
        var currentUserId = (Guid)CurrentUser.Id!;
        if (currentUserId == Guid.Empty || currentUserId == null)
        {
            throw new SecurityException($"currentUserId is null {currentUserId}");
        }
      
        if (!await _godGptService.CheckIsManager(currentUserId.ToString()))
        {
            _logger.LogInformation($"currentUserId is not manager {currentUserId}");
            throw new SecurityException($"currentUserId is not manager {currentUserId}");
        }
        
    }
} 