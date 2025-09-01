using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Analytics;
using Aevatar.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace Aevatar.Service;

/// <summary>
/// Apple SKAdNetwork signature verification service
/// Implements signature verification logic according to Apple official documentation
/// Reference: https://developer.apple.com/documentation/storekit/verifying-an-install-validation-postback
/// </summary>
public interface IAppleSignatureVerificationService
{
    /// <summary>
    /// Verify Apple attribution report signature
    /// </summary>
    /// <param name="report">Attribution report</param>
    /// <returns>Verification result</returns>
    Task<AppleAttributionVerificationResult> VerifySignatureAsync(AppleAttributionReportDto report);

    /// <summary>
    /// Generates a unique numeric string for app instance ID
    /// </summary>
    Task<string> GenerateUniqueNumericStringAsync();

    Task ForwardToFirebaseAnalyticsAsync(AppleAttributionReportDto report,
        AppleAttributionVerificationResult verificationResult);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppleSignatureVerificationService : ApplicationService, IAppleSignatureVerificationService
{
    private readonly ILogger<AppleSignatureVerificationService> _logger;
    private readonly IGoogleAnalyticsService _googleAnalyticsService;

    // Apple's public keys for different SKAdNetwork versions
    private static readonly Dictionary<string, string> ApplePublicKeys = new()
    {
        // P-192 public key for SKAdNetwork 1.0
        ["1.0"] =
            "MEkwEwYHKoZIzj0CAQYIKoZIzj0DAQEDMgAEMyHD625uvsmGq4C43cQ9BnfN2xslVT5V1nOmAMP6qaRRUll3PB1JYmgSm+62sosG",

        // NIST P-256 public key for SKAdNetwork 2.0+
        ["2.0+"] = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEWdp8GPcGqmhgzEFj9Z2nSpQVddayaPe4FMzqM9wib1+aHaaIzoHoLN9zW4K8y4SPykE3YVK3sVqW6Af0lfx3gg=="
    };

    public AppleSignatureVerificationService(ILogger<AppleSignatureVerificationService> logger, IGoogleAnalyticsService googleAnalyticsService)
    {
        _logger = logger;
        _googleAnalyticsService = googleAnalyticsService;
    }

    public async Task<AppleAttributionVerificationResult> VerifySignatureAsync(AppleAttributionReportDto report)
    {
        try
        {
            _logger.LogDebug(
                "[AppleSignatureVerificationService][VerifySignatureAsync] Starting signature verification for transaction: {TransactionId}",
                report.TransactionId);

            // Check basic parameters
            if (string.IsNullOrEmpty(report.AttributionSignature))
            {
                return new AppleAttributionVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Attribution signature is missing",
                    OriginalReport = report
                };
            }

            // Determine version
            var version = DetermineVersion(report);
            _logger.LogDebug(
                "[AppleSignatureVerificationService][VerifySignatureAsync] Detected SKAdNetwork version: {Version}",
                version);

            // Build signature string
            var signatureString = BuildSignatureString(report, version);
            _logger.LogDebug(
                "[AppleSignatureVerificationService][VerifySignatureAsync] Built signature string length: {Length}",
                signatureString.Length);

            // Verify signature
            var isValidSignature =
                await VerifyECDSASignatureAsync(signatureString, report.AttributionSignature, version);

            var result = new AppleAttributionVerificationResult
            {
                IsValid = isValidSignature,
                Version = version,
                IsWinningAttribution = report.DidWin ?? true, // Default to true for versions 1-2
                OriginalReport = report,
                ErrorMessage = isValidSignature ? null : "Signature verification failed"
            };

            _logger.LogInformation(
                "[AppleSignatureVerificationService][VerifySignatureAsync] Verification completed for transaction {TransactionId}: Valid={IsValid}, Version={Version}, IsWinning={IsWinning}",
                report.TransactionId, result.IsValid, result.Version, result.IsWinningAttribution);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppleSignatureVerificationService][VerifySignatureAsync] Error verifying signature for transaction: {TransactionId}",
                report.TransactionId);

