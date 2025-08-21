using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppleSignatureVerificationService : ApplicationService, IAppleSignatureVerificationService
{
    private readonly ILogger<AppleSignatureVerificationService> _logger;

    // Apple's public keys for different SKAdNetwork versions
    private static readonly Dictionary<string, string> ApplePublicKeys = new()
    {
        // P-192 public key for SKAdNetwork 1.0
        ["1.0"] = "MEkwEwYHKoZIzj0CAQYIKoZIzj0DAQEDMgAEMyHD625uvsmGq4C43cQ9BnfN2xslVT5V1nOmAMP6qaRRUll3PB1JYmgSm+62sosG",
        
        // NIST P-256 public key for SKAdNetwork 2.0+
        ["2.0+"] = "MFkwEwYHKoZIz0CAQYIKoZIz0DAQcDQgAEWdp8GPcGqmhgzEFj9Z2nSpQVddayaPe4FMzqM9wib1+aHaaIzoHoLN9zW4K8y4SPykE3YVK3sVqW6Af0lfx3gg=="
    };

    public AppleSignatureVerificationService(ILogger<AppleSignatureVerificationService> logger)
    {
        _logger = logger;
    }

    public async Task<AppleAttributionVerificationResult> VerifySignatureAsync(AppleAttributionReportDto report)
    {
        try
        {
            _logger.LogDebug("[AppleSignatureVerificationService][VerifySignatureAsync] Starting signature verification for transaction: {TransactionId}", 
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
            _logger.LogDebug("[AppleSignatureVerificationService][VerifySignatureAsync] Detected SKAdNetwork version: {Version}", version);

            // Build signature string
            var signatureString = BuildSignatureString(report, version);
            _logger.LogDebug("[AppleSignatureVerificationService][VerifySignatureAsync] Built signature string length: {Length}", signatureString.Length);

            // Verify signature
            var isValidSignature = await VerifyECDSASignatureAsync(signatureString, report.AttributionSignature, version);

            var result = new AppleAttributionVerificationResult
            {
                IsValid = isValidSignature,
                Version = version,
                IsWinningAttribution = report.DidWin ?? true, // Default to true for versions 1-2
                OriginalReport = report,
                ErrorMessage = isValidSignature ? null : "Signature verification failed"
            };

            _logger.LogInformation("[AppleSignatureVerificationService][VerifySignatureAsync] Verification completed for transaction {TransactionId}: Valid={IsValid}, Version={Version}, IsWinning={IsWinning}",
                report.TransactionId, result.IsValid, result.Version, result.IsWinningAttribution);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppleSignatureVerificationService][VerifySignatureAsync] Error verifying signature for transaction: {TransactionId}", 
                report.TransactionId);

            return new AppleAttributionVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Signature verification error: {ex.Message}",
                OriginalReport = report
            };
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
            using var ecdsa = version == "1.0" ? 
                ECDsa.Create(ECCurve.CreateFromFriendlyName("secp192r1")) : 
                ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Import Apple's public key
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // Prepare data to verify
            var dataBytes = Encoding.UTF8.GetBytes(signatureString);
            
            // Decode signature
            var signatureBytes = Convert.FromBase64String(base64Signature);

            // Verify signature using SHA-256
            var isValid = ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);

            _logger.LogDebug("[AppleSignatureVerificationService][VerifyECDSASignatureAsync] ECDSA verification result: {IsValid}, Version: {Version}", 
                isValid, version);

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppleSignatureVerificationService][VerifyECDSASignatureAsync] ECDSA verification failed for version: {Version}", version);
            return false;
        }
    }
}
