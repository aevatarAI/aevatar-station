namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Input DTO for Google Play Android purchase verification
/// </summary>
public class VerifyGooglePlayInput
{
    /// <summary>
    /// Google Play purchase token
    /// </summary>
    public string PurchaseToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Product ID (e.g., premium_monthly)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Android app package name
    /// </summary>
    public string PackageName { get; set; } = string.Empty;
    
    /// <summary>
    /// Google Play order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
}