            return new AppleAttributionVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Signature verification error: {ex.Message}",
                OriginalReport = report
            };
        }
    }

    public Task<string> GenerateUniqueNumericStringAsync()
    {
        var bytes = new byte[16];
        var timestampBytes = BitConverter.GetBytes(DateTimeOffset.UtcNow.Ticks);
        Array.Copy(timestampBytes, 0, bytes, 0, 8);
        var random = new Random();
        random.NextBytes(bytes.AsSpan(8, 8));
        var hexString = Convert.ToHexString(bytes).ToLowerInvariant();
        return Task.FromResult(hexString);
    }

    public async Task ForwardToFirebaseAnalyticsAsync(AppleAttributionReportDto report,
        AppleAttributionVerificationResult verificationResult)
    {
        try
        {
            var appInstanceId = await GenerateUniqueNumericStringAsync();

            // Build Firebase event data
            var firebaseEvent = new GoogleAnalyticsEventRequestDto
            {
                EventName = "campaign_details",
                UserId = null,
                AppInstanceId = appInstanceId,
                Parameters = new Dictionary<string, object>
                {
                    ["source_platform"] = "apple_skan",
                    ["app_id"] = report.AppId,
                    ["transaction_id"] = report.TransactionId,
                    ["version"] = verificationResult.Version ?? "unknown",
                    ["ad_network_id"] = report.AdNetworkId,
                    ["attribution_signature"] = report.AttributionSignature ?? "unknown",
                    ["is_verified"] = verificationResult.IsValid
                }
            };
            
            // source、medium
            if (report.CampaignId.HasValue)
            {
                firebaseEvent.Parameters["campaign_id"] = report.CampaignId.Value;
            }

            if (report.FidelityType.HasValue)
            {
                firebaseEvent.Parameters["fidelity_type"] = report.FidelityType.Value;
            }

            if (report.DidWin.HasValue)
            {
                firebaseEvent.Parameters["did_win"] = report.DidWin.Value;
            }

            if (report.Redownload.HasValue)
            {
                firebaseEvent.Parameters["redownload"] = report.Redownload.Value;
            }

            if (report.SourceAppId.HasValue)
            {
                firebaseEvent.Parameters["source_app_id"] = report.SourceAppId.Value;
            }

            if (report.ConversionValue.HasValue)
            {
                firebaseEvent.Parameters["conversion_value"] = report.ConversionValue.Value;
            }

            // Add optional parameters
            if (!string.IsNullOrEmpty(report.SourceIdentifier))
            {
                firebaseEvent.Parameters["source_identifier"] = report.SourceIdentifier;
            }

            if (!string.IsNullOrEmpty(report.SourceDomain))
            {
                firebaseEvent.Parameters["source_domain"] = report.SourceDomain;
            }

            if (!string.IsNullOrEmpty(report.CoarseConversionValue))
            {
                firebaseEvent.Parameters["coarse_conversion_value"] = report.CoarseConversionValue;
            }

            if (report.PostbackSequenceIndex.HasValue)
            {
                firebaseEvent.Parameters["postback_sequence_index"] = report.PostbackSequenceIndex.Value;
            }

            // Send to Firebase
            await _googleAnalyticsService.TrackFirebaseEventAsync(firebaseEvent);

            _logger.LogDebug(
                "[AppleAttributionController][ForwardToFirebaseAnalyticsAsync] Apple attribution data forwarded to Firebase: TransactionId={TransactionId}",
                report.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppleAttributionController][ForwardToFirebaseAnalyticsAsync] Failed to forward Apple attribution to Firebase: TransactionId={TransactionId}",
                report.TransactionId);
            throw;
        }
    }

    /// <summary>
    /// Determine SKAdNetwork version
    /// </summary>
    private string DetermineVersion(AppleAttributionReportDto report)
    {
        if (!string.IsNullOrEmpty(report.Version))
        {
            return report.Version;
        }

        // If no version field, it's version 1.0
        return "1.0";
    }

    /// <summary>
    /// Build signature string according to version
    /// Reference: Apple documentation parameter combination order
    /// </summary>
    private string BuildSignatureString(AppleAttributionReportDto report, string version)
    {
        const string separator = "\u2063"; // Unicode invisible separator

        return version switch
        {
            "1.0" => BuildVersion10SignatureString(report, separator),
            "2.0" or "2.1" => BuildVersion2xSignatureString(report, separator),
            "2.2" => BuildVersion22SignatureString(report, separator),
            "3.0" => BuildVersion3SignatureString(report, separator),
            "4.0" => BuildVersion4SignatureString(report, separator),
            _ => throw new NotSupportedException($"Unsupported SKAdNetwork version: {version}")
        };
    }

    /// <summary>
    /// SKAdNetwork 1.0 signature string
    /// ad-network-id + separator + campaign-id + separator + app-id + separator + transaction-id
    /// </summary>
    private string BuildVersion10SignatureString(AppleAttributionReportDto report, string separator)
    {
        return string.Join(separator,
            report.AdNetworkId,
            report.CampaignId?.ToString() ?? "0",
            report.AppId.ToString(),
            report.TransactionId
        );
    }

    /// <summary>
    /// SKAdNetwork 2.0/2.1 signature string
    /// version + separator + ad-network-id + separator + campaign-id + separator + app-id + separator + transaction-id + separator + redownload [+ separator + source-app-id]
    /// </summary>
    private string BuildVersion2xSignatureString(AppleAttributionReportDto report, string separator)
    {
        var parts = new List<string>
        {
            report.Version!,
            report.AdNetworkId,
            report.CampaignId?.ToString() ?? "0",
            report.AppId.ToString(),
            report.TransactionId,
            (report.Redownload ?? false).ToString().ToLowerInvariant()
        };

        // source-app-id is only included when privacy threshold is met
        if (report.SourceAppId.HasValue)
        {
            parts.Add(report.SourceAppId.Value.ToString());
        }

        return string.Join(separator, parts);
    }

    /// <summary>
    /// SKAdNetwork 2.2 signature string
    /// version + separator + ad-network-id + separator + campaign-id + separator + app-id + separator + transaction-id + separator + redownload [+ separator + source-app-id] + separator + fidelity-type
    /// </summary>
    private string BuildVersion22SignatureString(AppleAttributionReportDto report, string separator)
    {
        var parts = new List<string>
        {
            report.Version!,
            report.AdNetworkId,
            report.CampaignId?.ToString() ?? "0",
            report.AppId.ToString(),
            report.TransactionId,
            (report.Redownload ?? false).ToString().ToLowerInvariant()
        };

        // source-app-id is only included when privacy threshold is met
        if (report.SourceAppId.HasValue)
        {
            parts.Add(report.SourceAppId.Value.ToString());
        }

        parts.Add(report.FidelityType?.ToString() ?? "1");

        return string.Join(separator, parts);
    }

    /// <summary>
    /// SKAdNetwork 3.0 signature string
    /// version + separator + ad-network-id + separator + campaign-id + separator + app-id + separator + transaction-id + separator + redownload [+ separator + source-app-id] + separator + fidelity-type + separator + did-win
    /// </summary>
    private string BuildVersion3SignatureString(AppleAttributionReportDto report, string separator)
    {
        var parts = new List<string>
        {
            report.Version!,
            report.AdNetworkId,
            report.CampaignId?.ToString() ?? "0",
            report.AppId.ToString(),
            report.TransactionId,
            (report.Redownload ?? false).ToString().ToLowerInvariant()
        };

        // source-app-id is only included when privacy threshold is met
        if (report.SourceAppId.HasValue)
        {
            parts.Add(report.SourceAppId.Value.ToString());
        }

        parts.Add(report.FidelityType?.ToString() ?? "1");
        parts.Add((report.DidWin ?? true).ToString().ToLowerInvariant());

        return string.Join(separator, parts);
    }

    /// <summary>
    /// SKAdNetwork 4.0 signature string
    /// version + separator + ad-network-id + separator + source-identifier + separator + app-id + separator + transaction-id + separator + redownload + separator + [source-app-id|source-domain] + separator + fidelity-type + separator + did-win + separator + postback-sequence-index
    /// </summary>
    private string BuildVersion4SignatureString(AppleAttributionReportDto report, string separator)
    {
        var parts = new List<string>
        {
            report.Version!,
            report.AdNetworkId,
            report.SourceIdentifier ?? "0", // Version 4.0 uses source-identifier instead of campaign-id
            report.AppId.ToString(),
            report.TransactionId,
            (report.Redownload ?? false).ToString().ToLowerInvariant()
        };

        // Add source-app-id or source-domain (for web ads)
        if (report.SourceAppId.HasValue)
        {
            parts.Add(report.SourceAppId.Value.ToString());
        }
        else if (!string.IsNullOrEmpty(report.SourceDomain))
        {
            parts.Add(report.SourceDomain);
        }
        else
        {
            // If neither exists, need to decide whether to add empty value based on privacy threshold
            // Conservative handling here: don't add extra separator
        }

        parts.Add(report.FidelityType?.ToString() ?? "1");
        parts.Add((report.DidWin ?? true).ToString().ToLowerInvariant());
        parts.Add(report.PostbackSequenceIndex?.ToString() ?? "0");

        return string.Join(separator, parts);
    }

    /// <summary>
    /// Verify signature using ECDSA
    /// </summary>
    private async Task<bool> VerifyECDSASignatureAsync(string signatureString, string base64Signature, string version)
    {
        try
        {
            // Get Apple's public key for the corresponding version
            var publicKeyBase64 = version == "1.0" ? ApplePublicKeys["1.0"] : ApplePublicKeys["2.0+"];

            // Decode public key
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

            // Create ECDsa instance
            using var ecdsa = version == "1.0"
                ? ECDsa.Create(ECCurve.CreateFromFriendlyName("secp192r1"))
                : ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Import Apple's public key
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            // Prepare data to verify
            var dataBytes = Encoding.UTF8.GetBytes(signatureString);

            // Decode signature
            var signatureBytes = Convert.FromBase64String(base64Signature);
            if (signatureBytes.Length == 70)
            {
                var ieee1363Signature = ConvertDerToIeeeP1363(signatureBytes);
                if (ieee1363Signature != null)
                {
                    signatureBytes = ieee1363Signature;
                }
            }

            // Verify signature using SHA-256
            var isValid = ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);

            _logger.LogDebug(
                "[AppleSignatureVerificationService][VerifyECDSASignatureAsync] ECDSA verification result: {IsValid}, Version: {Version}",
                isValid, version);

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppleSignatureVerificationService][VerifyECDSASignatureAsync] ECDSA verification failed for version: {Version}",
                version);
            return false;
        }
    }

    private byte[]? ConvertDerToIeeeP1363(byte[] derSignature)
    {
        try
        {
            if (derSignature.Length < 6) return null;

            int index = 0;

            // SEQUENCE (0x30)
            if (derSignature[index++] != 0x30) return null;

            int sequenceLength = derSignature[index++];
            if (sequenceLength >= 0x80)
            {
                int lengthBytes = sequenceLength & 0x7F;
                sequenceLength = 0;
                for (int i = 0; i < lengthBytes; i++)
                {
                    sequenceLength = (sequenceLength << 8) | derSignature[index++];
                }
            }

            // r value
            if (derSignature[index++] != 0x02) return null;
            int rLength = derSignature[index++];
            if (rLength >= 0x80)
            {
                var lengthBytes = rLength & 0x7F;
                rLength = 0;
                for (var i = 0; i < lengthBytes; i++)
                {
                    rLength = (rLength << 8) | derSignature[index++];
                }
            }

            var rBytes = new byte[rLength];
            Array.Copy(derSignature, index, rBytes, 0, rLength);
            index += rLength;

            //s value
            if (derSignature[index++] != 0x02) return null;
            int sLength = derSignature[index++];
            if (sLength >= 0x80)
            {
                var lengthBytes = sLength & 0x7F;
                sLength = 0;
                for (var i = 0; i < lengthBytes; i++)
                {
                    sLength = (sLength << 8) | derSignature[index++];
                }
            }

            var sBytes = new byte[sLength];
            Array.Copy(derSignature, index, sBytes, 0, sLength);

            //IEEE P1363(P-256，32 byte)
            var ieee1363 = new byte[64];

            var rStart = 0;
            if (rBytes.Length > 1 && rBytes[0] == 0x00) rStart = 1;
            var rActualLength = rBytes.Length - rStart;
            if (rActualLength <= 32)
            {
                Array.Copy(rBytes, rStart, ieee1363, 32 - rActualLength, rActualLength);
            }

            var sStart = 0;
            if (sBytes.Length > 1 && sBytes[0] == 0x00) sStart = 1;
            var sActualLength = sBytes.Length - sStart;
            if (sActualLength <= 32)
            {
                Array.Copy(sBytes, sStart, ieee1363, 32 + (32 - sActualLength), sActualLength);
            }

            return ieee1363;
        }
        catch
        {
            return null;
        }
    }
}