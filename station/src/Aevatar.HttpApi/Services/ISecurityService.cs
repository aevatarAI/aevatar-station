using System.Threading.Tasks;
using Aevatar.Common;
using Microsoft.AspNetCore.Http;

namespace Aevatar.Services;

/// <summary>
/// Security service interface for authentication and verification
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Get the real client IP address from HTTP context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP address</returns>
    string GetRealClientIp(HttpContext context);

    /// <summary>
    /// Check if security verification is required for the client
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <returns>True if verification is required</returns>
    Task<bool> IsSecurityVerificationRequiredAsync(string clientIp);

    /// <summary>
    /// Verify security based on platform and provided tokens
    /// </summary>
    /// <param name="request">Security verification request</param>
    /// <returns>Verification result</returns>
    Task<SecurityVerificationResult> VerifySecurityAsync(SecurityVerificationRequest request);

    /// <summary>
    /// Increment request count for rate limiting
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    Task IncrementRequestCountAsync(string clientIp);
}

/// <summary>
/// Security verification request model
/// </summary>
public class SecurityVerificationRequest
{
    public PlatformType Platform { get; set; }
    public string ClientIp { get; set; } = "";
    public string? RecaptchaToken { get; set; }
    public string? AcToken { get; set; }
}

/// <summary>
/// Security verification result model
/// </summary>
public class SecurityVerificationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string VerificationMethod { get; set; } = "";

    public static SecurityVerificationResult CreateSuccess(string method) =>
        new() { Success = true, Message = "Verification successful", VerificationMethod = method };

    public static SecurityVerificationResult CreateFailure(string message) =>
        new() { Success = false, Message = message };
}