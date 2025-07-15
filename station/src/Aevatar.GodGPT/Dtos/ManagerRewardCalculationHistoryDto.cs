using System;

namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Manager data transfer object for reward calculation history information
/// </summary>
public class ManagerRewardCalculationHistoryDto
{
    /// <summary>
    /// Calculation date
    /// </summary>
    public DateTime CalculationDate { get; set; }
    
    /// <summary>
    /// Calculation date as UTC timestamp in seconds
    /// </summary>
    public long CalculationDateUtc { get; set; }
    
    /// <summary>
    /// Whether the calculation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Number of users who received rewards
    /// </summary>
    public int UsersRewarded { get; set; }
    
    /// <summary>
    /// Total credits distributed
    /// </summary>
    public int TotalCreditsDistributed { get; set; }
    
    /// <summary>
    /// Processing duration
    /// </summary>
    public TimeSpan ProcessingDuration { get; set; }
    
    /// <summary>
    /// Error message if calculation failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Start time of processed time range
    /// </summary>
    public DateTime ProcessedTimeRangeStart { get; set; }
    
    /// <summary>
    /// End time of processed time range
    /// </summary>
    public DateTime ProcessedTimeRangeEnd { get; set; }
} 