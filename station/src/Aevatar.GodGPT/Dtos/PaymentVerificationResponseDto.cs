namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Response DTO for payment verification operations
/// </summary>
public class PaymentVerificationResponseDto
{
    /// <summary>
    /// Whether the verification was successful
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Product ID
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription end date (if applicable)
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }
    
    /// <summary>
    /// Purchase timestamp in milliseconds
    /// </summary>
    public long PurchaseTimeMillis { get; set; }
    
    /// <summary>
    /// Error code (if any)
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;
}
