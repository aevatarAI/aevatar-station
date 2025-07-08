using System;

namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Data transfer object for user reward record information
/// </summary>
public class UserRewardRecordDto
{
    /// <summary>
    /// User identifier
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Twitter username
    /// </summary>
    public required string TwitterUsername { get; set; }
    
    /// <summary>
    /// Reward amount in credits
    /// </summary>
    public int RewardAmount { get; set; }
    
    /// <summary>
    /// Date when the reward was earned (UTC)
    /// </summary>
    public DateTime RewardDate { get; set; }
    
    /// <summary>
    /// Reason for the reward
    /// </summary>
    public required string RewardReason { get; set; }
    
    /// <summary>
    /// Transaction ID or reference
    /// </summary>
    public required string TransactionId { get; set; }
    
    /// <summary>
    /// Status of the reward (e.g., "Pending", "Completed", "Failed")
    /// </summary>
    public required string Status { get; set; }
} 