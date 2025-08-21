using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

/// <summary>
/// Device mapping service for Apple attribution and Firebase Analytics integration
/// </summary>
public interface IDeviceMappingService
{
    /// <summary>
    /// Register device mapping for Firebase Analytics attribution
    /// </summary>
    /// <param name="request">Device mapping registration data</param>
    /// <returns>Registration result</returns>
    Task<DeviceMappingRegistrationResponseDto> RegisterDeviceMappingAsync(DeviceMappingRegistrationDto request);

    /// <summary>
    /// Retrieve device mapping data
    /// </summary>
    /// <param name="bundleId">iOS App Bundle ID</param>
    /// <param name="idfv">iOS Identifier for Vendor</param>
    /// <returns>Device mapping data if found, null otherwise</returns>
    Task<DeviceMappingData?> GetDeviceMappingAsync(string idfv);

    /// <summary>
    /// Validate IDFV format
    /// </summary>
    /// <param name="idfv">IDFV to validate</param>
    /// <returns>True if valid UUID format, false otherwise</returns>
    bool IsValidIdfv(string idfv);
}

/// <summary>
/// Device mapping service implementation for Apple attribution and Firebase Analytics integration
/// </summary>
public class DeviceMappingService : IDeviceMappingService, ITransientDependency
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DeviceMappingService> _logger;

    // Redis key constants
    private const string DEVICE_MAPPING_KEY_PREFIX = "device_mapping";
    private const int MAPPING_TTL_DAYS = 30;

    public DeviceMappingService(
        IDistributedCache distributedCache,
        ILogger<DeviceMappingService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// Register device mapping for Firebase Analytics attribution
    /// </summary>
    public async Task<DeviceMappingRegistrationResponseDto> RegisterDeviceMappingAsync(DeviceMappingRegistrationDto request)
    {
        try
        {
            _logger.LogDebug("[DeviceMappingService][RegisterDeviceMappingAsync] Registering device mapping: IDFV={Idfv}, BundleId={BundleId}, AppInstanceId={AppInstanceId}",
                request.Idfv, request.BundleId, request.AppInstanceId);

            // 1. Validate IDFV format
            if (!IsValidIdfv(request.Idfv))
            {
                _logger.LogWarning("[DeviceMappingService][RegisterDeviceMappingAsync] Invalid IDFV format: {Idfv}", request.Idfv);
                return new DeviceMappingRegistrationResponseDto
                {
                    Success = false,
                    Message = "Invalid IDFV format"
                };
            }

            // 2. Check for existing mapping
            var existingMapping = await GetDeviceMappingAsync(request.Idfv);
            var isUpdate = existingMapping != null;

            // 3. Create mapping data
            var mappingData = new DeviceMappingData
            {
                Idfv = request.Idfv,
                AppInstanceId = request.AppInstanceId,
                BundleId = request.BundleId,
                InstallTimestamp = request.InstallTimestamp,
                Idfa = request.Idfa,
                DeviceModel = request.DeviceModel,
                OsVersion = request.OsVersion,
                AppVersion = request.AppVersion,
                CreatedAt = isUpdate ? existingMapping.CreatedAt : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RegistrationCount = isUpdate ? existingMapping.RegistrationCount + 1 : 1
            };

            // 4. Store mapping in Redis
            await StoreDeviceMappingAsync(mappingData);

            var response = new DeviceMappingRegistrationResponseDto
            {
                Success = true,
                Message = isUpdate ? "Device mapping updated successfully" : "Device mapping registered successfully",
                IsUpdate = isUpdate
            };

            _logger.LogDebug("[DeviceMappingService][RegisterDeviceMappingAsync] Device mapping {Action} successfully: IDFV={Idfv}",
                isUpdate ? "updated" : "registered", request.Idfv);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DeviceMappingService][RegisterDeviceMappingAsync] Error registering device mapping for IDFV: {Idfv}", request.Idfv);
            
            return new DeviceMappingRegistrationResponseDto
            {
                Success = false,
                Message = "Internal server error occurred during registration"
            };
        }
    }

    /// <summary>
    /// Retrieve device mapping data from Redis
    /// </summary>
    public async Task<DeviceMappingData?> GetDeviceMappingAsync(string idfv)
    {
        var key = BuildDeviceMappingKey(idfv);
        var value = await _distributedCache.GetStringAsync(key);
        
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogDebug("[DeviceMappingService][GetDeviceMappingAsync] No mapping found for key: {Key}", key);
            return null;
        }

        try
        {
            var mappingData = JsonSerializer.Deserialize<DeviceMappingData>(value);
            _logger.LogDebug("[DeviceMappingService][GetDeviceMappingAsync] Mapping found: Key={Key}, AppInstanceId={AppInstanceId}", 
                key, mappingData?.AppInstanceId);
            return mappingData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[DeviceMappingService][GetDeviceMappingAsync] Failed to deserialize mapping data for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Validate IDFV format (UUID)
    /// </summary>
    public bool IsValidIdfv(string idfv)
    {
        return Guid.TryParse(idfv, out _);
    }

    /// <summary>
    /// Store device mapping data in Redis
    /// </summary>
    private async Task StoreDeviceMappingAsync(DeviceMappingData mappingData)
    {
        var key = BuildDeviceMappingKey(mappingData.Idfv);
        var value = JsonSerializer.Serialize(mappingData);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MAPPING_TTL_DAYS)
        };

        await _distributedCache.SetStringAsync(key, value, options);
        
        _logger.LogDebug("[DeviceMappingService][StoreDeviceMappingAsync] Device mapping stored: Key={Key}", key);
    }

    /// <summary>
    /// Build Redis key for device mapping
    /// </summary>
    private static string BuildDeviceMappingKey(string idfv)
    {
        return $"{DEVICE_MAPPING_KEY_PREFIX}:{idfv}";
    }
}
