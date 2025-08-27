using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Aevatar.Dtos;

/// <summary>
/// Apple NSAdvertisingAttributionReportEndpoint attribution report DTO
/// Supports multiple versions of SKAdNetwork postback formats
/// </summary>
public class AppleAttributionReportDto
{
    /// <summary>
    /// SKAdNetwork version (version 2+)
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Ad network identifier
    /// </summary>
    [JsonPropertyName("ad-network-id")]
    [Required]
    public string AdNetworkId { get; set; } = string.Empty;

    /// <summary>
    /// Apple's attribution signature (version 2+)
    /// </summary>
    [JsonPropertyName("attribution-signature")]
    public string? AttributionSignature { get; set; }

    /// <summary>
    /// App Store ID of the advertised app
    /// </summary>
    [JsonPropertyName("app-id")]
    [Required]
    public long AppId { get; set; }

    /// <summary>
    /// Hierarchical source identifier (version 4+, replaces campaign-id)
    /// </summary>
    [JsonPropertyName("source-identifier")]
    public string? SourceIdentifier { get; set; }

    /// <summary>
    /// Campaign identifier (version 1-3)
    /// </summary>
    [JsonPropertyName("campaign-id")]
    public long? CampaignId { get; set; }

    /// <summary>
    /// App Store ID of the app that displays the ad (version 2+)
    /// </summary>
    [JsonPropertyName("source-app-id")]
    public long? SourceAppId { get; set; }

    /// <summary>
    /// Source domain for web ads (version 4+)
    /// </summary>
    [JsonPropertyName("source-domain")]
    public string? SourceDomain { get; set; }

    /// <summary>
    /// Conversion value (version 2+)
    /// </summary>
    [JsonPropertyName("conversion-value")]
    public int? ConversionValue { get; set; }

    /// <summary>
    /// Coarse conversion value (version 4+)
    /// </summary>
    [JsonPropertyName("coarse-conversion-value")]
    public string? CoarseConversionValue { get; set; }

    /// <summary>
    /// Whether attribution was won (version 3+)
    /// </summary>
    [JsonPropertyName("did-win")]
    public bool? DidWin { get; set; }

    /// <summary>
    /// Fidelity type (version 2.2+)
    /// 0 = view-through ad presentation, 1 = StoreKit-rendered ad or SKAdNetwork-attributed web ad
    /// </summary>
    [JsonPropertyName("fidelity-type")]
    public int? FidelityType { get; set; }

    /// <summary>
    /// Postback sequence index (version 4+)
    /// 0, 1, 2 represent the order of postbacks from three conversion windows
    /// </summary>
    [JsonPropertyName("postback-sequence-index")]
    public int? PostbackSequenceIndex { get; set; }

    /// <summary>
    /// Whether this is a redownload
    /// </summary>
    [JsonPropertyName("redownload")]
    public bool? Redownload { get; set; }

    /// <summary>
    /// Unique transaction ID for deduplication
    /// </summary>
    [JsonPropertyName("transaction-id")]
    [Required]
    public string TransactionId { get; set; } = string.Empty;
}

/// <summary>
/// Apple attribution report verification result
/// </summary>
public class AppleAttributionVerificationResult
{
    /// <summary>
    /// Whether verification was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Verification error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// SKAdNetwork version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Whether this is a winning attribution postback
    /// </summary>
    public bool IsWinningAttribution { get; set; }

    /// <summary>
    /// Original report data
    /// </summary>
    public AppleAttributionReportDto? OriginalReport { get; set; }
}

/// <summary>
/// Firebase Analytics event request DTO for forwarding attribution data
/// </summary>
public class AppleAttributionFirebaseEventDto
{
    /// <summary>
    /// Event name
    /// </summary>
    public string EventName { get; set; } = "apple_attribution_received";

    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Event parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}
