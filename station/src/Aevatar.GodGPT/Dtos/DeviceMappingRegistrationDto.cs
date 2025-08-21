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
    [StringLength(36, MinimumLength = 36)]
    public string Idfv { get; set; } = null!;
    
    /// <summary>
    /// Firebase App Instance ID - required for Firebase Analytics events
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public string AppInstanceId { get; set; } = null!;
    
    /// <summary>
    /// iOS App Bundle Identifier
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string BundleId { get; set; } = null!;
    
    /// <summary>
    /// App installation timestamp from client
    /// </summary>
    [Required]
    public DateTime InstallTimestamp { get; set; }
    
    /// <summary>
    /// iOS Advertising Identifier - optional, requires user consent
    /// </summary>
    [StringLength(36, MinimumLength = 36)]
    public string? Idfa { get; set; }
    
    /// <summary>
    /// Device model information (e.g., iPhone14,2)
    /// </summary>
    [StringLength(50)]
    public string? DeviceModel { get; set; }
    
    /// <summary>
    /// iOS version (e.g., 17.1.1)
    /// </summary>
    [StringLength(20)]
    public string? OsVersion { get; set; }
    
    /// <summary>
    /// App version at time of registration
    /// </summary>
    [StringLength(50)]
    public string? AppVersion { get; set; }
}

/// <summary>
/// Device mapping registration response DTO
/// </summary>
public class DeviceMappingRegistrationResponseDto
{
    /// <summary>
    /// Whether the registration was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Response message or error details
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// Server timestamp when registration was processed
    /// </summary>
    public DateTime RegisteredAt { get; set; }
    
    /// <summary>
    /// Whether this was an update to existing mapping
    /// </summary>
    public bool IsUpdate { get; set; }
    
    /// <summary>
    /// Expiration time for the mapping data
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Internal device mapping data structure for Redis storage
/// </summary>
public class DeviceMappingData
{
    public string Idfv { get; set; } = null!;
    public string AppInstanceId { get; set; } = null!;
    public string BundleId { get; set; } = null!;
    public DateTime InstallTimestamp { get; set; }
    public string? Idfa { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RegistrationCount { get; set; } = 1;
}
