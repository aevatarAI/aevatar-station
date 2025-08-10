namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Input DTO for Google Pay Web payment verification
/// </summary>
public class VerifyGooglePayInput
{
    /// <summary>
    /// Google Pay payment token from JS API
    /// </summary>
    public string PaymentToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Product ID (e.g., premium_monthly)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Google Pay order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Environment: "PRODUCTION" or "TEST"
    /// </summary>
    public string Environment { get; set; } = "PRODUCTION";
}
