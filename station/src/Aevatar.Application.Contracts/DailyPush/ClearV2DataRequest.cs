using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Request DTO for clearing V2 device data (testing purposes only)
/// </summary>
public class ClearV2DataRequest
{
    /// <summary>
    /// User ID to clear V2 device data for
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Confirmation flag - must be true to proceed with deletion
    /// </summary>
    [Required]
    public bool ConfirmClear { get; set; }
}
