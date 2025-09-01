namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Response DTO for Google Pay/Play verification
/// </summary>
public class GooglePayVerificationResponseDto
{
    /// <summary>
    /// Whether the payment verification was successful
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Verification result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction ID from payment processor
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription start date (if applicable)
    /// </summary>
    public DateTime? SubscriptionStartDate { get; set; }
    
    /// <summary>
    /// Subscription end date (if applicable)
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }
    
    /// <summary>
    /// Error code if verification failed
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Product ID that was verified
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment platform identifier
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Google Play payment state (for Android purchases)
    /// </summary>
    public int? PaymentState { get; set; }
    
    /// <summary>
    /// Whether subscription auto-renews (for subscriptions)
    /// </summary>
    public bool? AutoRenewing { get; set; }
    
    /// <summary>
    /// Purchase timestamp in milliseconds
    /// </summary>
    public long? PurchaseTimeMillis { get; set; }
    
    /// <summary>
    /// Purchase token (for reference)
    /// </summary>
    public string PurchaseToken { get; set; } = string.Empty;
}
