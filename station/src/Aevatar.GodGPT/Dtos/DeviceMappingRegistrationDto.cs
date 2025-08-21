using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Dtos;

/// <summary>
/// Device mapping registration request DTO
/// Used for registering Firebase Instance ID mapping for Apple attribution tracking
/// </summary>
public class DeviceMappingRegistrationDto
{
    /// <summary>
    /// iOS Identifier for Vendor - stable across app reinstalls for same vendor
    /// </summary>
    [Required]
    public string Idfv { get; set; }
    
    /// <summary>
    /// Firebase App Instance ID - required for Firebase Analytics events
    /// </summary>
    [Required]
    public string AppInstanceId { get; set; }
    
    /// <summary>
    /// iOS App Bundle Identifier
    /// </summary>
    [Required]
    public string BundleId { get; set; }
    
    /// <summary>
    /// App installation timestamp from client
    /// </summary>
    public DateTime? InstallTimestamp { get; set; }
    
    /// <summary>
    /// iOS Advertising Identifier - optional, requires user consent
    /// </summary>
    public string? Idfa { get; set; }
    
    /// <summary>
    /// Device model information (e.g., iPhone14,2)
    /// </summary>
    public string? DeviceModel { get; set; }
    
    /// <summary>
    /// iOS version (e.g., 17.1.1)
    /// </summary>
    public string? OsVersion { get; set; }
    
    /// <summary>
    /// App version at time of registration
    /// </summary>
    public string? AppVersion { get; set; }
}

/// <summary>
/// Device mapping registration response DTO
/// </summary>
public class DeviceMappingRegistrationResponseDto
{
    public bool Success { get; set; }
    
    public string Message { get; set; }
    
    public bool IsUpdate { get; set; }
}

/// <summary>
/// Internal device mapping data structure for Redis storage
/// </summary>
public class DeviceMappingData
{
    public string Idfv { get; set; }
    public string AppInstanceId { get; set; }
    public string BundleId { get; set; }
    public DateTime? InstallTimestamp { get; set; }
    public string? Idfa { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RegistrationCount { get; set; } = 1;
}
