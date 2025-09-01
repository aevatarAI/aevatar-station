namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Request DTO for Google Play transaction verification using RevenueCat
/// </summary>
public class GooglePlayTransactionVerificationRequestDto
{
    /// <summary>
    /// Transaction identifier provided by RevenueCat
    /// </summary>
    public string TransactionIdentifier { get; set; } = string.Empty;
}
